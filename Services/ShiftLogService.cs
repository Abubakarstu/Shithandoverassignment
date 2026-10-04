using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Models.Entities;
using ShiftHandover.Models.ViewModels;
using ShiftHandover.Services;

namespace ShiftHandover.Services;

public class ShiftLogService : IShiftLogService
{
    private readonly ShiftHandoverContext _context;
    private readonly IShiftService _shiftService;

    public ShiftLogService(ShiftHandoverContext context, IShiftService shiftService)
    {
        _context = context;
        _shiftService = shiftService;
    }

    // ------------------------------------------------------------------ Accidents

    public async Task<IReadOnlyList<Accident>> GetAccidentsAsync(int shiftId)
    {
        return await _context.Accidents
            .AsNoTracking()
            .Include(x => x.LoggedBy)
            .Where(x => x.ShiftId == shiftId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync();
    }

    public async Task<OperationResult<Accident>> SaveAccidentAsync(AccidentViewModel model, int supervisorId)
    {
        var guard = await _shiftService.GetEditableShiftAsync(model.ShiftId, supervisorId);
        if (!guard.Succeeded)
        {
            return OperationResult<Accident>.Fail(guard.ErrorMessage!);
        }

        Accident entity;

        if (model.Id is null or 0)
        {
            entity = new Accident { CreatedAt = DateTime.UtcNow };
            _context.Accidents.Add(entity);
        }
        else
        {
            var existing = await _context.Accidents.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing is null)
            {
                return OperationResult<Accident>.Fail("Accident record not found.");
            }

            entity = existing;
        }

        entity.ShiftId = model.ShiftId;
        entity.LoggedById = supervisorId;
        entity.OccurredAt = model.OccurredAt;
        entity.AccidentType = model.AccidentType;
        entity.Severity = model.Severity;
        entity.Location = model.Location;
        entity.PersonsInjured = model.PersonsInjured;
        entity.PersonsInvolved = model.PersonsInvolved;
        entity.InjuryType = model.InjuryType;
        entity.Description = model.Description;
        entity.ImmediateActionTaken = model.ImmediateActionTaken;
        entity.ReportedTo = model.ReportedTo;
        entity.InvestigationStatus = model.InvestigationStatus;
        entity.IsReportable = model.IsReportable;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return OperationResult<Accident>.Ok(entity);
    }

    public async Task<OperationResult> DeleteAccidentAsync(int accidentId, int supervisorId)
    {
        var accident = await _context.Accidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == accidentId);
        if (accident is null)
        {
            return OperationResult.Fail("Accident record not found.");
        }

        var guard = await _shiftService.GetEditableShiftAsync(accident.ShiftId, supervisorId);
        if (!guard.Succeeded)
        {
            return OperationResult.Fail(guard.ErrorMessage!);
        }

        _context.Accidents.Remove(accident);
        await _context.SaveChangesAsync();

        return OperationResult.Ok();
    }

    // ------------------------------------------------------------------ Incidents

    public async Task<IReadOnlyList<Incident>> GetIncidentsAsync(int shiftId)
    {
        return await _context.Incidents
            .AsNoTracking()
            .Include(x => x.LoggedBy)
            .Where(x => x.ShiftId == shiftId)
            .OrderBy(x => x.OccurredAt)
            .ToListAsync();
    }

    public async Task<OperationResult<Incident>> SaveIncidentAsync(IncidentViewModel model, int supervisorId)
    {
        var guard = await _shiftService.GetEditableShiftAsync(model.ShiftId, supervisorId);
        if (!guard.Succeeded)
        {
            return OperationResult<Incident>.Fail(guard.ErrorMessage!);
        }

        Incident entity;

        if (model.Id is null or 0)
        {
            entity = new Incident { CreatedAt = DateTime.UtcNow };
            _context.Incidents.Add(entity);
        }
        else
        {
            var existing = await _context.Incidents.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existing is null)
            {
                return OperationResult<Incident>.Fail("Incident record not found.");
            }

            entity = existing;
        }

        entity.ShiftId = model.ShiftId;
        entity.LoggedById = supervisorId;
        entity.OccurredAt = model.OccurredAt;
        entity.Category = model.Category;
        entity.Severity = model.Severity;
        entity.InvestigationStatus = model.InvestigationStatus;
        entity.Location = model.Location;
        entity.ReferenceNumber = model.ReferenceNumber;
        entity.Description = model.Description;
        entity.ActionTaken = model.ActionTaken;
        entity.EscalatedTo = model.EscalatedTo;
        entity.IsEscalated = model.IsEscalated;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return OperationResult<Incident>.Ok(entity);
    }

    public async Task<OperationResult> DeleteIncidentAsync(int incidentId, int supervisorId)
    {
        var incident = await _context.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == incidentId);
        if (incident is null)
        {
            return OperationResult.Fail("Incident record not found.");
        }

        var guard = await _shiftService.GetEditableShiftAsync(incident.ShiftId, supervisorId);
        if (!guard.Succeeded)
        {
            return OperationResult.Fail(guard.ErrorMessage!);
        }

        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();

        return OperationResult.Ok();
    }

    // ------------------------------------------------------------------ Manpower

    public async Task<IReadOnlyList<ManpowerRecord>> GetManpowerAsync(int shiftId)
    {
        return await _context.ManpowerRecords
            .AsNoTracking()
            .Include(x => x.LoggedBy)
            .Where(x => x.ShiftId == shiftId)
            .OrderBy(x => x.FunctionName)
            .ToListAsync();
    }

    public async Task<OperationResult<ManpowerRecord>> SaveManpowerAsync(ManpowerViewModel model, int supervisorId)
    {
        var guard = await _shiftService.GetEditableShiftAsync(model.ShiftId, supervisorId);
        if (!guard.Succeeded)
        {
            return OperationResult<ManpowerRecord>.Fail(guard.ErrorMessage!);
        }

        // One row per function per shift: if the function already exists, update it in place.
        var existing = await _context.ManpowerRecords
            .FirstOrDefaultAsync(x => x.ShiftId == model.ShiftId && x.FunctionName == model.FunctionName);

        ManpowerRecord entity;
        if (existing is not null)
        {
            entity = existing;
        }
        else
        {
            entity = new ManpowerRecord { CreatedAt = DateTime.UtcNow };
            _context.ManpowerRecords.Add(entity);
        }

        entity.ShiftId = model.ShiftId;
        entity.LoggedById = supervisorId;
        entity.FunctionName = model.FunctionName;
        entity.Planned = model.Planned;
        entity.OnDuty = model.OnDuty;
        entity.OnLeave = model.OnLeave;
        entity.Absent = model.Absent;
        entity.Overtime = model.Overtime;
        entity.Remarks = model.Remarks;
        entity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return OperationResult<ManpowerRecord>.Fail(
                $"A manpower row for '{model.FunctionName}' already exists on this shift.");
        }

        return OperationResult<ManpowerRecord>.Ok(entity);
    }

    public async Task<OperationResult> DeleteManpowerAsync(int manpowerId, int supervisorId)
    {
        var record = await _context.ManpowerRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == manpowerId);
        if (record is null)
        {
            return OperationResult.Fail("Manpower record not found.");
        }

        var guard = await _shiftService.GetEditableShiftAsync(record.ShiftId, supervisorId);
        if (!guard.Succeeded)
        {
            return OperationResult.Fail(guard.ErrorMessage!);
        }

        _context.ManpowerRecords.Remove(record);
        await _context.SaveChangesAsync();

        return OperationResult.Ok();
    }
}