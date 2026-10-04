using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Models.Entities;
using ShiftHandover.Models.Enums;

namespace ShiftHandover.Services;

public class ShiftService : IShiftService
{
    private readonly ShiftHandoverContext _context;

    public ShiftService(ShiftHandoverContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<Shift>> ClaimShiftAsync(int shiftId, int supervisorId)
    {
        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == shiftId);
        if (shift is null)
        {
            return OperationResult<Shift>.Fail("Shift not found.");
        }

        if (shift.Status == ShiftStatus.Closed)
        {
            return OperationResult<Shift>.Fail("This shift is already closed and can no longer be claimed.");
        }

        if (shift.ClaimedById == supervisorId)
        {
            return OperationResult<Shift>.Fail("You have already claimed this shift.");
        }

        if (shift.Status != ShiftStatus.Open || shift.ClaimedById.HasValue)
        {
            var owner = shift.ClaimedById.HasValue
                ? await _context.Supervisors.Where(x => x.Id == shift.ClaimedById).Select(x => x.FullName).FirstOrDefaultAsync()
                : null;

            return OperationResult<Shift>.Fail(
                $"This shift was already claimed{(owner is null ? string.Empty : $" by {owner}")} and is locked for editing.");
        }

        // Atomic conditional update: the WHERE clause re-checks the state inside the
        // database transaction, so two supervisors clicking "Claim" at the same time
        // cannot both win - only the first UPDATE affects a row.
        var affected = await _context.Shifts
            .Where(x => x.Id == shiftId && x.Status == ShiftStatus.Open && x.ClaimedById == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ShiftStatus.Claimed)
                .SetProperty(x => x.ClaimedById, supervisorId)
                .SetProperty(x => x.ClaimedAt, DateTime.UtcNow));

        if (affected == 0)
        {
            return OperationResult<Shift>.Fail("Another supervisor claimed this shift a moment ago. Please reload the page.");
        }

        await AddAuditAsync("Shift", shiftId, "Claim", $"Shift claimed by supervisor #{supervisorId}.", supervisorId);

        var claimed = await _context.Shifts
            .AsNoTracking()
            .Include(x => x.ClaimedBy)
            .FirstAsync(x => x.Id == shiftId);

        return OperationResult<Shift>.Ok(claimed);
    }

    public async Task<OperationResult<Shift>> CloseShiftAsync(
        int shiftId,
        int supervisorId,
        string? handoverRemarks,
        string? pendingActions)
    {
        // Tracked query: the entity has to be attached to the context, otherwise
        // SaveChangesAsync has nothing to write.
        var shift = await _context.Shifts
            .Include(x => x.ClaimedBy)
            .FirstOrDefaultAsync(x => x.Id == shiftId);

        var validation = ValidateEditable(shift, supervisorId);
        if (!validation.Succeeded)
        {
            return OperationResult<Shift>.Fail(validation.ErrorMessage!);
        }

        var tracked = shift!;

        if (tracked.Status != ShiftStatus.Claimed)
        {
            return OperationResult<Shift>.Fail("Only a claimed shift can be closed.");
        }

        tracked.Status = ShiftStatus.Closed;
        tracked.ClosedAt = DateTime.UtcNow;
        tracked.ClosedById = supervisorId;
        tracked.HandoverRemarks = handoverRemarks;
        tracked.PendingActions = pendingActions;

        try
        {
            // RowVersion is configured as a concurrency token, so if another request
            // modified the same shift we get a DbUpdateConcurrencyException here.
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<Shift>.Fail("This shift was modified by someone else. Please reload and try again.");
        }

        await AddAuditAsync("Shift", shiftId, "Close", $"Shift closed by supervisor #{supervisorId}.", supervisorId);

        var closed = await _context.Shifts
            .AsNoTracking()
            .Include(x => x.ClaimedBy)
            .Include(x => x.ClosedBy)
            .FirstAsync(x => x.Id == shiftId);

        return OperationResult<Shift>.Ok(closed);
    }

    public async Task<OperationResult<Shift>> GetEditableShiftAsync(int shiftId, int supervisorId)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .Include(x => x.ClaimedBy)
            .Include(x => x.ClosedBy)
            .FirstOrDefaultAsync(x => x.Id == shiftId);

        var validation = ValidateEditable(shift, supervisorId);
        if (!validation.Succeeded)
        {
            return OperationResult<Shift>.Fail(validation.ErrorMessage!);
        }

        return OperationResult<Shift>.Ok(shift!);
    }

    /// <summary>
    /// The single place where the "who may touch this shift" rules live:
    /// a shift must exist, must not be closed, and must be claimed by this supervisor.
    /// </summary>
    private static OperationResult ValidateEditable(Shift? shift, int supervisorId)
    {
        if (shift is null)
        {
            return OperationResult.Fail("Shift not found.");
        }

        if (shift.Status == ShiftStatus.Closed)
        {
            return OperationResult.Fail("This shift is closed. Closed shifts are read-only and cannot be modified.");
        }

        if (shift.Status != ShiftStatus.Claimed || shift.ClaimedById != supervisorId)
        {
            return OperationResult.Fail("Only the supervisor who claimed this shift can add or edit records on it.");
        }

        return OperationResult.Ok();
    }

    private async Task AddAuditAsync(string entity, int? entityId, string action, string? details, int? userId)
    {
        var supervisor = userId.HasValue
            ? await _context.Supervisors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId)
            : null;

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = entity,
            EntityId = entityId,
            Action = action,
            Details = details,
            UserId = userId,
            UserName = supervisor?.FullName
        });

        await _context.SaveChangesAsync();
    }
}