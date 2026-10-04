namespace ShiftHandover.Services;

/// <summary>
/// Business rules for claiming, logging into and closing shifts.
/// Controllers call this service so the rules are enforced in one place.
/// </summary>
public interface IShiftService
{
    Task<OperationResult<Models.Entities.Shift>> ClaimShiftAsync(int shiftId, int supervisorId);

    Task<OperationResult<Models.Entities.Shift>> CloseShiftAsync(
        int shiftId,
        int supervisorId,
        string? handoverRemarks,
        string? pendingActions);

    /// <summary>
    /// Loads a shift after checking that it is still open for logging:
    /// it must be claimed by <paramref name="supervisorId"/> and must not be closed.
    /// </summary>
    Task<OperationResult<Models.Entities.Shift>> GetEditableShiftAsync(int shiftId, int supervisorId);
}