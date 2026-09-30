using family_tree.Entities;
using family_tree.Seed;
using family_tree.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace family_tree.Tests;

public class SeedLoaderTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static readonly string HartleyPath = Path.Combine(AppContext.BaseDirectory, "seed", "hartley-family.json");

    private async Task<SeedResult> LoadAsync(SeedFile file)
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SeedLoader>().LoadAsync(file, CancellationToken.None);
    }

    private Task<(int Trees, int People, int ParentLinks, int Partnerships)> CountAsync() =>
        WithDbAsync(async db => (
            await db.FamilyTrees.CountAsync(),
            await db.People.CountAsync(),
            await db.ParentChildren.CountAsync(),
            await db.Partnerships.CountAsync()));

    [Fact]
    public async Task Hartley_family_loads_cleanly()
    {
        var file = await SeedFile.ReadAsync(HartleyPath, CancellationToken.None);

        var result = await LoadAsync(file);

        Assert.Empty(result.Warnings);
        Assert.Equal((1, file.People.Count, file.Parents.Count, file.Partnerships.Count), await CountAsync());
    }

    [Fact]
    public async Task Loading_again_replaces_the_tree_and_leaves_other_trees_alone()
    {
        var otherTree = await CreateTreeAsync("Another family");
        await CreatePersonAsync(otherTree, "Unrelated");
        var file = await SeedFile.ReadAsync(HartleyPath, CancellationToken.None);

        await LoadAsync(file);
        await LoadAsync(file);

        Assert.Equal((2, file.People.Count + 1, file.Parents.Count, file.Partnerships.Count), await CountAsync());
    }

    [Fact]
    public async Task Rule_violation_names_the_entry_and_saves_nothing()
    {
        var file = new SeedFile("Loop",
            [new SeedPerson("a", "A"), new SeedPerson("b", "B")],
            [new SeedParentLink("b", "a", ParentChildType.Biological), new SeedParentLink("a", "b", ParentChildType.Biological)],
            []);

        var ex = await Assert.ThrowsAsync<SeedException>(() => LoadAsync(file));

        Assert.Contains("'b' -> 'a'", ex.Message);
        Assert.Contains("(cycle)", ex.Message);
        Assert.Equal((0, 0, 0, 0), await CountAsync());
    }

    [Fact]
    public async Task Unknown_person_key_is_reported()
    {
        var file = new SeedFile("Typo",
            [new SeedPerson("a", "A")],
            [new SeedParentLink("a", "nobody", ParentChildType.Biological)],
            []);

        var ex = await Assert.ThrowsAsync<SeedException>(() => LoadAsync(file));

        Assert.Contains("'nobody'", ex.Message);
    }

    [Fact]
    public async Task Duplicate_person_key_is_reported()
    {
        var file = new SeedFile("Dupes", [new SeedPerson("a", "A"), new SeedPerson("a", "Also A")], [], []);

        var ex = await Assert.ThrowsAsync<SeedException>(() => LoadAsync(file));

        Assert.Contains("more than once", ex.Message);
    }
}
