using System.ComponentModel.DataAnnotations;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// Record of every automatic e-mail produced when a shift is closed.
/// </summary>
public class EmailLog
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift? Shift { get; set; }

    public int? ShiftReportId { get; set; }

    public ShiftReport? ShiftReport { get; set; }

    [Required, StringLength(300)]
    [Display(Name = "To")]
    public string ToAddresses { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Body { get; set; }

    [StringLength(300)]
    public string? AttachmentFileName { get; set; }

    public DeliveryStatus Status { get; set; }

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }
}