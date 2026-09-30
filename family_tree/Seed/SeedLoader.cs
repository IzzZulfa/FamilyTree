using family_tree.Data;
using family_tree.Dtos;
using family_tree.Services;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Seed;

public record SeedResult(int TreeId, int People, int ParentLinks, int Partnerships, IReadOnlyList<string> Warnings);

/// <summary>
/// Loads a <see cref="SeedFile"/> through the normal services, so seed data must pass every
/// validation rule. Runs in one transaction: a failure part-way through leaves nothing behind.
/// </summary>
public class SeedLoader(
    FamilyTreeDbContext db,
    TreeService trees,
    PersonService people,
    ParentChildService parentChild,
    PartnershipService partnerships)
{
    public async Task<SeedResult> LoadAsync(SeedFile file, CancellationToken ct)
    {
        // The services call SaveChangesAsync themselves; EF enlists those calls in this transaction.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await DeleteTreesNamedAsync(file.Tree.Trim(), ct);
        var tree = await trees.CreateAsync(new CreateTreeRequest(file.Tree), ct);

        var ids = new Dictionary<string, int>();
        var warnings = new List<string>();

        foreach (var person in file.People)
        {
            if (string.IsNullOrWhiteSpace(person.GivenName))
            {
                throw new SeedException($"Person '{person.Key}' has no given name.");
            }
            if (ids.ContainsKey(person.Key))
            {
                throw new SeedException($"Person key '{person.Key}' is used more than once.");
            }

            var result = await people.CreateAsync(tree.Id, person.ToRequest(), ct);
            ids[person.Key] = result.Data.Id;
            warnings.AddRange(result.Warnings);
        }

        foreach (var link in file.Parents)
        {
            var description = $"Parent link '{link.Parent}' -> '{link.Child}'";
            var result = await RunAsync(description, () => parentChild.AddParentAsync(
                Resolve(ids, link.Child, description),
                new AddParentRequest(Resolve(ids, link.Parent, description), link.Type),
                ct));
            warnings.AddRange(result.Warnings);
        }

        foreach (var partnership in file.Partnerships)
        {
            var description = $"Partnership '{partnership.Person1}' + '{partnership.Person2}'";
            var result = await RunAsync(description, () => partnerships.AddAsync(
                Resolve(ids, partnership.Person1, description),
                new AddPartnershipRequest(Resolve(ids, partnership.Person2, description), partnership.Type,
                    partnership.StartDate, partnership.EndDate, partnership.EndReason),
                ct));
            warnings.AddRange(result.Warnings);
        }

        await transaction.CommitAsync(ct);
        return new SeedResult(tree.Id, file.People.Count, file.Parents.Count, file.Partnerships.Count, warnings);
    }

    /// <summary>Makes re-seeding repeatable: removes any earlier copy of this tree, edges first.</summary>
    private async Task DeleteTreesNamedAsync(string name, CancellationToken ct)
    {
        var treeIds = db.FamilyTrees.Where(t => t.Name == name).Select(t => t.Id);

        // Both ends of an edge are always in the same tree (rule 6), so checking one end is enough.
        await db.ParentChildren.Where(e => treeIds.Contains(e.Child.TreeId)).ExecuteDeleteAsync(ct);
        await db.Partnerships.Where(p => treeIds.Contains(p.Person1.TreeId)).ExecuteDeleteAsync(ct);
        await db.People.Where(p => treeIds.Contains(p.TreeId)).ExecuteDeleteAsync(ct);
        await db.FamilyTrees.Where(t => t.Name == name).ExecuteDeleteAsync(ct);
    }

    private static int Resolve(Dictionary<string, int> ids, string key, string description) =>
        ids.TryGetValue(key, out var id)
            ? id
            : throw new SeedException($"{description}: no person has the key '{key}'.");

    /// <summary>Re-throws a rule violation with the seed entry that caused it.</summary>
    private static async Task<T> RunAsync<T>(string description, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (ValidationException ex)
        {
            throw new SeedException($"{description}: {ex.Message} ({ex.Code})", ex);
        }
    }
}
