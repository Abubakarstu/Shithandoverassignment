namespace ShiftHandover.Models.Enums;

/// <summary>Workflow status for an accident or an incident.</summary>
public enum InvestigationStatus
{
    Open = 0,
    UnderInvestigation = 1,
    ActionTaken = 2,
    Closed = 3
}