using System.Security.Claims;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Helpers;

public static class ClaimsPrincipalExtensions
{
    public const string SupervisorIdClaimType = "supervisor_id";
    public const string FullNameClaimType = "full_name";
    public const string EmployeeCodeClaimType = "employee_code";

    public static int GetSupervisorId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(SupervisorIdClaimType), out var id) ? id : 0;

    public static string GetFullName(this ClaimsPrincipal user) =>
        user.FindFirstValue(FullNameClaimType) ?? user.Identity?.Name ?? "Unknown user";

    public static string GetEmployeeCode(this ClaimsPrincipal user) =>
        user.FindFirstValue(EmployeeCodeClaimType) ?? string.Empty;

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        string.Equals(user.FindFirstValue(ClaimTypes.Role), nameof(SupervisorRole.Admin), StringComparison.OrdinalIgnoreCase);
}