using ShiftHandover.Models.Entities;
using ShiftHandover.Models.ViewModels;

namespace ShiftHandover.Services;

/// <summary>
/// CRUD for the three log types. Every write goes through
/// <see cref="IShiftService.GetEditableShiftAsync"/> so closed shifts and shifts owned by
/// another supervisor can never be modified, regardless of what the UI allows.
/// </summary>
public interface IShiftLogService
{
    Task<IReadOnlyList<Accident>> GetAccidentsAsync(int shiftId);
    Task<OperationResult<Accident>> SaveAccidentAsync(AccidentViewModel model, int supervisorId);
    Task<OperationResult> DeleteAccidentAsync(int accidentId, int supervisorId);

    Task<IReadOnlyList<Incident>> GetIncidentsAsync(int shiftId);
    Task<OperationResult<Incident>> SaveIncidentAsync(IncidentViewModel model, int supervisorId);
    Task<OperationResult> DeleteIncidentAsync(int incidentId, int supervisorId);

    Task<IReadOnlyList<ManpowerRecord>> GetManpowerAsync(int shiftId);
    Task<OperationResult<ManpowerRecord>> SaveManpowerAsync(ManpowerViewModel model, int supervisorId);
    Task<OperationResult> DeleteManpowerAsync(int manpowerId, int supervisorId);
}