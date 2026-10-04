namespace ShiftHandover.Models.Enums;

/// <summary>
/// Shared severity scale used by accidents and incidents so both can be filtered/sorted consistently.
/// </summary>
public enum SeverityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}