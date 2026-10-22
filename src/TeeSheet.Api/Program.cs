using Microsoft.EntityFrameworkCore;
using TeeSheet.Api.Endpoints;
using TeeSheet.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Inject TimeProvider instead of calling DateTime.UtcNow, so tests can fake "now" (needed for A5 / A12).
builder.Services.AddSingleton(TimeProvider.System);

// The connection string is resolved lazily (inside the factory lambda) so integration tests can override it.
builder.Services.AddDbContext<TeeSheetDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("TeeTimeDb")
        ?? throw new InvalidOperationException(
            "Missing connection string 'ConnectionStrings:TeeTimeDb'. See README.md, step 3 (user-secrets).");

    options.UseSqlServer(connectionString);

    if (sp.GetRequiredService<IHostEnvironment>().IsDevelopment())
    {
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();            // /openapi/v1.json
    app.MapDevEndpoints();       // /dev/stats
}

app.MapHealthChecks("/health");
app.MapTeeTimeEndpoints();

app.Run();

// Makes the entry point visible to WebApplicationFactory<Program> in the integration tests.
public partial class Program { }
