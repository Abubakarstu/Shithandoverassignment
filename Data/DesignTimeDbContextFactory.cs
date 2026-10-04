using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShiftHandover.Data;

/// <summary>
/// Lets "dotnet ef" create a DbContext without booting the whole web host.
/// Keeps migrations runnable from the command line and from Visual Studio.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShiftHandoverContext>
{
    public ShiftHandoverContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShiftHandoverContext>();

        var connectionString = Environment.GetEnvironmentVariable("SHIFT_HANDOVER_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ShiftHandoverDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False";

        optionsBuilder.UseSqlServer(connectionString);

        return new ShiftHandoverContext(optionsBuilder.Options);
    }
}