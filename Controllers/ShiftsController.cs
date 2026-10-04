using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Helpers;
using ShiftHandover.Models.Enums;
using ShiftHandover.Models.ViewModels;
using ShiftHandover.Services;

namespace ShiftHandover.Controllers;

[Authorize]
public class ShiftsController : Controller
{
    private readonly ShiftHandoverContext _context;
    private readonly IShiftService _shiftService;
    private readonly IReportDispatchService _dispatchService;
    private readonly ILogger<ShiftsController> _logger;

    public ShiftsController(
        ShiftHandoverContext context,
        IShiftService shiftService,
        IReportDispatchService dispatchService,
        ILogger<ShiftsController> logger)
    {
        _context = context;
        _shiftService = shiftService;
        _dispatchService = dispatchService;
        _logger = logger;
    }

    public static readonly string[] ShiftTypeNames = ShiftTypes.All.ToArray();

    // ------------------------------------------------------------------ List

    [HttpGet]
    public async Task<IActionResult> Index(ShiftListViewModel filter)
    {
        var supervisorId = User.GetSupervisorId();

        filter.CurrentSupervisorId = supervisorId;
        filter.IsAdmin = User.IsAdmin();

        var query = _context.Shifts
            .AsNoTracking()
            .Include(x => x.ClaimedBy)
            .Include(x => x.ClosedBy)
            .AsQueryable();

        if (filter.StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == filter.StatusFilter.Value);
        }

        if (filter.FromDate.HasValue)
        {
            var from = filter.FromDate.Value.Date;
            query = query.Where(x => x.ShiftDate >= from);
        }

