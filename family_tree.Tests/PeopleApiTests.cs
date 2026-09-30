using System.Net;
using System.Net.Http.Json;
using family_tree.Dtos;
using family_tree.Entities;
using family_tree.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Tests;

public class PeopleApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Create_then_get_round_trips_all_fields()
    {
        var treeId = await CreateTreeAsync();
        var request = new SavePersonRequest(
            GivenName: "Mary",
            Surname: "Jones",
            BirthSurname: "Smith",
            Sex: Sex.Female,
            BirthDate: Date(1921, 3, precision: DatePrecision.Month),
            BirthPlace: "Cardiff",
            DeathDate: Date(1999, 12, 24),
            Notes: "Loved gardening");

        var response = await PostAsync($"/api/trees/{treeId}/people", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<SaveResponse<PersonResponse>>(response);
        Assert.Empty(created.Warnings);
        Assert.Equal($"/api/people/{created.Data.Id}", response.Headers.Location?.AbsolutePath);

        var fetched = await ReadAsync<PersonResponse>(await Client.GetAsync($"/api/people/{created.Data.Id}"));
        Assert.Equal(created.Data, fetched);
        Assert.Equal(new PersonResponse(created.Data.Id, treeId, "Mary", "Jones", "Smith", Sex.Female,
            Date(1921, 3, precision: DatePrecision.Month), "Cardiff", Date(1999, 12, 24), "Loved gardening"),
            fetched);
    }

    [Fact]
    public async Task Optional_fields_default_sensibly()
    {
        var treeId = await CreateTreeAsync();

        var response = await PostAsync($"/api/trees/{treeId}/people", new { givenName = "Ann" });

        var person = (await ReadAsync<SaveResponse<PersonResponse>>(response)).Data;
        Assert.Equal("", person.Surname);
        Assert.Equal(Sex.Unknown, person.Sex);
        Assert.Null(person.BirthDate);
    }

    [Fact]
    public async Task Create_without_given_name_is_rejected()
    {
        var treeId = await CreateTreeAsync();

        var response = await PostAsync($"/api/trees/{treeId}/people", new { surname = "Jones" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Numeric_enum_values_are_rejected()
    {
        var treeId = await CreateTreeAsync();

        var response = await PostAsync($"/api/trees/{treeId}/people", new { givenName = "Ann", sex = 99 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_returns_only_people_in_that_tree()
    {
        var treeA = await CreateTreeAsync("A");
        var treeB = await CreateTreeAsync("B");
        await CreatePersonAsync(treeA, "Alice");
        await CreatePersonAsync(treeB, "Bob");

        var people = await ReadAsync<List<PersonResponse>>(await Client.GetAsync($"/api/trees/{treeA}/people"));

        Assert.Equal("Alice", Assert.Single(people).GivenName);
    }

    [Fact]
    public async Task Missing_tree_or_person_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/trees/999/people")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await PostAsync("/api/trees/999/people", new SavePersonRequest("Ann"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/people/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await Client.PutAsJsonAsync("/api/people/999", new SavePersonRequest("Ann"), Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync("/api/people/999")).StatusCode);
    }

    [Fact]
    public async Task Update_replaces_all_fields()
    {
        var treeId = await CreateTreeAsync();
        var id = await CreatePersonAsync(treeId, "Jon", birthDate: Date(1900));

        var response = await Client.PutAsJsonAsync($"/api/people/{id}",
            new SavePersonRequest("John", Surname: "Smith", Sex: Sex.Male), Json);

        response.EnsureSuccessStatusCode();
        var updated = (await ReadAsync<SaveResponse<PersonResponse>>(response)).Data;
        Assert.Equal("John", updated.GivenName);
        Assert.Equal("Smith", updated.Surname);
        Assert.Null(updated.BirthDate); // PUT is a full replacement: omitted fields are cleared.
    }

    // Rule 5 (warning): death should not precede birth.
    [Fact]
    public async Task Death_before_birth_is_saved_with_a_warning()
    {
        var treeId = await CreateTreeAsync();

        var response = await PostAsync($"/api/trees/{treeId}/people",
            new SavePersonRequest("Ann", BirthDate: Date(1950), DeathDate: Date(1940)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await ReadAsync<SaveResponse<PersonResponse>>(response);
        Assert.Contains("death date", Assert.Single(result.Warnings));
    }

    [Fact]
    public async Task Death_on_the_day_of_birth_is_not_a_warning()
    {
        var treeId = await CreateTreeAsync();

        var response = await PostAsync($"/api/trees/{treeId}/people",
            new SavePersonRequest("Ann", BirthDate: Date(1950, 5, 5), DeathDate: Date(1950, 5, 5)));

        Assert.Empty((await ReadAsync<SaveResponse<PersonResponse>>(response)).Warnings);
    }

    [Fact]
    public async Task Update_death_before_birth_returns_a_warning()
    {
        var treeId = await CreateTreeAsync();
        var id = await CreatePersonAsync(treeId, "Ann", birthDate: Date(1950));

        var response = await Client.PutAsJsonAsync($"/api/people/{id}",
            new SavePersonRequest("Ann", BirthDate: Date(1950), DeathDate: Date(1949)), Json);

        response.EnsureSuccessStatusCode();
        Assert.Single((await ReadAsync<SaveResponse<PersonResponse>>(response)).Warnings);
    }

    [Fact]
    public async Task Delete_removes_the_person()
    {
        var treeId = await CreateTreeAsync();
        var id = await CreatePersonAsync(treeId, "Ann");

        var response = await Client.DeleteAsync($"/api/people/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/people/{id}")).StatusCode);
        Assert.False(await WithDbAsync(db => db.People.AnyAsync()));
    }
}
