using Microsoft.EntityFrameworkCore;
using ShiftHandover.Models.Entities;

namespace ShiftHandover.Data;

public class ShiftHandoverContext : DbContext
{
    public ShiftHandoverContext(DbContextOptions<ShiftHandoverContext> options) : base(options)
    {
    }

    public DbSet<Supervisor> Supervisors => Set<Supervisor>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Accident> Accidents => Set<Accident>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<ManpowerRecord> ManpowerRecords => Set<ManpowerRecord>();
    public DbSet<ShiftReport> ShiftReports => Set<ShiftReport>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Supervisor>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.EmployeeCode).IsUnique();

            entity.HasMany(x => x.ClaimedShifts)
                  .WithOne(x => x.ClaimedBy)
                  .HasForeignKey(x => x.ClaimedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.ClosedShifts)
                  .WithOne(x => x.ClosedBy)
                  .HasForeignKey(x => x.ClosedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.AccidentsLogged)
                  .WithOne(x => x.LoggedBy)
                  .HasForeignKey(x => x.LoggedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.IncidentsLogged)
                  .WithOne(x => x.LoggedBy)
                  .HasForeignKey(x => x.LoggedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.ManpowerRecordsLogged)
                  .WithOne(x => x.LoggedBy)
                  .HasForeignKey(x => x.LoggedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            // A duty slot can only exist once per date + shift type.
            entity.HasIndex(x => new { x.ShiftDate, x.ShiftType })
                  .IsUnique()
                  .HasDatabaseName("UX_Shifts_ShiftDate_ShiftType");

            entity.HasIndex(x => new { x.Status, x.ShiftDate });

            entity.HasMany(x => x.Accidents)
                  .WithOne(x => x.Shift)
                  .HasForeignKey(x => x.ShiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Incidents)
                  .WithOne(x => x.Shift)
                  .HasForeignKey(x => x.ShiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.ManpowerRecords)
                  .WithOne(x => x.Shift)
                  .HasForeignKey(x => x.ShiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Reports)
                  .WithOne(x => x.Shift)
                  .HasForeignKey(x => x.ShiftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(x => x.RowVersion).IsRowVersion();

            // Only date + time is meaningful, never the clock part of ShiftDate.
            entity.Property(x => x.ShiftDate).HasColumnType("date");
        });

        modelBuilder.Entity<Accident>(entity =>
        {
            entity.HasIndex(x => new { x.ShiftId, x.OccurredAt });
            entity.Property(x => x.AccidentType).HasConversion<string>();
            entity.Property(x => x.Severity).HasConversion<string>();
            entity.Property(x => x.InvestigationStatus).HasConversion<string>();
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasIndex(x => new { x.ShiftId, x.OccurredAt });
            entity.Property(x => x.Category).HasConversion<string>();
            entity.Property(x => x.Severity).HasConversion<string>();
            entity.Property(x => x.InvestigationStatus).HasConversion<string>();
        });

        modelBuilder.Entity<ManpowerRecord>(entity =>
        {
            // One manpower row per function per shift.
            entity.HasIndex(x => new { x.ShiftId, x.FunctionName })
                  .IsUnique()
                  .HasDatabaseName("UX_Manpower_Shift_Function");
        });

        modelBuilder.Entity<ShiftReport>(entity =>
        {
            entity.HasOne(x => x.GeneratedBy)
                  .WithMany()
                  .HasForeignKey(x => x.GeneratedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            // Restrict (not Cascade): SQL Server rejects multiple cascade paths from
            // Shift -> EmailLogs, because Shift already cascades to ShiftReports.
            entity.HasOne(x => x.Shift)
                  .WithMany()
                  .HasForeignKey(x => x.ShiftId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ShiftReport)
                  .WithMany()
                  .HasForeignKey(x => x.ShiftReportId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => new { x.ShiftId, x.CreatedAt });
        });
    }
}