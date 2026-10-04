namespace ShiftHandover.Helpers;

/// <summary>
/// Single source of truth for the three duty slots used when generating the roster.
/// </summary>
public static class ShiftTypes
{
    public const string Morning = "Morning";
    public const string Afternoon = "Afternoon";
    public const string Night = "Night";

    public static IReadOnlyList<string> All { get; } = new[] { Morning, Afternoon, Night };

    public static IReadOnlyList<(string Type, TimeSpan Start, TimeSpan End)> Definitions { get; } = new[]
    {
        (Morning, new TimeSpan(6, 0, 0), new TimeSpan(14, 0, 0)),
        (Afternoon, new TimeSpan(14, 0, 0), new TimeSpan(22, 0, 0)),
        (Night, new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0))
    };

    public static (string Type, TimeSpan Start, TimeSpan End) Get(string type) =>
        Definitions.First(x => x.Type == type);
}