using System.Net;
using System.Net.Http.Json;
using family_tree.Dtos;
using family_tree.Entities;
using family_tree.Services;
using family_tree.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Tests;

public class ParentChildApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private Task<HttpResponseMessage> AddParentAsync(
        int childId, int parentId, ParentChildType type = ParentChildType.Biological) =>
        PostAsync($"/api/people/{childId}/parents", new AddParentRequest(parentId, type));

    private async Task<int> AddParentOkAsync(
        int childId, int parentId, ParentChildType type = ParentChildType.Biological)
    {
        var response = await AddParentAsync(childId, parentId, type);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<SaveResponse<ParentChildResponse>>(response)).Data.Id;
    }

    [Fact]
    public async Task Add_parent_creates_the_edge()
    {
        var treeId = await CreateTreeAsync();
        var child = await CreatePersonAsync(treeId, "Child");
        var mother = await CreatePersonAsync(treeId, "Mother");

        var response = await AddParentAsync(child, mother, ParentChildType.Adoptive);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await ReadAsync<SaveResponse<ParentChildResponse>>(response);
        Assert.Equal(new ParentChildResponse(result.Data.Id, mother, child, ParentChildType.Adoptive), result.Data);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Add_parent_to_missing_child_returns_404()
    {
        var treeId = await CreateTreeAsync();
        var parent = await CreatePersonAsync(treeId, "Parent");

        var response = await AddParentAsync(999, parent);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_missing_parent_is_rejected()
    {
        var treeId = await CreateTreeAsync();
        var child = await CreatePersonAsync(treeId, "Child");

        await AssertValidationErrorAsync(await AddParentAsync(child, 999), ValidationCodes.PersonNotFound);
    }

    // Rule 3: no self-relationships.
    [Fact]
    public async Task Person_cannot_be_their_own_parent()
    {
        var treeId = await CreateTreeAsync();
        var person = await CreatePersonAsync(treeId, "Narcissus");

        await AssertValidationErrorAsync(await AddParentAsync(person, person), ValidationCodes.SelfRelationship);
    }

    // Rule 1: no cycles.
    [Fact]
    public async Task Child_cannot_become_parent_of_their_parent()
    {
        var treeId = await CreateTreeAsync();
        var parent = await CreatePersonAsync(treeId, "Parent");
        var child = await CreatePersonAsync(treeId, "Child");
        await AddParentOkAsync(child, parent);

        await AssertValidationErrorAsync(await AddParentAsync(parent, child), ValidationCodes.Cycle);
    }

    [Fact]
    public async Task Descendant_several_generations_down_cannot_become_an_ancestor()
    {
        var treeId = await CreateTreeAsync();
        var grandparent = await CreatePersonAsync(treeId, "Grandparent");
        var parent = await CreatePersonAsync(treeId, "Parent");
        var child = await CreatePersonAsync(treeId, "Child");
        var grandchild = await CreatePersonAsync(treeId, "Grandchild");
        await AddParentOkAsync(parent, grandparent);
        await AddParentOkAsync(child, parent, ParentChildType.Adoptive); // cycles count every edge type
        await AddParentOkAsync(grandchild, child);

        await AssertValidationErrorAsync(await AddParentAsync(grandparent, grandchild), ValidationCodes.Cycle);
    }

    [Fact]
    public async Task Pedigree_collapse_is_not_a_cycle()
    {
        // Cousins marry and have a child: that child reaches the shared grandparent by two paths.
        // Adding the second path must be allowed; it forms a diamond, not a loop.
        var treeId = await CreateTreeAsync();
        var ancestor = await CreatePersonAsync(treeId, "Ancestor");
        var sonA = await CreatePersonAsync(treeId, "SonA");
        var sonB = await CreatePersonAsync(treeId, "SonB");
        var cousinA = await CreatePersonAsync(treeId, "CousinA");
        var cousinB = await CreatePersonAsync(treeId, "CousinB");
        var child = await CreatePersonAsync(treeId, "Child");
        await AddParentOkAsync(sonA, ancestor);
        await AddParentOkAsync(sonB, ancestor);
        await AddParentOkAsync(cousinA, sonA);
        await AddParentOkAsync(cousinB, sonB);
        await AddParentOkAsync(child, cousinA);

        await AddParentOkAsync(child, cousinB);
    }

    // Rule 2: at most two biological parents; other types are unlimited.
    [Fact]
    public async Task Third_biological_parent_is_rejected()
    {
        var treeId = await CreateTreeAsync();
        var child = await CreatePersonAsync(treeId, "Child");
        await AddParentOkAsync(child, await CreatePersonAsync(treeId, "Mother"));
        await AddParentOkAsync(child, await CreatePersonAsync(treeId, "Father"));

        var response = await AddParentAsync(child, await CreatePersonAsync(treeId, "Third"));

        await AssertValidationErrorAsync(response, ValidationCodes.TooManyBiologicalParents);
    }

    [Theory]
    [InlineData(ParentChildType.Adoptive)]
    [InlineData(ParentChildType.Step)]
    [InlineData(ParentChildType.Foster)]
    public async Task Non_biological_parents_are_unlimited(ParentChildType type)
    {
        var treeId = await CreateTreeAsync();
        var child = await CreatePersonAsync(treeId, "Child");
        await AddParentOkAsync(child, await CreatePersonAsync(treeId, "Mother"));
        await AddParentOkAsync(child, await CreatePersonAsync(treeId, "Father"));

        for (var i = 0; i < 3; i++)
        {
            await AddParentOkAsync(child, await CreatePersonAsync(treeId, $"Other {i}"), type);
        }
    }

    // Rule 6: same tree.
    [Fact]
    public async Task Parent_from_another_tree_is_rejected()
    {
        var child = await CreatePersonAsync(await CreateTreeAsync("A"), "Child");
        var parent = await CreatePersonAsync(await CreateTreeAsync("B"), "Parent");

        await AssertValidationErrorAsync(await AddParentAsync(child, parent), ValidationCodes.DifferentTrees);
    }

    [Fact]
    public async Task Same_parent_cannot_be_added_twice()
    {
        var treeId = await CreateTreeAsync();
        var child = await CreatePersonAsync(treeId, "Child");
        var parent = await CreatePersonAsync(treeId, "Parent");
        await AddParentOkAsync(child, parent);

        var response = await AddParentAsync(child, parent, ParentChildType.Step);

        await AssertValidationErrorAsync(response, ValidationCodes.DuplicateParentChild);
    }

    // Rule 5 (warning): a parent should be born before their child.
    [Fact]
    public async Task Parent_born_after_child_is_saved_with_a_warning()
    {
        var treeId = await CreateTreeAsync();
        var child = await CreatePersonAsync(treeId, "Child", birthDate: Date(1950));
        var parent = await CreatePersonAsync(treeId, "Parent", birthDate: Date(1960));

        var response = await AddParentAsync(child, parent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var warning = Assert.Single((await ReadAsync<SaveResponse<ParentChildResponse>>(response)).Warnings);
        Assert.Contains("not born before", warning);
    }

    [Fact]
    public async Task Changing_a_birth_date_warns_about_existing_parents_and_children()
    {
        var treeId = await CreateTreeAsync();
        var parent = await CreatePersonAsync(treeId, "Parent", birthDate: Date(1920));
        var person = await CreatePersonAsync(treeId, "Person", birthDate: Date(1950));
        var child = await CreatePersonAsync(treeId, "Child", birthDate: Date(1980));
        await AddParentOkAsync(person, parent);
        await AddParentOkAsync(child, person);

        var response = await Client.PutAsJsonAsync($"/api/people/{person}",
            new SavePersonRequest("Person", BirthDate: Date(1990)), Json);

        response.EnsureSuccessStatusCode();
        var warnings = (await ReadAsync<SaveResponse<PersonResponse>>(response)).Warnings;
        Assert.Single(warnings); // born after their child; still after their parent, so no second warning
    }

    [Fact]
    public async Task Delete_edge_removes_only_the_edge()
    {
        var treeId = await CreateTreeAsync();
        var child = await CreatePersonAsync(treeId, "Child");
        var parent = await CreatePersonAsync(treeId, "Parent");
        var edgeId = await AddParentOkAsync(child, parent);

        var response = await Client.DeleteAsync($"/api/parent-child/{edgeId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await WithDbAsync(db => db.ParentChildren.CountAsync()));
        Assert.Equal(2, await WithDbAsync(db => db.People.CountAsync()));
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync($"/api/parent-child/{edgeId}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_person_removes_their_edges()
    {
        var treeId = await CreateTreeAsync();
        var grandparent = await CreatePersonAsync(treeId, "Grandparent");
        var parent = await CreatePersonAsync(treeId, "Parent");
        var child = await CreatePersonAsync(treeId, "Child");
        await AddParentOkAsync(parent, grandparent);
        await AddParentOkAsync(child, parent);

        var response = await Client.DeleteAsync($"/api/people/{parent}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await WithDbAsync(db => db.ParentChildren.CountAsync()));
        Assert.Equal(2, await WithDbAsync(db => db.People.CountAsync()));
    }
}
