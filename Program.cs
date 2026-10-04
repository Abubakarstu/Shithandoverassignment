using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Services

builder.Services.AddControllersWithViews();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

builder.Services.AddDbContext<ShiftHandoverContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);

        // Pages that load three collections at once (shift details, the handover report)
        // are split into separate round trips instead of one cartesian single query.
        sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });
});

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services
    .AddScoped<IShiftService, ShiftService>()
    .AddScoped<IShiftLogService, ShiftLogService>()
    .AddScoped<IReportDispatchService, ReportDispatchService>()
    .AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddScoped<IReportService>(sp => new ReportService(
    sp.GetRequiredService<ShiftHandoverContext>(),
    builder.Configuration["CompanyName"] ?? "Gulf Air Operations"));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ShiftHandover.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Return a real 403 instead of a redirect so unauthorised access is
        // obvious in the browser and in API-style checks.
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ------------------------------------------------------- Reverse-proxy support
// Shared Windows hosting (Plesk/IIS) terminates the request in front of the app, so
// the app only ever sees http://localhost. Without this, UseHttpsRedirection sends
// visitors into a redirect loop and the auth cookie loses its Secure flag.
// This must stay the first middleware in the pipeline.

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    ForwardLimit = 1,

    // The hosting provider's proxy addresses are not known in advance, so clear the
    // built-in loopback allow-list. Safe here because only the platform proxy can
    // reach this process.
    KnownNetworks = { },
    KnownProxies = { },
});

// ---------------------------------------------------------------- Startup tasks

var companyName = app.Configuration["CompanyName"] ?? "Gulf Air Operations";
var storageOptions = app.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

try
{
    var (reportFolder, outboxFolder) = storageOptions.Resolve(app.Environment.ContentRootPath);
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

    startupLogger.LogInformation("Reports folder: {ReportFolder}", reportFolder);
    startupLogger.LogInformation("E-mail outbox  : {OutboxFolder}", outboxFolder);
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex,
        "The report storage folders could not be created. Grant the application pool write access, "
        + "or set 'Storage:Root' in appsettings.json to a writable absolute path.");
    throw;
}

// HTTPS redirection is opt-in. A shared host that has no certificate installed yet serves
// plain HTTP only, and forcing a redirect there produces a site nobody can reach.
var enableHttpsRedirection = app.Configuration.GetValue("Security:EnableHttpsRedirection", app.Environment.IsDevelopment());

if (!enableHttpsRedirection && !app.Environment.IsDevelopment())
{
    app.Logger.LogWarning(
        "HTTPS redirection is disabled, so the application is reachable over plain HTTP. "
        + "Install a certificate and set 'Security:EnableHttpsRedirection' to true before going live.");
}

var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", !app.Environment.IsDevelopment());

// Demo accounts carry a published password, so they are never created in Production
// unless the operator explicitly opts in.
var seedOnStartup = app.Configuration.GetValue("Database:SeedOnStartup", app.Environment.IsDevelopment());

if (applyMigrations || seedOnStartup)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ShiftHandoverContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (applyMigrations)
        {
            context.Database.Migrate();
            logger.LogInformation("Database schema is up to date.");
        }

        if (seedOnStartup)
        {
            var reportFolder = storageOptions.Resolve(app.Environment.ContentRootPath).ReportFolder;
            await SeedData.SeedAsync(context, companyName, reportFolder);

            logger.LogInformation(
                "Seed data applied. Demo accounts use password {Password}. Remove the demo accounts before going live.",
                SeedData.DefaultPassword);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Database initialisation failed. Check the connection string, that SQL Server is reachable, "
            + "and that the schema script has been applied.");
    }
}

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();