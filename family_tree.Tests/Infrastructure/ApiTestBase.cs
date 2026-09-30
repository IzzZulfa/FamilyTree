using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using family_tree.Data;
using family_tree.Dtos;
using family_tree.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace family_tree.Tests.Infrastructure;

/// <summary>
/// Base class for API tests: resets the database before each test, exposes an HttpClient,
/// and offers helpers for building up a small family.
/// xUnit creates a new instance of the test class for every test, so InitializeAsync runs per test.
/// </summary>
[Collection(DatabaseCollection.Name)]
public abstract class ApiTestBase(ApiFactory factory) : IAsyncLifetime
{
    /// <summary>Matches the API's JSON settings (camelCase, enums as strings).</summary>
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

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

    protected Task<HttpResponseMessage> PostAsync(string url, object body) =>
        Client.PostAsJsonAsync(url, body, Json);

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<T>(Json);
        return body ?? throw new InvalidOperationException("Response body was empty.");
    }

    /// <summary>Asserts a 400 ProblemDetails response carrying the given validation code.</summary>
    protected static async Task AssertValidationErrorAsync(HttpResponseMessage response, string expectedCode)
    {
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ProblemDetails>(response);
        Assert.True(problem.Extensions.TryGetValue("code", out var code), "ProblemDetails has no 'code'.");
        Assert.Equal(expectedCode, code?.ToString());
    }

    protected static FuzzyDateDto Date(int year, int month = 1, int day = 1,
        DatePrecision precision = DatePrecision.Exact) =>
        new(new DateOnly(year, month, day), precision);

    protected async Task<int> CreateTreeAsync(string name = "Test family")
    {
        var response = await PostAsync("/api/trees", new CreateTreeRequest(name));
        response.EnsureSuccessStatusCode();
        return (await ReadAsync<TreeResponse>(response)).Id;
    }

    protected async Task<int> CreatePersonAsync(
        int treeId, string givenName, FuzzyDateDto? birthDate = null, FuzzyDateDto? deathDate = null)
    {
        var response = await PostAsync($"/api/trees/{treeId}/people",
            new SavePersonRequest(givenName, BirthDate: birthDate, DeathDate: deathDate));
        response.EnsureSuccessStatusCode();
        return (await ReadAsync<SaveResponse<PersonResponse>>(response)).Data.Id;
    }
}
