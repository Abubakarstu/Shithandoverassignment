using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// A duty slot (date + shift type) that a supervisor can claim.
/// Only the claiming supervisor may add logs to a claimed shift, and nobody may edit a closed shift.
/// </summary>
public class Shift
{
    public int Id { get; set; }

    /// <summary>Calendar date of the duty, stored with the time component zeroed.</summary>
    public DateTime ShiftDate { get; set; }

    /// <summary>Morning / Afternoon / Night.</summary>
    public string ShiftType { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    [StringLength(120)]
    public string? Area { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    // ---- Claiming ----
    public int? ClaimedById { get; set; }

    public Supervisor? ClaimedBy { get; set; }

    public DateTime? ClaimedAt { get; set; }

    // ---- Closing ----
    public int? ClosedById { get; set; }

    public Supervisor? ClosedBy { get; set; }

    public DateTime? ClosedAt { get; set; }

    [StringLength(2000)]
    [Display(Name = "Handover remarks")]
    public string? HandoverRemarks { get; set; }

    /// <summary>Free text field for pending items the next shift must pick up.</summary>
    [StringLength(2000)]
    [Display(Name = "Pending actions")]
    public string? PendingActions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Row version used for optimistic concurrency so two supervisors can never claim the same shift.</summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // ---- Navigation ----
    public ICollection<Accident> Accidents { get; set; } = new List<Accident>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    public ICollection<ManpowerRecord> ManpowerRecords { get; set; } = new List<ManpowerRecord>();
    public ICollection<ShiftReport> Reports { get; set; } = new List<ShiftReport>();

    public bool IsEditableBy(int supervisorId) =>
        Status != ShiftStatus.Closed && ClaimedById == supervisorId;

    [NotMapped]
    public string StatusBadge => Status switch
    {
        ShiftStatus.Claimed => "badge-claimed",
        ShiftStatus.Closed => "badge-closed",
        _ => "badge-open"
    };
}