using family_tree.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace family_tree.Tests.Infrastructure;

/// <summary>
/// Hosts the real API in memory, pointed at a dedicated PostgreSQL test database.
/// Shared by every test in the "Database" collection (created once per test run).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Override with the FAMILYTREE_TEST_CONNECTION environment variable if your
    // local Postgres uses different credentials.
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("FAMILYTREE_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=familytree_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FamilyTree"] = ConnectionString,
            }));
    }

    /// <summary>Recreates the test database from the migrations, so it always matches the model.</summary>
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyTreeDbContext>();

        // Safety net: these tests drop and truncate the database, so refuse to run
        // if the override above didn't take effect and we're pointed at a real one.
        var databaseName = db.Database.GetDbConnection().Database;
        if (!databaseName.EndsWith("_test", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to run tests against database '{databaseName}'; its name must end in '_test'.");
        }

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    /// <summary>Empties every table and restarts id sequences, giving each test a clean slate.</summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyTreeDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE partnerships, parent_children, people, family_trees RESTART IDENTITY CASCADE");
    }

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}
