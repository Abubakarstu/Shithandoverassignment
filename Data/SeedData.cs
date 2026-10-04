using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Models.Entities;
using ShiftHandover.Models.Enums;
using ShiftHandover.Services;

namespace ShiftHandover.Data;

public static class SeedData
{
    public const string DefaultPassword = "Pass@123";

    private static readonly (string Name, string Email, string Code, string Dept, SupervisorRole Role)[] Supervisors =
    {
        ("Sara Al Mansoori",  "sara.almansoori@gulfair.test",     "SUP-1001", "Ramp Operations",   SupervisorRole.Admin),
        ("Mohammed Al Zaabi", "mohammed.alzaabi@gulfair.test",    "SUP-1002", "Terminal Operations", SupervisorRole.Supervisor),
        ("Fatima Al Blooshi", "fatima.alblooshi@gulfair.test",    "SUP-1003", "Safety & HSE",      SupervisorRole.Supervisor),
        ("Omar Al Suwaidi",   "omar.alsuwaidi@gulfair.test",      "SUP-1004", "Baggage Services",  SupervisorRole.Supervisor)
    };

    private static readonly (string Type, TimeSpan Start, TimeSpan End)[] ShiftDefinitions =
    {
        ("Morning",   new TimeSpan(6, 0, 0),  new TimeSpan(14, 0, 0)),
        ("Afternoon", new TimeSpan(14, 0, 0), new TimeSpan(22, 0, 0)),
        ("Night",     new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0))
    };

    public static async Task SeedAsync(ShiftHandoverContext context, string companyName, string reportFolder)
    {
        Directory.CreateDirectory(reportFolder);

        if (await context.Supervisors.AnyAsync())
        {
            return; // Already seeded.
        }

        var users = new List<Supervisor>();

        foreach (var template in Supervisors)
        {
            var (hash, salt) = PasswordHasher.Hash(DefaultPassword);

            users.Add(new Supervisor
            {
                FullName = template.Name,
                Email = template.Email,
                EmployeeCode = template.Code,
                Department = template.Dept,
                Role = template.Role,
                IsActive = true,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            });
        }

        context.Supervisors.AddRange(users);
        await context.SaveChangesAsync();

        var sara = users.First(x => x.Email == "sara.almansoori@gulfair.test");
        var mohammed = users.First(x => x.Email == "mohammed.alzaabi@gulfair.test");
        var fatima = users.First(x => x.Email == "fatima.alblooshi@gulfair.test");

        // ---- One fully worked example: yesterday's morning shift, closed, with a generated PDF ----
        var yesterday = DateTime.Today.AddDays(-1);

        var closedShift = new Shift
        {
            ShiftDate = yesterday,
            ShiftType = "Morning",
            StartTime = new TimeSpan(6, 0, 0),
            EndTime = new TimeSpan(14, 0, 0),
            Area = "Terminal 1",
            Remarks = "Standard operations",
            Status = ShiftStatus.Closed,
            ClaimedById = mohammed.Id,
            ClaimedAt = yesterday.Date.AddHours(5).AddMinutes(45),
            ClosedById = mohammed.Id,
            ClosedAt = yesterday.Date.AddHours(14).AddMinutes(20),
            HandoverRemarks = "Ramp fully operational. Two stands under maintenance, both back in service by 11:00.",
            PendingActions = "Follow up with engineering on the belt loader hydraulic leak (WO-44182).",
            CreatedAt = DateTime.UtcNow
        };

        context.Shifts.Add(closedShift);
        await context.SaveChangesAsync();

        context.Accidents.AddRange(
            new Accident
            {
                ShiftId = closedShift.Id,
                LoggedById = mohammed.Id,
                OccurredAt = yesterday.Date.AddHours(7).AddMinutes(15),
                AccidentType = AccidentType.NearMiss,
                Severity = SeverityLevel.Medium,
                Location = "Stand 12 - Ramp",
                PersonsInjured = 0,
                PersonsInvolved = "Ramp agent (2)",
                InjuryType = "None - near miss",
                Description = "Ramp agent stepped into the engine intake arc while the A320 was still running engines; cleared by marshaller.",
                ImmediateActionTaken = "Chocks and cones re-confirmed, marshaller posted, agent re-briefed on PPE and stand safety zones.",
                ReportedTo = "Safety & HSE",
                InvestigationStatus = InvestigationStatus.Closed,
                IsReportable = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Accident
            {
                ShiftId = closedShift.Id,
                LoggedById = mohammed.Id,
                OccurredAt = yesterday.Date.AddHours(10).AddMinutes(40),
                AccidentType = AccidentType.Minor,
                Severity = SeverityLevel.Low,
                Location = "Terminal 1 - Baggage Hall",
                PersonsInjured = 1,
                PersonsInvolved = "Belt loader operator",
                InjuryType = "Minor abrasion - left forearm",
                Description = "Slipped while stepping down from the belt loader platform; grazed forearm on the handrail.",
                ImmediateActionTaken = "First aid administered, area cleaned and de-wetted, operator stood down for the remainder of the shift.",
                ReportedTo = "Safety & HSE",
                InvestigationStatus = InvestigationStatus.UnderInvestigation,
                IsReportable = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        context.Incidents.AddRange(
            new Incident
            {
                ShiftId = closedShift.Id,
                LoggedById = fatima.Id,
                OccurredAt = yesterday.Date.AddHours(8).AddMinutes(5),
                Category = IncidentCategory.Technical,
                Severity = SeverityLevel.Medium,
                InvestigationStatus = InvestigationStatus.UnderInvestigation,
                Location = "Terminal 1 - Check-in Row C",
                ReferenceNumber = "INC-2026-1143",
                Description = "Check-in desks C14-C18 offline for 18 minutes after a baggage system integration failure.",
                ActionTaken = "Passengers re-bridged to desks A05-A11, vendor ticket raised, manual boarding list printed for 6 flights.",
                EscalatedTo = "IT Operations Manager",
                IsEscalated = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Incident
            {
                ShiftId = closedShift.Id,
                LoggedById = mohammed.Id,
                OccurredAt = yesterday.Date.AddHours(12).AddMinutes(30),
                Category = IncidentCategory.Security,
                Severity = SeverityLevel.Low,
                InvestigationStatus = InvestigationStatus.Closed,
                Location = "Landside - Kerbside Zone C",
                ReferenceNumber = "SEC-2026-0455",
                Description = "Unattended item reported at kerbside; false alarm - child's pram left by a passenger.",
                ActionTaken = "Item checked by security, passenger located, incident closed after cordon removal.",
                EscalatedTo = null,
                IsEscalated = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        context.ManpowerRecords.AddRange(
            new ManpowerRecord
            {
                ShiftId = closedShift.Id,
                LoggedById = mohammed.Id,
                FunctionName = "Ramp Operations",
                Planned = 24,
                OnDuty = 22,
                OnLeave = 2,
                Absent = 0,
                Overtime = 3,
                Remarks = "Two staff reassigned to stand closure at 09:00.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManpowerRecord
            {
                ShiftId = closedShift.Id,
                LoggedById = mohammed.Id,
                FunctionName = "Check-in & Boarding",
                Planned = 18,
                OnDuty = 18,
                OnLeave = 0,
                Absent = 0,
                Overtime = 2,
                Remarks = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManpowerRecord
            {
                ShiftId = closedShift.Id,
                LoggedById = fatima.Id,
                FunctionName = "Baggage Services",
                Planned = 30,
                OnDuty = 27,
                OnLeave = 1,
                Absent = 2,
                Overtime = 0,
                Remarks = "Two absences covered by overtime from the previous roster.",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManpowerRecord
            {
                ShiftId = closedShift.Id,
                LoggedById = fatima.Id,
                FunctionName = "Safety & Fire",
                Planned = 6,
                OnDuty = 6,
                OnLeave = 0,
                Absent = 0,
                Overtime = 0,
                Remarks = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        // Render the PDF for the example shift so the Reports screen is not empty.
        try
        {
            var reportService = new ReportService(context, companyName);
            var reportData = await reportService.BuildReportDataAsync(closedShift.Id, mohammed.DisplayName);
            var pdfBytes = await reportService.GeneratePdfAsync(reportData);
            var fileName = reportService.BuildFileName(reportData);
            var filePath = Path.Combine(reportFolder, fileName);

            await File.WriteAllBytesAsync(filePath, pdfBytes);

            context.ShiftReports.Add(new ShiftReport
            {
                ShiftId = closedShift.Id,
                GeneratedById = mohammed.Id,
                GeneratedAt = yesterday.Date.AddHours(14).AddMinutes(21),
                FileName = fileName,
                FilePath = filePath,
                AccidentCount = reportData.AccidentCount,
                IncidentCount = reportData.IncidentCount,
                ManpowerRowCount = reportData.ManpowerRecords.Count,
                EmailedSuccessfully = false
            });
        }
        catch (Exception)
        {
            // A missing sample PDF must never stop the application from starting.
        }

        context.AuditLogs.Add(new AuditLog
        {
            EntityName = "Shift",
            EntityId = closedShift.Id,
            Action = "Seed",
            Details = "Sample closed shift created by the seeder.",
            UserId = sara.Id,
            UserName = sara.FullName
        });

        await context.SaveChangesAsync();

        // ---- Forward-looking roster: today and the next 6 days, still unclaimed ----
        var roster = new List<Shift>();

        for (var offset = 0; offset <= 6; offset++)
        {
            var date = DateTime.Today.AddDays(offset);

            foreach (var definition in ShiftDefinitions)
            {
                roster.Add(new Shift
                {
                    ShiftDate = date,
                    ShiftType = definition.Type,
                    StartTime = definition.Start,
                    EndTime = definition.End,
                    Area = "Terminal 1",
                    Status = ShiftStatus.Open,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        context.Shifts.AddRange(roster);
        await context.SaveChangesAsync();
    }
}