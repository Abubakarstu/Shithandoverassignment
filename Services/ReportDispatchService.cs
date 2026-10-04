using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShiftHandover.Data;
using ShiftHandover.Models.Entities;
using ShiftHandover.Models.Enums;
using ShiftHandover.Services;

namespace ShiftHandover.Services;

public class ReportDispatchResult
{
    public required ShiftReport Report { get; init; }

    public required IReadOnlyList<EmailLog> Emails { get; init; }

    public required EmailSendResult Delivery { get; init; }

    public IReadOnlyList<string> Recipients { get; init; } = Array.Empty<string>();
}

public interface IReportDispatchService
{
    /// <summary>
    /// Generates the handover PDF for a closed shift, stores it, then e-mails it
    /// to the other supervisors. Safe to call more than once (e.g. a manual resend).
    /// </summary>
    Task<ReportDispatchResult> GenerateAndDistributeAsync(int shiftId, int supervisorId);
}

/// <summary>
/// Orchestrates the "close shift -&gt; PDF -&gt; e-mail" workflow.
/// </summary>
public class ReportDispatchService : IReportDispatchService
{
    private readonly ShiftHandoverContext _context;
    private readonly IReportService _reportService;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly string _reportFolder;
    private readonly ILogger<ReportDispatchService> _logger;

    public ReportDispatchService(
        ShiftHandoverContext context,
        IReportService reportService,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        IOptions<StorageOptions> storageOptions,
        IWebHostEnvironment environment,
        ILogger<ReportDispatchService> logger)
    {
        _context = context;
        _reportService = reportService;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
        _reportFolder = storageOptions.Value.Resolve(environment.ContentRootPath).ReportFolder;
    }

    public async Task<ReportDispatchResult> GenerateAndDistributeAsync(int shiftId, int supervisorId)
    {
        var supervisor = await _context.Supervisors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == supervisorId)
            ?? throw new InvalidOperationException($"Supervisor {supervisorId} not found.");

        var data = await _reportService.BuildReportDataAsync(shiftId, supervisor.DisplayName);

        // 1. Render the PDF.
        var pdfBytes = await _reportService.GeneratePdfAsync(data);
        var fileName = _reportService.BuildFileName(data);
        var filePath = Path.Combine(_reportFolder, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);

        // 2. Record the generated report.
        var report = new ShiftReport
        {
            ShiftId = shiftId,
            GeneratedById = supervisorId,
            GeneratedAt = DateTime.Now,
            FileName = fileName,
            FilePath = filePath,
            AccidentCount = data.AccidentCount,
            IncidentCount = data.IncidentCount,
            ManpowerRowCount = data.ManpowerRecords.Count
        };

        _context.ShiftReports.Add(report);
        await _context.SaveChangesAsync();

        // 3. Work out who should receive it: every OTHER active supervisor.
        var recipients = await ResolveRecipientsAsync(supervisorId);

        // 4. Send the e-mail with the PDF attached.
        var subject = $"[Shift Handover] {data.ShiftLabel} - {data.ShiftWindow}";
        var body = BuildEmailBody(data, supervisor);

        var delivery = recipients.Count == 0
            ? EmailSendResult.Outboxed(0, "No recipients available.")
            : await _emailSender.SendAsync(recipients, subject, body, pdfBytes, fileName);

        var log = new EmailLog
        {
            ShiftId = shiftId,
            ShiftReportId = report.Id,
            ToAddresses = string.Join(", ", recipients),
            Subject = subject,
            Body = body,
            AttachmentFileName = fileName,
            Status = delivery.Status,
            ErrorMessage = delivery.ErrorMessage,
            CreatedAt = DateTime.Now,
            SentAt = delivery.Status == DeliveryStatus.Sent ? DateTime.Now : null
        };

        _context.EmailLogs.Add(log);

        report.EmailedSuccessfully = delivery.Status == DeliveryStatus.Sent && recipients.Count > 0;

