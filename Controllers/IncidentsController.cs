using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Helpers;
using ShiftHandover.Models.Entities;
using ShiftHandover.Models.ViewModels;
using ShiftHandover.Services;

namespace ShiftHandover.Controllers;

/// <summary>
/// Incident logging for a claimed, still-open shift.
/// </summary>
[Authorize]
public class IncidentsController : Controller
{
    private readonly ShiftHandoverContext _context;
    private readonly IShiftService _shiftService;
    private readonly IShiftLogService _logService;

    public IncidentsController(
        ShiftHandoverContext context,
        IShiftService shiftService,
        IShiftLogService logService)
    {
        _context = context;
        _shiftService = shiftService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int shiftId)
    {
        var shift = await _context.Shifts.AsNoTracking()
            .Include(x => x.ClaimedBy)
            .Include(x => x.ClosedBy)
            .FirstOrDefaultAsync(x => x.Id == shiftId);

        if (shift is null)
        {
            TempData["Error"] = "Shift not found.";
            return RedirectToAction("Index", "Shifts");
        }

        ViewBag.Shift = shift;
        ViewBag.CanEdit = shift.IsEditableBy(User.GetSupervisorId());
        ViewBag.Incidents = await _logService.GetIncidentsAsync(shiftId);

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Create(int shiftId)
    {
        var guard = await _shiftService.GetEditableShiftAsync(shiftId, User.GetSupervisorId());
        if (!guard.Succeeded)
        {
            TempData["Error"] = guard.ErrorMessage;
            return RedirectToAction("Details", "Shifts", new { id = shiftId });
        }

        return View("Form", new IncidentViewModel
        {
            ShiftId = shiftId,
            ShiftLabel = ShiftLabel(guard.Value!),
            OccurredAt = ClampToShift(guard.Value!, DateTime.Now)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var incident = await _context.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (incident is null)
        {
            TempData["Error"] = "Incident record not found.";
            return RedirectToAction("Index", "Shifts");
        }

        var guard = await _shiftService.GetEditableShiftAsync(incident.ShiftId, User.GetSupervisorId());
        if (!guard.Succeeded)
        {
            TempData["Error"] = guard.ErrorMessage;
            return RedirectToAction("Details", "Shifts", new { id = incident.ShiftId });
        }

        return View("Form", new IncidentViewModel
        {
            Id = incident.Id,
            ShiftId = incident.ShiftId,
            ShiftLabel = ShiftLabel(guard.Value!),
            OccurredAt = incident.OccurredAt,
            Category = incident.Category,
            Severity = incident.Severity,
            InvestigationStatus = incident.InvestigationStatus,
            Location = incident.Location,
            ReferenceNumber = incident.ReferenceNumber,
            Description = incident.Description,
            ActionTaken = incident.ActionTaken,
            EscalatedTo = incident.EscalatedTo,
            IsEscalated = incident.IsEscalated
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(IncidentViewModel model)
    {
        var guard = await _shiftService.GetEditableShiftAsync(model.ShiftId, User.GetSupervisorId());
        if (!guard.Succeeded)
        {
            TempData["Error"] = guard.ErrorMessage;
            return RedirectToAction("Details", "Shifts", new { id = model.ShiftId });
        }

        if (!ModelState.IsValid)
        {
            model.ShiftLabel = ShiftLabel(guard.Value!);
            return View("Form", model);
        }

        var result = await _logService.SaveIncidentAsync(model, User.GetSupervisorId());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            model.ShiftLabel = ShiftLabel(guard.Value!);
            return View("Form", model);
        }

        TempData["Success"] = model.Id is null
            ? "Incident recorded successfully."
            : "Incident record updated.";

        return RedirectToAction(nameof(Index), new { shiftId = model.ShiftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var incident = await _context.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        var shiftId = incident?.ShiftId ?? 0;

        var result = await _logService.DeleteIncidentAsync(id, User.GetSupervisorId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Incident record deleted."
            : result.ErrorMessage;

        return RedirectToAction("Details", "Shifts", new { id = shiftId });
    }

    private static string ShiftLabel(Shift shift) =>
        $"{shift.ShiftDate:dd MMM yyyy} - {shift.ShiftType} ({shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm})";

    private static DateTime ClampToShift(Shift shift, DateTime candidate)
    {
        if (candidate < shift.ShiftDate.Date || candidate > shift.ShiftDate.Date.AddDays(1))
        {
            return shift.ShiftDate.Date.Add(shift.StartTime);
        }

        return DateTime.Parse($"{shift.ShiftDate:yyyy-MM-dd} {candidate:HH\\:mm}");
    }
}