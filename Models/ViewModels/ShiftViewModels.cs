using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftHandover.Models.Entities;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Models.ViewModels;

public class ShiftListViewModel
{
    public List<Shift> Shifts { get; set; } = new();

    public ShiftStatus? StatusFilter { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    /// <summary>When true the list only contains shifts claimed by the signed-in supervisor.</summary>
    public bool MineOnly { get; set; }

    public int CurrentSupervisorId { get; set; }

    public bool IsAdmin { get; set; }

    public int OpenCount => Shifts.Count(x => x.Status == ShiftStatus.Open);
    public int ClaimedCount => Shifts.Count(x => x.Status == ShiftStatus.Claimed);
    public int ClosedCount => Shifts.Count(x => x.Status == ShiftStatus.Closed);
}

public class CloseShiftViewModel
{
    public int ShiftId { get; set; }

    [StringLength(2000)]
    [Display(Name = "Handover remarks")]
    public string? HandoverRemarks { get; set; }

    [StringLength(2000)]
    [Display(Name = "Pending actions for the next shift")]
    public string? PendingActions { get; set; }
}

public class GenerateShiftsViewModel
{
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "From date")]
    public DateTime FromDate { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "To date")]
    public DateTime ToDate { get; set; } = DateTime.Today.AddDays(6);

    [Display(Name = "Shift types")]
    public bool IncludeMorning { get; set; } = true;

    public bool IncludeAfternoon { get; set; } = true;

    public bool IncludeNight { get; set; } = true;

    [StringLength(120)]
    public string? Area { get; set; }
}

public class DashboardViewModel
{
    public string SupervisorName { get; set; } = string.Empty;

    public int OpenShifts { get; set; }

    public int MyActiveShifts { get; set; }

    public int ClosedToday { get; set; }

    public int AccidentsThisMonth { get; set; }

    public int IncidentsThisMonth { get; set; }

    public List<Shift> MyActiveShiftList { get; set; } = new();

    public List<Shift> OpenShiftList { get; set; } = new();

    public List<EmailLog> RecentEmails { get; set; } = new();
}