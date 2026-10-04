namespace ShiftHandover.Helpers;

/// <summary>
/// Small formatting helpers used by views and the PDF template.
/// </summary>
public static class Display
{
    public static string ShiftWindow(Models.Entities.Shift shift) =>
        $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}";

    public static string Pretty(DateTime value) => value.ToString("dd MMM yyyy HH:mm");

    public static string PrettyDate(DateTime value) => value.ToString("dd MMM yyyy");

    public static string TimeOnly(DateTime value) => value.ToString("HH:mm");

    /// <summary>"NearMiss" -&gt; "Near Miss"</summary>
    public static string Humanise<TEnum>(TEnum value) where TEnum : struct, Enum =>
        SplitPascalCase(value.ToString());

    public static string SplitPascalCase(string value) =>
        string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));

    public static string Truncate(string? text, int maxLength) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Length <= maxLength ? text : text[..maxLength] + "...";
}