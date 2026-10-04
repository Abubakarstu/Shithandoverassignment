namespace ShiftHandover.Models.Enums;

public enum SupervisorRole
{
    /// <summary>Can claim shifts, log entries and close shifts.</summary>
    Supervisor = 0,

    /// <summary>Can do everything a supervisor can do plus manage shifts and view all reports.</summary>
    Admin = 1
}