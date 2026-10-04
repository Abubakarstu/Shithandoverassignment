using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Helpers;
using ShiftHandover.Models;
using ShiftHandover.Models.Enums;
using ShiftHandover.Models.ViewModels;

namespace ShiftHandover.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ShiftHandoverContext _context;

    public HomeController(ShiftHandoverContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var supervisorId = User.GetSupervisorId();
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var model = new DashboardViewModel
        {
            SupervisorName = User.GetFullName(),
            OpenShifts = await _context.Shifts.CountAsync(x => x.Status == ShiftStatus.Open),
            MyActiveShifts = await _context.Shifts.CountAsync(x => x.Status == ShiftStatus.Claimed && x.ClaimedById == supervisorId),
            ClosedToday = await _context.Shifts.CountAsync(x => x.Status == ShiftStatus.Closed && x.ClosedAt >= today),
            AccidentsThisMonth = await _context.Accidents.CountAsync(x => x.OccurredAt >= monthStart),
            IncidentsThisMonth = await _context.Incidents.CountAsync(x => x.OccurredAt >= monthStart),
            MyActiveShiftList = await _context.Shifts
                .AsNoTracking()
                .Include(x => x.ClaimedBy)
                .Where(x => x.Status == ShiftStatus.Claimed && x.ClaimedById == supervisorId)
                .OrderBy(x => x.ShiftDate)
                .Take(8)
                .ToListAsync(),
            OpenShiftList = await _context.Shifts
                .AsNoTracking()
                .Where(x => x.Status == ShiftStatus.Open)
                .OrderBy(x => x.ShiftDate)
                .ThenBy(x => x.StartTime)
                .Take(8)
                .ToListAsync(),
            RecentEmails = await _context.EmailLogs
                .AsNoTracking()
                .Include(x => x.Shift)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}