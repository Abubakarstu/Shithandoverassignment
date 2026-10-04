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
/// Accident logging. Always scoped to a shift the signed-in supervisor has claimed
/// and that has not been closed yet.
/// </summary>
[Authorize]
public class AccidentsController : Controller
{
    private readonly ShiftHandoverContext _context;
    private readonly IShiftService _shiftService;
    private readonly IShiftLogService _logService;

    public AccidentsController(
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
        ViewBag.Accidents = await _logService.GetAccidentsAsync(shiftId);

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

        return View("Form", new AccidentViewModel
        {
            ShiftId = shiftId,
            ShiftLabel = ShiftLabel(guard.Value!),
            OccurredAt = ClampToShift(guard.Value!, DateTime.Now)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var accident = await _context.Accidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (accident is null)
        {
            TempData["Error"] = "Accident record not found.";
            return RedirectToAction("Index", "Shifts");
        }

        var guard = await _shiftService.GetEditableShiftAsync(accident.ShiftId, User.GetSupervisorId());
        if (!guard.Succeeded)
        {
            TempData["Error"] = guard.ErrorMessage;
            return RedirectToAction("Details", "Shifts", new { id = accident.ShiftId });
        }

        var model = new AccidentViewModel
        {
            Id = accident.Id,
            ShiftId = accident.ShiftId,
            ShiftLabel = ShiftLabel(guard.Value!),
            OccurredAt = accident.OccurredAt,
            AccidentType = accident.AccidentType,
            Severity = accident.Severity,
            Location = accident.Location,
            PersonsInjured = accident.PersonsInjured,
            PersonsInvolved = accident.PersonsInvolved,
            InjuryType = accident.InjuryType,
            Description = accident.Description,
            ImmediateActionTaken = accident.ImmediateActionTaken,
            ReportedTo = accident.ReportedTo,
            InvestigationStatus = accident.InvestigationStatus,
            IsReportable = accident.IsReportable
        };

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AccidentViewModel model)
    {
        // Re-validate the shift rules even if the client bypassed the form.
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

        var result = await _logService.SaveAccidentAsync(model, User.GetSupervisorId());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            model.ShiftLabel = ShiftLabel(guard.Value!);
            return View("Form", model);
        }

        TempData["Success"] = model.Id is null
            ? "Accident recorded successfully."
            : "Accident record updated.";

        return RedirectToAction(nameof(Index), new { shiftId = model.ShiftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var accident = await _context.Accidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        var shiftId = accident?.ShiftId ?? 0;

        var result = await _logService.DeleteAccidentAsync(id, User.GetSupervisorId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Accident record deleted."
            : result.ErrorMessage;

        return RedirectToAction("Details", "Shifts", new { id = shiftId });
    }

    private static string ShiftLabel(Shift shift) =>
        $"{shift.ShiftDate:dd MMM yyyy} - {shift.ShiftType} ({shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm})";

    private static DateTime ClampToShift(Shift shift, DateTime candidate)
    {
        // Default the "time of accident" to something sensible inside the shift window.
        if (candidate < shift.ShiftDate.Date || candidate > shift.ShiftDate.Date.AddDays(1))
        {
            return shift.ShiftDate.Date.Add(shift.StartTime);
        }

        return DateTime.Parse($"{shift.ShiftDate:yyyy-MM-dd} {candidate:HH\\:mm}");
    }
}