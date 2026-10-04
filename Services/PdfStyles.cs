using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace ShiftHandover.Services;

/// <summary>
/// Shared colours and cell styling for the handover PDF.
/// Colours are plain hex strings so the template does not depend on a specific
/// version of the QuestPDF colour palette.
/// </summary>
public static class PdfStyles
{
    public const string Accent = "#0F4C81";
    public const string AccentSoft = "#E8EFF7";
    public const string RowFill = "#F2F4F7";
    public const string Border = "#DEE2E6";
    public const string TextMuted = "#6C757D";
    public const string TextDark = "#343A40";
    public const string Red = "#C1121F";
    public const string Orange = "#D9480F";
    public const string Green = "#2B8A3E";
    public const string White = "#FFFFFF";

    public static string SeverityColor(Models.Enums.SeverityLevel severity) => severity switch
    {
        Models.Enums.SeverityLevel.Critical => Red,
        Models.Enums.SeverityLevel.High => Orange,
        Models.Enums.SeverityLevel.Medium => TextDark,
        _ => TextMuted
    };

    /// <summary>Styles a table header cell (accent background, white bold text follows).</summary>
    public static IContainer HeaderCell(this IContainer container) =>
        container.Background(Accent).PaddingVertical(4).PaddingHorizontal(5);

    /// <summary>Styles a normal body cell.</summary>
    public static IContainer BodyCell(this IContainer container) =>
        container.BorderBottom(1).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(5);

    /// <summary>Styles a totals row cell.</summary>
    public static IContainer TotalCell(this IContainer container) =>
        container.Background(AccentSoft).BorderBottom(1).BorderColor(Border).PaddingVertical(4).PaddingHorizontal(5);
}