        if (filter.ToDate.HasValue)
        {
            // Inclusive of the whole "To" day.
            var toExclusive = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.ShiftDate < toExclusive);
        }

        if (filter.MineOnly)
        {
            query = query.Where(x => x.ClaimedById == supervisorId);
        }

        filter.Shifts = await query
            .OrderBy(x => x.ShiftDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        return View(filter);
    }

    // ------------------------------------------------------------------ Details

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? message, string? error)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .Include(x => x.ClaimedBy)
            .Include(x => x.ClosedBy)
            .Include(x => x.Accidents)
            .Include(x => x.Incidents)
            .Include(x => x.ManpowerRecords)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shift is null)
        {
            TempData["Error"] = "Shift not found.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            TempData["Success"] = message;
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            TempData["Error"] = error;
        }

        ViewBag.SupervisorId = User.GetSupervisorId();
        ViewBag.IsAdmin = User.IsAdmin();
        ViewBag.CanEdit = shift.IsEditableBy(User.GetSupervisorId());

        return View(shift);
    }

    // ------------------------------------------------------------------ Claim

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(int id)
    {
        var result = await _shiftService.ClaimShiftAsync(id, User.GetSupervisorId());

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] =
            "Shift claimed successfully. You are now the only supervisor who can log into it.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------------ Release (mistake recovery)

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int id)
    {
        var supervisorId = User.GetSupervisorId();

        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == id);
        if (shift is null)
        {
            TempData["Error"] = "Shift not found.";
            return RedirectToAction(nameof(Index));
        }

        if (shift.Status == ShiftStatus.Closed)
        {
            TempData["Error"] = "A closed shift cannot be released.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (shift.ClaimedById != supervisorId)
        {
            TempData["Error"] = "Only the supervisor who claimed this shift can release it.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var hasEntries = await _context.Shifts.AnyAsync(x => x.Id == id &&
            (x.Accidents.Any() || x.Incidents.Any() || x.ManpowerRecords.Any()));

        if (hasEntries)
        {
            TempData["Error"] = "This shift already has logged entries and can no longer be released. Close it instead.";
            return RedirectToAction(nameof(Details), new { id });
        }

        shift.Status = ShiftStatus.Open;
        shift.ClaimedById = null;
        shift.ClaimedAt = null;
        await _context.SaveChangesAsync();

        _context.AuditLogs.Add(new Models.Entities.AuditLog
        {
            EntityName = "Shift",
            EntityId = id,
            Action = "Release",
            Details = $"Shift released back to the open pool by supervisor #{supervisorId}.",
            UserId = supervisorId,
            UserName = User.GetFullName()
        });
        await _context.SaveChangesAsync();

        TempData["Success"] = "Shift released and is available for other supervisors again.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------------ Close

    [HttpGet]
    public async Task<IActionResult> Close(int id)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .Include(x => x.ClaimedBy)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shift is null)
        {
            TempData["Error"] = "Shift not found.";
            return RedirectToAction(nameof(Index));
        }

        if (shift.ClaimedById != User.GetSupervisorId())
        {
            TempData["Error"] = "Only the supervisor who claimed this shift can close it.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (shift.Status == ShiftStatus.Closed)
        {
            TempData["Error"] = "This shift is already closed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.Shift = shift;

        return View(new CloseShiftViewModel
        {
            ShiftId = id,
            HandoverRemarks = shift.HandoverRemarks,
            PendingActions = shift.PendingActions
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(CloseShiftViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var shiftForView = await _context.Shifts.AsNoTracking().Include(x => x.ClaimedBy)
                .FirstOrDefaultAsync(x => x.Id == model.ShiftId);

            ViewBag.Shift = shiftForView;
            return View(model);
        }

        var supervisorId = User.GetSupervisorId();
        var result = await _shiftService.CloseShiftAsync(
            model.ShiftId, supervisorId, model.HandoverRemarks, model.PendingActions);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id = model.ShiftId });
        }

        // Shift is now closed and frozen: generate the PDF and e-mail it to the other supervisors.
        try
        {
            var dispatch = await _dispatchService.GenerateAndDistributeAsync(model.ShiftId, supervisorId);

            var statusText = dispatch.Delivery.Status switch
            {
                DeliveryStatus.Sent =>
                    $"The handover PDF was e-mailed to {dispatch.Recipients.Count} supervisor(s): {string.Join(", ", dispatch.Recipients)}.",
                DeliveryStatus.QueuedToOutbox =>
                    $"SMTP was unavailable, so the PDF and message were written to App_Data/EmailOutbox for {dispatch.Recipients.Count} recipient(s).",
                _ =>
                    $"Report generated as '{dispatch.Report.FileName}' but e-mail delivery failed: {dispatch.Delivery.ErrorMessage}"
            };

            TempData["Success"] = $"Shift closed. {statusText}";
        }
        catch (Exception ex)
        {
            // The shift stays closed even if report generation fails - never roll back the closure.
            _logger.LogError(ex, "Shift {ShiftId} closed but report generation failed.", model.ShiftId);
            TempData["Error"] =
                $"Shift was closed successfully, but the handover report could not be generated: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = model.ShiftId });
    }

    // ------------------------------------------------------------------ Shift roster generation (admin)

    [HttpGet]
    [Authorize(Roles = nameof(SupervisorRole.Admin))]
    public IActionResult Generate() => View(new GenerateShiftsViewModel());

    [HttpPost]
    [Authorize(Roles = nameof(SupervisorRole.Admin))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateShiftsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.ToDate.Date < model.FromDate.Date)
        {
            ModelState.AddModelError(nameof(model.ToDate), "The end date cannot be before the start date.");
            return View(model);
        }

        var wanted = new List<(string Type, TimeSpan Start, TimeSpan End)>();
        if (model.IncludeMorning)
        {
            wanted.Add(ShiftTypes.Get(ShiftTypes.Morning));
        }

        if (model.IncludeAfternoon)
        {
            wanted.Add(ShiftTypes.Get(ShiftTypes.Afternoon));
        }

        if (model.IncludeNight)
        {
            wanted.Add(ShiftTypes.Get(ShiftTypes.Night));
        }

        if (wanted.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Select at least one shift type.");
            return View(model);
        }

        if ((model.ToDate.Date - model.FromDate.Date).TotalDays > 60)
        {
            ModelState.AddModelError(string.Empty, "Please generate shifts in blocks of 60 days or less.");
            return View(model);
        }

        var newShifts = new List<Models.Entities.Shift>();

        for (var date = model.FromDate.Date; date <= model.ToDate.Date; date = date.AddDays(1))
        {
            foreach (var definition in wanted)
            {
                // Skip if the (date, type) slot already exists.
                if (await _context.Shifts.AnyAsync(x => x.ShiftDate == date && x.ShiftType == definition.Type))
                {
                    continue;
                }

                newShifts.Add(new Models.Entities.Shift
                {
                    ShiftDate = date,
                    ShiftType = definition.Type,
                    StartTime = definition.Start,
                    EndTime = definition.End,
                    Area = model.Area,
                    Status = ShiftStatus.Open,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (newShifts.Count == 0)
        {
            TempData["Error"] = "No new shifts were created - all of those slots already exist.";
            return RedirectToAction(nameof(Index));
        }

        _context.Shifts.AddRange(newShifts);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{newShifts.Count} shift(s) created and waiting to be claimed.";
        return RedirectToAction(nameof(Index));
    }
}