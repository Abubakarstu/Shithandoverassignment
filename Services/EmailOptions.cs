namespace ShiftHandover.Services;

public class EmailOptions
{
    public const string SectionName = "EmailSettings";

    /// <summary>Master switch for automatic distribution of handover reports.</summary>
    public bool Enabled { get; set; } = true;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = "no-reply@gulfair.test";

    public string FromDisplayName { get; set; } = "Shift Handover System";

    /// <summary>
    /// Extra static recipients (comma separated), e.g. the station manager or HSE inbox.
    /// </summary>
    public string? AdditionalRecipients { get; set; }

    /// <summary>
    /// When true every other active supervisor receives the report.
    /// When false only <see cref="AdditionalRecipients"/> are used.
    /// </summary>
    public bool NotifyAllActiveSupervisors { get; set; } = true;

    /// <summary>Seconds to wait for the SMTP server before falling back to the local outbox.</summary>
    public int TimeoutSeconds { get; set; } = 15;
}