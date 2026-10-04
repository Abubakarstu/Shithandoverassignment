namespace ShiftHandover.Services;

/// <summary>
/// Filesystem locations for generated PDFs and for e-mails that could not be
/// delivered over SMTP.
/// </summary>
/// <remarks>
/// Both default to a folder inside the application (<c>App_Data</c>), which is what a
/// single-instance deployment on Windows hosting needs. Set <c>Storage:Root</c> to an
/// absolute path to move them somewhere else when the web application directory is
/// read-only, or to a mapped network drive when reports must be shared between servers.
/// </remarks>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Absolute path that holds the report and outbox folders. When null, or when the
    /// value is not an absolute path, the folders are created under
    /// <c>{ContentRoot}/App_Data</c>.
    /// </summary>
    public string? Root { get; set; }

    public string ReportFolderName { get; set; } = "Reports";

    public string OutboxFolderName { get; set; } = "EmailOutbox";

    /// <summary>
    /// Resolves the folder paths against a content root and creates them on demand.
    /// </summary>
    public (string ReportFolder, string OutboxFolder) Resolve(string contentRoot)
    {
        var root = string.IsNullOrWhiteSpace(Root) || !Path.IsPathRooted(Root)
            ? Path.Combine(contentRoot, "App_Data")
            : Root!;

        var reports = Path.Combine(root, ReportFolderName);
        var outbox = Path.Combine(root, OutboxFolderName);

        Directory.CreateDirectory(reports);
        Directory.CreateDirectory(outbox);

        return (reports, outbox);
    }
}