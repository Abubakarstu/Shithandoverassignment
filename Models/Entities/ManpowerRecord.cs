using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// Manpower (head count) snapshot for one function/area during a shift.
/// One row per function per shift, e.g. Ramp Operations - planned 12, on duty 11.
/// </summary>
public class ManpowerRecord
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift? Shift { get; set; }

    public int LoggedById { get; set; }

    public Supervisor? LoggedBy { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Function / area")]
    public string FunctionName { get; set; } = string.Empty;

    [Range(0, 10000)]
    public int Planned { get; set; }

    [Range(0, 10000)]
    [Display(Name = "On duty")]
    public int OnDuty { get; set; }

    [Range(0, 10000)]
    [Display(Name = "On leave")]
    public int OnLeave { get; set; }

    [Range(0, 10000)]
    public int Absent { get; set; }

    [Range(0, 10000)]
    public int Overtime { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public int Shortfall => Math.Max(0, Planned - OnDuty);
}