        await _context.SaveChangesAsync();

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ShiftReport",
            EntityId = report.Id,
            Action = "GenerateAndDistribute",
            Details = $"PDF '{fileName}' generated and e-mailed to {recipients.Count} recipient(s) [{delivery.Status}].",
            UserId = supervisorId,
            UserName = supervisor.FullName
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Shift {ShiftId} closed: report {FileName} dispatched to {Count} recipient(s) ({Status}).",
            shiftId, fileName, recipients.Count, delivery.Status);

        return new ReportDispatchResult
        {
            Report = report,
            Emails = new[] { log },
            Delivery = delivery,
            Recipients = recipients
        };
    }

    private async Task<List<string>> ResolveRecipientsAsync(int senderSupervisorId)
    {
        var recipients = new List<string>();

        if (_emailOptions.NotifyAllActiveSupervisors)
        {
            var others = await _context.Supervisors
                .AsNoTracking()
                .Where(x => x.IsActive && x.Id != senderSupervisorId)
                .Select(x => x.Email)
                .ToListAsync();

            recipients.AddRange(others);
        }

        if (!string.IsNullOrWhiteSpace(_emailOptions.AdditionalRecipients))
        {
            recipients.AddRange(_emailOptions.AdditionalRecipients
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return recipients.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BuildEmailBody(Models.Reports.ShiftReportData data, Supervisor sender)
    {
        var rows = new System.Text.StringBuilder();

        rows.Append("<div style='font-family:Segoe UI,Arial,sans-serif;font-size:13px;color:#222'>");
        rows.Append($"<p>Dear Supervisor,</p>");
        rows.Append($"<p>The following shift has been completed and closed. The full handover report is attached as a PDF.</p>");

        rows.Append("<table cellpadding='6' cellspacing='0' border='0' style='border-collapse:collapse;background:#f2f4f7'>");
        AppendRow(rows, "Shift", data.ShiftLabel);
        AppendRow(rows, "Shift window", data.ShiftWindow);
        AppendRow(rows, "Area", data.Shift.Area ?? "-");
        AppendRow(rows, "Closed by", $"{sender.FullName} ({sender.EmployeeCode})");
        AppendRow(rows, "Closed at", data.Shift.ClosedAt?.ToString("dd MMM yyyy HH:mm") ?? DateTime.Now.ToString("dd MMM yyyy HH:mm"));
        AppendRow(rows, "Accidents", $"{data.AccidentCount} ({data.ReportableAccidents} reportable, {data.PersonsInjured} injured)");
        AppendRow(rows, "Incidents", $"{data.IncidentCount} ({data.EscalatedIncidents} escalated)");
        AppendRow(rows, "Manpower on duty", $"{data.TotalOnDuty} of {data.TotalPlanned} planned");
        rows.Append("</table>");

        if (data.Accidents.Any(x => x.IsReportable))
        {
            rows.Append("<p style='color:#b00020'><b>Attention:</b> this shift contains a reportable accident that requires HSE follow-up.</p>");
        }

        if (!string.IsNullOrWhiteSpace(data.Shift.PendingActions))
        {
            rows.Append("<p><b>Pending actions for the next shift:</b><br/>");
            rows.Append(System.Net.WebUtility.HtmlEncode(data.Shift.PendingActions).Replace("\n", "<br/>"));
            rows.Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(data.Shift.HandoverRemarks))
        {
            rows.Append("<p><b>Handover remarks:</b><br/>");
            rows.Append(System.Net.WebUtility.HtmlEncode(data.Shift.HandoverRemarks).Replace("\n", "<br/>"));
            rows.Append("</p>");
        }

        rows.Append("<p style='color:#666;font-size:11px'>This e-mail was generated automatically by the Shift Handover System.</p>");
        rows.Append("</div>");

        return rows.ToString();
    }

    private static void AppendRow(System.Text.StringBuilder sb, string label, string value)
    {
        sb.Append("<tr>");
        sb.Append("<td style='font-weight:bold;white-space:nowrap'>&nbsp;");
        sb.Append(System.Net.WebUtility.HtmlEncode(label));
        sb.Append("&nbsp;</td><td>");
        sb.Append(System.Net.WebUtility.HtmlEncode(value));
        sb.Append("</td></tr>");
    }
}