using System.ComponentModel.DataAnnotations;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// An accident recorded during a shift. Always belongs to exactly one <see cref="Shift"/>.
/// </summary>
public class Accident
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift? Shift { get; set; }

    /// <summary>Supervisor who entered the accident record.</summary>
    public int LoggedById { get; set; }

    public Supervisor? LoggedBy { get; set; }

    [Required]
    [Display(Name = "Date and time of accident")]
    public DateTime OccurredAt { get; set; }

    public AccidentType AccidentType { get; set; }

    public SeverityLevel Severity { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Number of people injured (0 for pure property damage).</summary>
    [Range(0, 100)]
    [Display(Name = "Persons injured")]
    public int PersonsInjured { get; set; }

    [StringLength(150)]
    [Display(Name = "Person(s) involved")]
    public string? PersonsInvolved { get; set; }

    [StringLength(150)]
    [Display(Name = "Injury type")]
    public string? InjuryType { get; set; }

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    [Display(Name = "Immediate action taken")]
    public string ImmediateActionTaken { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Reported to")]
    public string? ReportedTo { get; set; }

    public InvestigationStatus InvestigationStatus { get; set; } = InvestigationStatus.Open;

    /// <summary>True when the accident must be escalated to the HSE / regulator.</summary>
    [Display(Name = "Reportable to regulator")]
    public bool IsReportable { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}