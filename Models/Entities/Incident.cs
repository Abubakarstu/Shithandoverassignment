using System.ComponentModel.DataAnnotations;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// An incident (non-injury event) recorded during a shift.
/// </summary>
public class Incident
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift? Shift { get; set; }

    public int LoggedById { get; set; }

    public Supervisor? LoggedBy { get; set; }

    [Required]
    [Display(Name = "Date and time of incident")]
    public DateTime OccurredAt { get; set; }

    public IncidentCategory Category { get; set; }

    public SeverityLevel Severity { get; set; }

    public InvestigationStatus InvestigationStatus { get; set; } = InvestigationStatus.Open;

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(150)]
    public string? ReferenceNumber { get; set; }

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    [Display(Name = "Action taken")]
    public string ActionTaken { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Escalated to")]
    public string? EscalatedTo { get; set; }

    public bool IsEscalated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}