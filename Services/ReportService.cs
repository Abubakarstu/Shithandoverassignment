using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShiftHandover.Data;
using ShiftHandover.Models.Enums;
using ShiftHandover.Models.Reports;
using ShiftHandover.Services;

namespace ShiftHandover.Services;

public interface IReportService
{
    Task<ShiftReportData> BuildReportDataAsync(int shiftId, string generatedByName);

    Task<byte[]> GeneratePdfAsync(ShiftReportData data);

    Task<byte[]> GeneratePdfAsync(int shiftId, string generatedByName);

    string BuildFileName(ShiftReportData data);
}

/// <summary>
/// Renders the shift handover report with QuestPDF.
/// </summary>
public class ReportService : IReportService
{
    private readonly ShiftHandoverContext _context;
    private readonly string _companyName;

    public ReportService(ShiftHandoverContext context, string companyName)
    {
        _context = context;
        _companyName = companyName;
    }

    public async Task<ShiftReportData> BuildReportDataAsync(int shiftId, string generatedByName)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.ClaimedBy)
            .Include(x => x.ClosedBy)
            .Include(x => x.Accidents).ThenInclude(x => x.LoggedBy)
            .Include(x => x.Incidents).ThenInclude(x => x.LoggedBy)
            .Include(x => x.ManpowerRecords).ThenInclude(x => x.LoggedBy)
            .FirstOrDefaultAsync(x => x.Id == shiftId)
            ?? throw new InvalidOperationException($"Shift {shiftId} not found.");

        return new ShiftReportData
        {
            Shift = shift,
            ClaimedBy = shift.ClaimedBy,
            ClosedBy = shift.ClosedBy,
            Accidents = shift.Accidents.OrderBy(x => x.OccurredAt).ToList(),
            Incidents = shift.Incidents.OrderBy(x => x.OccurredAt).ToList(),
            ManpowerRecords = shift.ManpowerRecords.OrderBy(x => x.FunctionName).ToList(),
            GeneratedByName = generatedByName,
            GeneratedAt = DateTime.Now
        };
    }

    public async Task<byte[]> GeneratePdfAsync(int shiftId, string generatedByName) =>
        await GeneratePdfAsync(await BuildReportDataAsync(shiftId, generatedByName));

    public Task<byte[]> GeneratePdfAsync(ShiftReportData data) =>
        Task.FromResult(ComposeDocument(data).GeneratePdf());

    public string BuildFileName(ShiftReportData data) =>
        $"Shift-Handover_{data.Shift.ShiftDate:yyyyMMdd}_{data.Shift.ShiftType}_{data.Shift.Id}.pdf";

    private Document ComposeDocument(ShiftReportData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Segoe UI"));

                page.Header().Element(c => ComposeHeader(c, data));
                page.Content().Element(c => ComposeBody(c, data));
                page.Footer().Element(ComposeFooter);
            });
        });
    }

    // ------------------------------------------------------------------ Header

    private void ComposeHeader(IContainer container, ShiftReportData data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_companyName).FontSize(9).FontColor(PdfStyles.TextMuted);
                    c.Item().Text("SHIFT HANDOVER REPORT").FontSize(18).Bold().FontColor(PdfStyles.Accent);
                });

                row.ConstantItem(190).AlignRight().Column(c =>
                {
                    c.Item().Text($"Report generated: {data.GeneratedAt:dd MMM yyyy HH:mm}")
                        .FontSize(8).FontColor(PdfStyles.TextDark);
                    c.Item().Text($"By: {data.GeneratedByName}").FontSize(8).FontColor(PdfStyles.TextDark);
                    c.Item().Text($"Status: {data.Shift.Status}")
                        .FontSize(8).SemiBold()
                        .FontColor(data.Shift.Status == ShiftStatus.Closed ? PdfStyles.Green : PdfStyles.Orange);
                });
            });

            column.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(PdfStyles.Accent);

            column.Item().PaddingTop(8).Element(c => ComposeShiftSummary(c, data));
        });
    }

    private void ComposeShiftSummary(IContainer container, ShiftReportData data)
    {
        container.Border(1).BorderColor(PdfStyles.Border).Padding(8).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "Date", data.Shift.ShiftDate.ToString("dd MMMM yyyy")));
                row.RelativeItem().Element(c => Field(c, "Shift", data.Shift.ShiftType));
                row.RelativeItem().Element(c => Field(c, "Shift window", data.ShiftWindow));
                row.RelativeItem().Element(c => Field(c, "Area", data.Shift.Area ?? "-"));
            });

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Element(c => Field(c, "Claimed by", data.ClaimedBy?.DisplayName ?? "-"));
                row.RelativeItem().Element(c => Field(c, "Claimed at", FormatTime(data.Shift.ClaimedAt)));
                row.RelativeItem().Element(c => Field(c, "Closed by", data.ClosedBy?.DisplayName ?? "-"));
                row.RelativeItem().Element(c => Field(c, "Closed at", FormatTime(data.Shift.ClosedAt)));
            });
        });
    }

    // ------------------------------------------------------------------ Body

    private void ComposeBody(IContainer container, ShiftReportData data)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(12);

            column.Item().Element(c => ComposeKpiStrip(c, data));

            column.Item().Element(c => SectionTitle(c, "1. Accidents",
                $"{data.AccidentCount} recorded | {data.PersonsInjured} injured | {data.ReportableAccidents} reportable"));
            column.Item().Element(c => ComposeAccidentsTable(c, data));

            column.Item().Element(c => SectionTitle(c, "2. Incidents",
                $"{data.IncidentCount} recorded | {data.EscalatedIncidents} escalated"));
            column.Item().Element(c => ComposeIncidentsTable(c, data));

            column.Item().Element(c => SectionTitle(c, "3. Manpower Details",
                $"{data.ManpowerRecords.Count} function(s) reported"));
            column.Item().Element(c => ComposeManpowerTable(c, data));

            column.Item().Element(c => SectionTitle(c, "4. Handover Notes",
                "To be actioned by the oncoming shift"));
            column.Item().Border(1).BorderColor(PdfStyles.Border).Padding(8).Column(c =>
            {
                c.Item().Text("Remarks").SemiBold().FontColor(PdfStyles.Accent);
                c.Item().Text(string.IsNullOrWhiteSpace(data.Shift.HandoverRemarks)
                    ? "No remarks recorded."
                    : data.Shift.HandoverRemarks);
                c.Item().PaddingTop(6).Text("Pending actions").SemiBold().FontColor(PdfStyles.Accent);
                c.Item().Text(string.IsNullOrWhiteSpace(data.Shift.PendingActions)
                    ? "No pending actions recorded."
                    : data.Shift.PendingActions);
            });

            column.Item().PaddingTop(12).Element(ComposeSignatures);
        });
    }

    private void ComposeAccidentsTable(IContainer container, ShiftReportData data)
    {
        if (data.Accidents.Count == 0)
        {
            EmptyState(container, "No accidents were recorded during this shift.");
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(56);   // Time
                cols.ConstantColumn(62);   // Type
                cols.ConstantColumn(52);   // Severity
                cols.ConstantColumn(76);   // Location
                cols.RelativeColumn();     // Description / action
                cols.ConstantColumn(44);   // Injured
            });

            table.Cell().HeaderCell().Text("Time").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Type").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Severity").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Location").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Description / immediate action").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Injured").SemiBold().FontColor(PdfStyles.White);

            foreach (var accident in data.Accidents)
            {
                table.Cell().BodyCell().Text(accident.OccurredAt.ToString("HH:mm"));
                table.Cell().BodyCell().Text(Friendly(accident.AccidentType.ToString()));
                table.Cell().BodyCell().Text(Friendly(accident.Severity.ToString()))
                      .FontColor(PdfStyles.SeverityColor(accident.Severity));
                table.Cell().BodyCell().Text(accident.Location);
                table.Cell().BodyCell().Column(c =>
                {
                    c.Item().Text(accident.Description);
                    c.Item().Text($"Action taken: {accident.ImmediateActionTaken}")
                        .FontColor(PdfStyles.TextDark).SemiBold();
                    c.Item().Text($"Status: {Friendly(accident.InvestigationStatus.ToString())}" +
                                  (accident.IsReportable ? "   |   REPORTABLE TO REGULATOR" : string.Empty))
                        .FontSize(8).SemiBold()
                        .FontColor(accident.IsReportable ? PdfStyles.Red : PdfStyles.TextMuted);
                });
                table.Cell().BodyCell().Text(accident.PersonsInjured.ToString());
            }
        });
    }

    private void ComposeIncidentsTable(IContainer container, ShiftReportData data)
    {
        if (data.Incidents.Count == 0)
        {
            EmptyState(container, "No incidents were recorded during this shift.");
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(56);
                cols.ConstantColumn(66);
                cols.ConstantColumn(52);
                cols.ConstantColumn(76);
                cols.RelativeColumn();
            });

            table.Cell().HeaderCell().Text("Time").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Category").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Severity").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Location").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Description / action taken").SemiBold().FontColor(PdfStyles.White);

            foreach (var incident in data.Incidents)
            {
                table.Cell().BodyCell().Text(incident.OccurredAt.ToString("HH:mm"));
                table.Cell().BodyCell().Text(Friendly(incident.Category.ToString()));
                table.Cell().BodyCell().Text(Friendly(incident.Severity.ToString()))
                      .FontColor(PdfStyles.SeverityColor(incident.Severity));
                table.Cell().BodyCell().Text(incident.Location);
                table.Cell().BodyCell().Column(c =>
                {
                    c.Item().Text(incident.Description);
                    c.Item().Text($"Action taken: {incident.ActionTaken}")
                        .FontColor(PdfStyles.TextDark).SemiBold();
                    c.Item().Text($"Status: {Friendly(incident.InvestigationStatus.ToString())}" +
                                  (incident.IsEscalated ? $"   |   ESCALATED TO {incident.EscalatedTo}" : string.Empty))
                        .FontSize(8).SemiBold()
                        .FontColor(incident.IsEscalated ? PdfStyles.Orange : PdfStyles.TextMuted);
                });
            }
        });
    }

    private void ComposeManpowerTable(IContainer container, ShiftReportData data)
    {
        if (data.ManpowerRecords.Count == 0)
        {
            EmptyState(container, "No manpower details were recorded during this shift.");
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.ConstantColumn(50);
                cols.ConstantColumn(50);
                cols.ConstantColumn(50);
                cols.ConstantColumn(46);
                cols.ConstantColumn(52);
                cols.RelativeColumn(2);
            });

            table.Cell().HeaderCell().Text("Function / Area").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Planned").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("On Duty").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("On Leave").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Absent").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Overtime").SemiBold().FontColor(PdfStyles.White);
            table.Cell().HeaderCell().Text("Remarks").SemiBold().FontColor(PdfStyles.White);

            foreach (var m in data.ManpowerRecords)
            {
                table.Cell().BodyCell().Text(m.FunctionName);
                table.Cell().BodyCell().AlignCenter().Text(m.Planned.ToString());
                table.Cell().BodyCell().AlignCenter().Text(m.OnDuty.ToString()).SemiBold()
                      .FontColor(m.OnDuty < m.Planned ? PdfStyles.Red : PdfStyles.Green);
                table.Cell().BodyCell().AlignCenter().Text(m.OnLeave.ToString());
                table.Cell().BodyCell().AlignCenter().Text(m.Absent.ToString());
                table.Cell().BodyCell().AlignCenter().Text(m.Overtime.ToString());
                table.Cell().BodyCell().Text(m.Remarks ?? "-");
            }

            table.Cell().TotalCell().Text("TOTAL").SemiBold();
            table.Cell().TotalCell().AlignCenter().Text(data.TotalPlanned.ToString()).SemiBold();
            table.Cell().TotalCell().AlignCenter().Text(data.TotalOnDuty.ToString()).SemiBold();
            table.Cell().TotalCell().AlignCenter().Text(data.TotalOnLeave.ToString()).SemiBold();
            table.Cell().TotalCell().AlignCenter().Text(data.TotalAbsent.ToString()).SemiBold();
            table.Cell().TotalCell().AlignCenter().Text(data.TotalOvertime.ToString()).SemiBold();
            table.Cell().TotalCell()
                 .Text($"Shortfall: {Math.Max(0, data.TotalPlanned - data.TotalOnDuty)}").SemiBold();
        });
    }

    // ------------------------------------------------------------------ KPI strip

    private void ComposeKpiStrip(IContainer container, ShiftReportData data)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => Kpi(c, "Accidents", data.AccidentCount.ToString(),
                data.AccidentCount > 0 ? PdfStyles.Red : PdfStyles.Green));
            row.ConstantItem(8);
            row.RelativeItem().Element(c => Kpi(c, "Incidents", data.IncidentCount.ToString(), PdfStyles.Orange));
            row.ConstantItem(8);
            row.RelativeItem().Element(c => Kpi(c, "Manpower on duty", $"{data.TotalOnDuty}/{data.TotalPlanned}",
                data.TotalOnDuty >= data.TotalPlanned ? PdfStyles.Green : PdfStyles.Red));
            row.ConstantItem(8);
            row.RelativeItem().Element(c => Kpi(c, "Absent", data.TotalAbsent.ToString(),
                data.TotalAbsent > 0 ? PdfStyles.Red : PdfStyles.Green));
            row.ConstantItem(8);
            row.RelativeItem().Element(c => Kpi(c, "Overtime", data.TotalOvertime.ToString(), PdfStyles.Accent));
        });
    }

    private void Kpi(IContainer container, string label, string value, string color)
    {
        container.Border(1).BorderColor(PdfStyles.Border).Background(PdfStyles.RowFill).Padding(7).Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontSize(7).FontColor(PdfStyles.TextDark);
            c.Item().Text(value).FontSize(15).Bold().FontColor(color);
        });
    }

    // ------------------------------------------------------------------ Small helpers

    private static void Field(IContainer container, string label, string value)
    {
        container.Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontSize(7).FontColor(PdfStyles.TextMuted);
            c.Item().Text(value).FontSize(9).SemiBold();
        });
    }

    private static void SectionTitle(IContainer container, string title, string subtitle)
    {
        container.PaddingBottom(5).Row(row =>
        {
            row.RelativeItem().Text(title).FontSize(12).Bold().FontColor(PdfStyles.Accent);
            row.ConstantItem(250).AlignRight().Text(subtitle).FontSize(8).FontColor(PdfStyles.TextMuted);
        });
    }

    private static void EmptyState(IContainer container, string message)
    {
        container.Border(1).BorderColor(PdfStyles.Border).Background(PdfStyles.RowFill)
            .Padding(10)
            .Text(message)
            .Italic()
            .FontColor(PdfStyles.TextDark);
    }

    private static void ComposeSignatures(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => SignatureBox(c, "Outgoing Supervisor"));
            row.ConstantItem(20);
            row.RelativeItem().Element(c => SignatureBox(c, "Incoming Supervisor"));
            row.ConstantItem(20);
            row.RelativeItem().Element(c => SignatureBox(c, "Safety / HSE Review"));
        });
    }

    private static void SignatureBox(IContainer container, string title)
    {
        container.Column(c =>
        {
            c.Item().BorderTop(1).BorderColor(PdfStyles.TextDark).PaddingTop(3)
                .Text(title).FontSize(8).FontColor(PdfStyles.TextDark);
            c.Item().Text("Name / Signature / Date").FontSize(7).FontColor(PdfStyles.TextMuted);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(PdfStyles.Border).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Shift Handover System - generated automatically on shift closure. ")
                    .FontSize(7).FontColor(PdfStyles.TextMuted);
                text.CurrentPageNumber().FontSize(7).FontColor(PdfStyles.TextMuted);
                text.Span(" / ").FontSize(7).FontColor(PdfStyles.TextMuted);
                text.TotalPages().FontSize(7).FontColor(PdfStyles.TextMuted);
            });

            row.ConstantItem(170).AlignRight()
                .Text("Confidential - Airport Operations").FontSize(7).FontColor(PdfStyles.TextMuted);
        });
    }

    private static string FormatTime(DateTime? value) =>
        value?.ToString("dd MMM yyyy HH:mm") ?? "-";

    private static string Friendly(string pascalCase) =>
        string.Concat(pascalCase.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}