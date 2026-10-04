using System.ComponentModel.DataAnnotations;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// Audit trail of the PDF handover report generated for a closed shift.
/// </summary>
public class ShiftReport
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift? Shift { get; set; }

    public int GeneratedById { get; set; }

    public Supervisor? GeneratedBy { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [StringLength(300)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>Path of the stored PDF, used for later downloads and e-mail attachments.</summary>
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public int AccidentCount { get; set; }

    public int IncidentCount { get; set; }

    public int ManpowerRowCount { get; set; }

    public bool EmailedSuccessfully { get; set; }
}