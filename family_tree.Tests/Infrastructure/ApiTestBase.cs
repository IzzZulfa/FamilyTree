using family_tree.Data;
using Microsoft.Extensions.DependencyInjection;

namespace family_tree.Tests.Infrastructure;

/// <summary>
/// Base class for API tests: resets the database before each test and exposes an HttpClient.
/// xUnit creates a new instance of the test class for every test, so InitializeAsync runs per test.
/// </summary>
[Collection(DatabaseCollection.Name)]
public abstract class ApiTestBase(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;
    protected HttpClient Client { get; } = factory.CreateClient();

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Runs code against a fresh DbContext, e.g. to check what was actually persisted.</summary>
    protected async Task<T> WithDbAsync<T>(Func<FamilyTreeDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<FamilyTreeDbContext>());
    }
}
