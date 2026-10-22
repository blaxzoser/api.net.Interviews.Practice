using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeeSheet.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace TeeSheet.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against a real SQL Server in Docker (Testcontainers), with the real schema script applied.
/// One container is shared by every test in the "Api" collection.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabase = "TeeTimeTests";

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private string _connectionString = "";

    /// <summary>Every SQL command EF executed (see <see cref="SqlCommandLog"/>).</summary>
    public SqlCommandLog SqlLog { get; } = new();

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        await using (var master = new SqlConnection(_sql.GetConnectionString()))
        {
            await master.OpenAsync();
            await using var create = master.CreateCommand();
            create.CommandText = $"CREATE DATABASE {TestDatabase}";
            await create.ExecuteNonQueryAsync();
        }

        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = TestDatabase };
        _connectionString = builder.ConnectionString;

        var schemaPath = Path.Combine(AppContext.BaseDirectory, "sql", "01-schema.sql");
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var schema = connection.CreateCommand();
        schema.CommandText = await File.ReadAllTextAsync(schemaPath);
        await schema.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TeeTimeDb", _connectionString);

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(SqlLog);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
        });
    }

    /// <summary>Runs <paramref name="action"/> with a DbContext from a fresh scope (used to arrange test data).</summary>
    public async Task<T> WithDbAsync<T>(Func<TeeSheetDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TeeSheetDbContext>());
    }

    // WebApplicationFactory already has ValueTask DisposeAsync(); xunit's IAsyncLifetime wants Task DisposeAsync().
    // `new` hides the base method and satisfies xunit.
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}
