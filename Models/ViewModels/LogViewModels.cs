using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Models.ViewModels;

public class AccidentViewModel
{
    public int? Id { get; set; }

    public int ShiftId { get; set; }

    [Required]
    [Display(Name = "Date and time of accident")]
    public DateTime OccurredAt { get; set; } = DateTime.Now.AddMinutes(-5);

    [Required(ErrorMessage = "Accident type is required.")]
    public AccidentType AccidentType { get; set; }

    public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Persons injured cannot be negative.")]
    [Display(Name = "Persons injured")]
    public int PersonsInjured { get; set; }

    [StringLength(150)]
    [Display(Name = "Person(s) involved")]
    public string? PersonsInvolved { get; set; }

    [StringLength(150)]
    [Display(Name = "Injury type")]
    public string? InjuryType { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Immediate action taken is required.")]
    [StringLength(2000)]
    [Display(Name = "Immediate action taken")]
    public string ImmediateActionTaken { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Reported to")]
    public string? ReportedTo { get; set; }

    public InvestigationStatus InvestigationStatus { get; set; } = InvestigationStatus.Open;

    [Display(Name = "Reportable to regulator")]
    public bool IsReportable { get; set; }

    // Populated for the view only.
    public string ShiftLabel { get; set; } = string.Empty;

    public bool IsClosedShift { get; set; }

    public List<SelectListItem> AccidentTypeList => EnumHelper.ToSelectList<AccidentType>();
    public List<SelectListItem> SeverityList => EnumHelper.ToSelectList<SeverityLevel>();
    public List<SelectListItem> InvestigationStatusList => EnumHelper.ToSelectList<InvestigationStatus>();
}

public class IncidentViewModel
{
    public int? Id { get; set; }

    public int ShiftId { get; set; }
    [Required]
    [Display(Name = "Date and time of incident")]
    public DateTime OccurredAt { get; set; } = DateTime.Now.AddMinutes(-5);

    public IncidentCategory Category { get; set; } = IncidentCategory.Operational;

    public SeverityLevel Severity { get; set; } = SeverityLevel.Low;

    public InvestigationStatus InvestigationStatus { get; set; } = InvestigationStatus.Open;

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Reference number")]
    public string? ReferenceNumber { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Action taken is required.")]
    [StringLength(2000)]
    [Display(Name = "Action taken")]
    public string ActionTaken { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Escalated to")]
    public string? EscalatedTo { get; set; }

    [Display(Name = "Escalated")]
    public bool IsEscalated { get; set; }

    public string ShiftLabel { get; set; } = string.Empty;

    public bool IsClosedShift { get; set; }

    public List<SelectListItem> CategoryList => EnumHelper.ToSelectList<IncidentCategory>();
    public List<SelectListItem> SeverityList => EnumHelper.ToSelectList<SeverityLevel>();
    public List<SelectListItem> InvestigationStatusList => EnumHelper.ToSelectList<InvestigationStatus>();
}

public class ManpowerViewModel
{
    public int? Id { get; set; }

    public int ShiftId { get; set; }
    [Required(ErrorMessage = "Function / area is required.")]
    [StringLength(120)]
    [Display(Name = "Function / area")]
    public string FunctionName { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "Planned cannot be negative.")]
    public int Planned { get; set; }

    [Range(0, 10000, ErrorMessage = "On duty cannot be negative.")]
    [Display(Name = "On duty")]
    public int OnDuty { get; set; }

    [Range(0, 10000, ErrorMessage = "On leave cannot be negative.")]
    [Display(Name = "On leave")]
    public int OnLeave { get; set; }

    [Range(0, 10000, ErrorMessage = "Absent cannot be negative.")]
    public int Absent { get; set; }

    [Range(0, 10000, ErrorMessage = "Overtime cannot be negative.")]
    public int Overtime { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    public string ShiftLabel { get; set; } = string.Empty;

    public bool IsClosedShift { get; set; }
}

internal static class EnumHelper
{
    public static List<SelectListItem> ToSelectList<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(v => new SelectListItem
            {
                Value = v.ToString(),
                Text = SplitPascalCase(v.ToString())
            })
            .ToList();
    }

    private static string SplitPascalCase(string value) =>
        string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
}