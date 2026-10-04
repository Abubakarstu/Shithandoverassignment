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
/// Manpower (head count) logging for a claimed, still-open shift.
/// One row per function per shift.
/// </summary>
[Authorize]
public class ManpowerController : Controller
{
    private readonly ShiftHandoverContext _context;
    private readonly IShiftService _shiftService;
    private readonly IShiftLogService _logService;

    public ManpowerController(
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
        ViewBag.Records = await _logService.GetManpowerAsync(shiftId);

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

        return View("Form", new ManpowerViewModel
        {
            ShiftId = shiftId,
            ShiftLabel = ShiftLabel(guard.Value!)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _context.ManpowerRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (record is null)
        {
            TempData["Error"] = "Manpower record not found.";
            return RedirectToAction("Index", "Shifts");
        }

        var guard = await _shiftService.GetEditableShiftAsync(record.ShiftId, User.GetSupervisorId());
        if (!guard.Succeeded)
        {
            TempData["Error"] = guard.ErrorMessage;
            return RedirectToAction("Details", "Shifts", new { id = record.ShiftId });
        }

        return View("Form", new ManpowerViewModel
        {
            Id = record.Id,
            ShiftId = record.ShiftId,
            ShiftLabel = ShiftLabel(guard.Value!),
            FunctionName = record.FunctionName,
            Planned = record.Planned,
            OnDuty = record.OnDuty,
            OnLeave = record.OnLeave,
            Absent = record.Absent,
            Overtime = record.Overtime,
            Remarks = record.Remarks
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ManpowerViewModel model)
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

        var result = await _logService.SaveManpowerAsync(model, User.GetSupervisorId());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            model.ShiftLabel = ShiftLabel(guard.Value!);
            return View("Form", model);
        }

        TempData["Success"] = model.Id is null
            ? "Manpower details saved."
            : "Manpower details updated.";

        return RedirectToAction(nameof(Index), new { shiftId = model.ShiftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _context.ManpowerRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        var shiftId = record?.ShiftId ?? 0;

        var result = await _logService.DeleteManpowerAsync(id, User.GetSupervisorId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Manpower row deleted."
            : result.ErrorMessage;

        return RedirectToAction("Details", "Shifts", new { id = shiftId });
    }

    private static string ShiftLabel(Shift shift) =>
        $"{shift.ShiftDate:dd MMM yyyy} - {shift.ShiftType} ({shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm})";
}