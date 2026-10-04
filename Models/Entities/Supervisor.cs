using System.ComponentModel.DataAnnotations;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Models.Entities;

/// <summary>
/// A user of the system. Only supervisors/administrators can sign in.
/// </summary>
public class Supervisor
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [Display(Name = "Employee code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Department { get; set; } = string.Empty;

    public SupervisorRole Role { get; set; } = SupervisorRole.Supervisor;

    public bool IsActive { get; set; } = true;

    /// <summary>Base64 PBKDF2 hash of the password.</summary>
    [Required, StringLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Base64 PBKDF2 salt.</summary>
    [Required, StringLength(200)]
    public string PasswordSalt { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Navigation ----
    public ICollection<Shift> ClaimedShifts { get; set; } = new List<Shift>();
    public ICollection<Shift> ClosedShifts { get; set; } = new List<Shift>();
    public ICollection<Accident> AccidentsLogged { get; set; } = new List<Accident>();
    public ICollection<Incident> IncidentsLogged { get; set; } = new List<Incident>();
    public ICollection<ManpowerRecord> ManpowerRecordsLogged { get; set; } = new List<ManpowerRecord>();

    /// <summary>Full name plus employee code, handy for views and PDF reports.</summary>
    public string DisplayName => $"{FullName} ({EmployeeCode})";
}