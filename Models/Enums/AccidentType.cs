namespace ShiftHandover.Models.Enums;

public enum AccidentType
{
    /// <summary>Near miss - no injury but could have caused one.</summary>
    NearMiss = 0,

    /// <summary>First aid case, no lost time.</summary>
    Minor = 1,

    /// <summary>Lost time / recordable injury.</summary>
    Recordable = 2,

    /// <summary>Fatality.</summary>
    Fatal = 3,

    /// <summary>Property or equipment damage only.</summary>
    PropertyDamage = 4
}