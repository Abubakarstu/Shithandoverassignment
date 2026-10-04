using System.ComponentModel.DataAnnotations;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// Generic audit trail for state changes (claim, close, log create/update/delete, report generated).
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    [StringLength(60)]
    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [StringLength(60)]
    public string Action { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Details { get; set; }

    public int? UserId { get; set; }

    [StringLength(150)]
    public string? UserName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}