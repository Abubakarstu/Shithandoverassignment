using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Services;

public class EmailSendResult
{
    public DeliveryStatus Status { get; init; }

    public string? ErrorMessage { get; init; }

    public int RecipientCount { get; init; }

    public static EmailSendResult Sent(int recipients) => new() { Status = DeliveryStatus.Sent, RecipientCount = recipients };

    public static EmailSendResult Outboxed(int recipients, string note) => new()
    {
        Status = DeliveryStatus.QueuedToOutbox,
        ErrorMessage = note,
        RecipientCount = recipients
    };

    public static EmailSendResult Failed(string error, int recipients = 0) => new()
    {
        Status = DeliveryStatus.Failed,
        ErrorMessage = error,
        RecipientCount = recipients
    };
}

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        IReadOnlyCollection<string> recipients,
        string subject,
        string body,
        byte[]? attachment,
        string? attachmentFileName);
}

/// <summary>
/// Sends the handover report over SMTP. If SMTP is not configured or unreachable the
/// message plus its attachment are written to App_Data/EmailOutbox so nothing is lost
/// and the demo still runs end to end without a mail server.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly string _outboxPath;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailOptions> options,
        IOptions<StorageOptions> storageOptions,
        IWebHostEnvironment environment,
        ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
        _outboxPath = storageOptions.Value.Resolve(environment.ContentRootPath).OutboxFolder;
    }

    public async Task<EmailSendResult> SendAsync(
        IReadOnlyCollection<string> recipients,
        string subject,
        string body,
        byte[]? attachment,
        string? attachmentFileName)
    {
        var valid = recipients
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!_options.Enabled)
        {
            return WriteToOutbox(valid, subject, body, attachment, attachmentFileName,
                "Automatic e-mail is disabled in configuration (EmailSettings:Enabled=false).");
        }

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            return WriteToOutbox(valid, subject, body, attachment, attachmentFileName,
                "No SMTP host configured (EmailSettings:Host).");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var recipient in valid)
            {
                message.To.Add(recipient);
            }

            if (attachment is not null && !string.IsNullOrEmpty(attachmentFileName))
            {
                var stream = new MemoryStream(attachment);
                message.Attachments.Add(new Attachment(stream, attachmentFileName));
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Timeout = _options.TimeoutSeconds * 1000,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.UserName))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Handover report e-mail sent to {Recipients}.", string.Join(", ", valid));

            return EmailSendResult.Sent(valid.Count);
        }
        catch (SmtpFailedRecipientsException ex)
        {
            return EmailSendResult.Failed($"SMTP rejected the message: {ex.Message}", valid.Count);
        }
        catch (SmtpException ex)
        {
            _logger.LogWarning(ex, "SMTP delivery failed, falling back to the local outbox.");

            return WriteToOutbox(valid, subject, body, attachment, attachmentFileName,
                $"SMTP delivery failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "E-mail delivery failed, falling back to the local outbox.");

            return WriteToOutbox(valid, subject, body, attachment, attachmentFileName,
                $"Delivery failed: {ex.Message}");
        }
    }

    private EmailSendResult WriteToOutbox(
        IReadOnlyCollection<string> recipients,
        string subject,
        string body,
        byte[]? attachment,
        string? attachmentFileName,
        string note)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        var pdfPath = Path.Combine(_outboxPath, $"{stamp}_{(attachmentFileName ?? "report.pdf")}");
        var htmlPath = Path.Combine(_outboxPath, $"{stamp}.html");

        if (attachment is not null)
        {
            File.WriteAllBytes(pdfPath, attachment);
        }

        var toLine = string.Join(", ", recipients);
        var attachmentNote = attachment is null
            ? string.Empty
            : $"<p><b>Attachment:</b> {attachmentFileName} (saved next to this file: <code>{Path.GetFileName(pdfPath)}</code>)</p>";

        File.WriteAllText(htmlPath,
            $"<html><body><h3>{System.Net.WebUtility.HtmlEncode(subject)}</h3>" +
            $"<p><b>To:</b> {System.Net.WebUtility.HtmlEncode(toLine)}</p>" +
            body +
            attachmentNote +
            $"<hr><p style='color:#888'>NOT DELIVERED VIA SMTP - {System.Net.WebUtility.HtmlEncode(note)}</p>" +
            "</body></html>");

        _logger.LogInformation("E-mail written to local outbox ({Outbox}): {Note}", _outboxPath, note);

        return EmailSendResult.Outboxed(recipients.Count, note);
    }
}