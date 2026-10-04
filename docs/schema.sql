IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityName] nvarchar(60) NOT NULL,
        [EntityId] int NULL,
        [Action] nvarchar(60) NOT NULL,
        [Details] nvarchar(1000) NULL,
        [UserId] int NULL,
        [UserName] nvarchar(150) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [Supervisors] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(120) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [EmployeeCode] nvarchar(30) NOT NULL,
        [Department] nvarchar(20) NOT NULL,
        [Role] int NOT NULL,
        [IsActive] bit NOT NULL,
        [PasswordHash] nvarchar(200) NOT NULL,
        [PasswordSalt] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Supervisors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [Shifts] (
        [Id] int NOT NULL IDENTITY,
        [ShiftDate] date NOT NULL,
        [ShiftType] nvarchar(450) NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [Area] nvarchar(120) NULL,
        [Remarks] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [ClaimedById] int NULL,
        [ClaimedAt] datetime2 NULL,
        [ClosedById] int NULL,
        [ClosedAt] datetime2 NULL,
        [HandoverRemarks] nvarchar(2000) NULL,
        [PendingActions] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Shifts_Supervisors_ClaimedById] FOREIGN KEY ([ClaimedById]) REFERENCES [Supervisors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Shifts_Supervisors_ClosedById] FOREIGN KEY ([ClosedById]) REFERENCES [Supervisors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [Accidents] (
        [Id] int NOT NULL IDENTITY,
        [ShiftId] int NOT NULL,
        [LoggedById] int NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [AccidentType] nvarchar(max) NOT NULL,
        [Severity] nvarchar(max) NOT NULL,
        [Location] nvarchar(150) NOT NULL,
        [PersonsInjured] int NOT NULL,
        [PersonsInvolved] nvarchar(150) NULL,
        [InjuryType] nvarchar(150) NULL,
        [Description] nvarchar(2000) NOT NULL,
        [ImmediateActionTaken] nvarchar(2000) NOT NULL,
        [ReportedTo] nvarchar(150) NULL,
        [InvestigationStatus] nvarchar(max) NOT NULL,
        [IsReportable] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Accidents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Accidents_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Accidents_Supervisors_LoggedById] FOREIGN KEY ([LoggedById]) REFERENCES [Supervisors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [Incidents] (
        [Id] int NOT NULL IDENTITY,
        [ShiftId] int NOT NULL,
        [LoggedById] int NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Severity] nvarchar(max) NOT NULL,
        [InvestigationStatus] nvarchar(max) NOT NULL,
        [Location] nvarchar(150) NOT NULL,
        [ReferenceNumber] nvarchar(150) NULL,
        [Description] nvarchar(2000) NOT NULL,
        [ActionTaken] nvarchar(2000) NOT NULL,
        [EscalatedTo] nvarchar(150) NULL,
        [IsEscalated] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Incidents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Incidents_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Incidents_Supervisors_LoggedById] FOREIGN KEY ([LoggedById]) REFERENCES [Supervisors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [ManpowerRecords] (
        [Id] int NOT NULL IDENTITY,
        [ShiftId] int NOT NULL,
        [LoggedById] int NOT NULL,
        [FunctionName] nvarchar(120) NOT NULL,
        [Planned] int NOT NULL,
        [OnDuty] int NOT NULL,
        [OnLeave] int NOT NULL,
        [Absent] int NOT NULL,
        [Overtime] int NOT NULL,
        [Remarks] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ManpowerRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ManpowerRecords_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ManpowerRecords_Supervisors_LoggedById] FOREIGN KEY ([LoggedById]) REFERENCES [Supervisors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [ShiftReports] (
        [Id] int NOT NULL IDENTITY,
        [ShiftId] int NOT NULL,
        [GeneratedById] int NOT NULL,
        [GeneratedAt] datetime2 NOT NULL,
        [FileName] nvarchar(300) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [AccidentCount] int NOT NULL,
        [IncidentCount] int NOT NULL,
        [ManpowerRowCount] int NOT NULL,
        [EmailedSuccessfully] bit NOT NULL,
        CONSTRAINT [PK_ShiftReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShiftReports_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ShiftReports_Supervisors_GeneratedById] FOREIGN KEY ([GeneratedById]) REFERENCES [Supervisors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE TABLE [EmailLogs] (
        [Id] int NOT NULL IDENTITY,
        [ShiftId] int NOT NULL,
        [ShiftReportId] int NULL,
        [ToAddresses] nvarchar(300) NOT NULL,
        [Subject] nvarchar(300) NOT NULL,
        [Body] nvarchar(2000) NULL,
        [AttachmentFileName] nvarchar(300) NULL,
        [Status] nvarchar(max) NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SentAt] datetime2 NULL,
        CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailLogs_ShiftReports_ShiftReportId] FOREIGN KEY ([ShiftReportId]) REFERENCES [ShiftReports] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_EmailLogs_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Accidents_LoggedById] ON [Accidents] ([LoggedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Accidents_ShiftId_OccurredAt] ON [Accidents] ([ShiftId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_ShiftId_CreatedAt] ON [EmailLogs] ([ShiftId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmailLogs_ShiftReportId] ON [EmailLogs] ([ShiftReportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Incidents_LoggedById] ON [Incidents] ([LoggedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Incidents_ShiftId_OccurredAt] ON [Incidents] ([ShiftId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ManpowerRecords_LoggedById] ON [ManpowerRecords] ([LoggedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Manpower_Shift_Function] ON [ManpowerRecords] ([ShiftId], [FunctionName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftReports_GeneratedById] ON [ShiftReports] ([GeneratedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftReports_ShiftId] ON [ShiftReports] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shifts_ClaimedById] ON [Shifts] ([ClaimedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shifts_ClosedById] ON [Shifts] ([ClosedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shifts_Status_ShiftDate] ON [Shifts] ([Status], [ShiftDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Shifts_ShiftDate_ShiftType] ON [Shifts] ([ShiftDate], [ShiftType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Supervisors_Email] ON [Supervisors] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Supervisors_EmployeeCode] ON [Supervisors] ([EmployeeCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004171712_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004171712_InitialCreate', N'9.0.0');
END;

COMMIT;
GO

