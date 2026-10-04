using ShiftHandover.Models.Entities;

namespace ShiftHandover.Models.Reports;

/// <summary>
/// Everything the PDF template needs, loaded in one aggregate query so the
/// template never touches the DbContext (keeps the view layer free of data access).
/// </summary>
public class ShiftReportData
{
    public required Shift Shift { get; init; }

    public Supervisor? ClaimedBy { get; init; }

    public Supervisor? ClosedBy { get; init; }

    public required IReadOnlyList<Accident> Accidents { get; init; }

    public required IReadOnlyList<Incident> Incidents { get; init; }

    public required IReadOnlyList<ManpowerRecord> ManpowerRecords { get; init; }

    public DateTime GeneratedAt { get; init; } = DateTime.Now;

    public string GeneratedByName { get; init; } = string.Empty;

    // ---- Aggregates shown in the summary strip ----
    public int AccidentCount => Accidents.Count;

    public int PersonsInjured => Accidents.Sum(x => x.PersonsInjured);

    public int ReportableAccidents => Accidents.Count(x => x.IsReportable);

    public int IncidentCount => Incidents.Count;

    public int EscalatedIncidents => Incidents.Count(x => x.IsEscalated);

    public int TotalPlanned => ManpowerRecords.Sum(x => x.Planned);

    public int TotalOnDuty => ManpowerRecords.Sum(x => x.OnDuty);

    public int TotalOnLeave => ManpowerRecords.Sum(x => x.OnLeave);

    public int TotalAbsent => ManpowerRecords.Sum(x => x.Absent);

    public int TotalOvertime => ManpowerRecords.Sum(x => x.Overtime);

    public string ShiftLabel => $"{Shift.ShiftDate:dd MMM yyyy} - {Shift.ShiftType} shift";

    public string ShiftWindow =>
        $"{Shift.StartTime:hh\\:mm} - {Shift.EndTime:hh\\:mm}";
}