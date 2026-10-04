using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Helpers;
using ShiftHandover.Models.Enums;
using ShiftHandover.Services;

namespace ShiftHandover.Controllers;

/// <summary>
/// Lists closed shifts, serves their handover PDFs and shows the automatic e-mail log.
/// </summary>
[Authorize]
public class ReportsController : Controller
{
    private readonly ShiftHandoverContext _context;
    private readonly IReportService _reportService;
    private readonly IReportDispatchService _dispatchService;

    public ReportsController(
        ShiftHandoverContext context,
        IReportService reportService,
        IReportDispatchService dispatchService)
    {
        _context = context;
        _reportService = reportService;
        _dispatchService = dispatchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var supervisorId = User.GetSupervisorId();

        var closedShifts = await _context.Shifts
            .AsNoTracking()
            .Include(x => x.ClaimedBy)
            .Include(x => x.ClosedBy)
            .Include(x => x.Reports)
            .Where(x => x.Status == ShiftStatus.Closed)
            .OrderByDescending(x => x.ShiftDate)
            .ThenByDescending(x => x.ClosedAt)
            .ToListAsync();

        ViewBag.Shifts = closedShifts;
        ViewBag.SupervisorId = supervisorId;
        ViewBag.IsAdmin = User.IsAdmin();
        ViewBag.AuditLogs = await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(25)
            .ToListAsync();

        return View();
    }

    /// <summary>Opens the stored PDF in the browser.</summary>
    [HttpGet]
    public async Task<IActionResult> Preview(int shiftId)
    {
        var (bytes, fileName) = await BuildPdfAsync(shiftId);
        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>Downloads the stored PDF.</summary>
    [HttpGet]
    public async Task<IActionResult> Download(int shiftId)
    {
        var (bytes, fileName) = await BuildPdfAsync(shiftId);
        return File(bytes, "application/pdf", fileName, enableRangeProcessing: true);
    }

    /// <summary>Re-generates and re-sends the handover e-mail (admin, or the supervisor who closed it).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = nameof(SupervisorRole.Admin))]
    public async Task<IActionResult> Resend(int shiftId)
    {
        try
        {
            var result = await _dispatchService.GenerateAndDistributeAsync(shiftId, User.GetSupervisorId());

            TempData[result.Delivery.Status == DeliveryStatus.Failed ? "Error" : "Success"] =
                result.Delivery.Status switch
                {
                    DeliveryStatus.Sent =>
                        $"Report re-sent to {result.Recipients.Count} supervisor(s).",
                    DeliveryStatus.QueuedToOutbox =>
                        $"SMTP unavailable - report written to App_Data/EmailOutbox for {result.Recipients.Count} recipient(s).",
                    _ =>
                        $"Report generated but delivery failed: {result.Delivery.ErrorMessage}"
                };
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not regenerate the report: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EmailLog()
    {
        var logs = await _context.EmailLogs
            .AsNoTracking()
            .Include(x => x.Shift)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync();

        return View(logs);
    }

    private async Task<(byte[] Bytes, string FileName)> BuildPdfAsync(int shiftId)
    {
        var stored = await _context.ShiftReports
            .AsNoTracking()
            .Where(x => x.ShiftId == shiftId)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync();

        if (stored is not null && !string.IsNullOrWhiteSpace(stored.FilePath) && System.IO.File.Exists(stored.FilePath))
        {
            return (await System.IO.File.ReadAllBytesAsync(stored.FilePath), stored.FileName);
        }

        // Nothing on disk (e.g. the App_Data folder was cleared): render it on the fly.
        var data = await _reportService.BuildReportDataAsync(shiftId, User.GetFullName());

        return (await _reportService.GeneratePdfAsync(data), _reportService.BuildFileName(data));
    }
}