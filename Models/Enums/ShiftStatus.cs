namespace ShiftHandover.Models.Enums;

/// <summary>
/// Lifecycle of a shift.
/// </summary>
public enum ShiftStatus
{
    /// <summary>Published and waiting for a supervisor to claim it.</summary>
    Open = 0,

    /// <summary>Claimed by a supervisor who is now the only one allowed to log into it.</summary>
    Claimed = 1,

    /// <summary>Duty finished, logging is frozen and the handover PDF has been distributed.</summary>
    Closed = 2
}