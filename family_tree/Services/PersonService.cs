using family_tree.Data;
using family_tree.Dtos;
using family_tree.Entities;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Services;

public class PersonService(FamilyTreeDbContext db)
{
    public async Task<List<PersonResponse>> ListInTreeAsync(int treeId, CancellationToken ct)
    {
        await EnsureTreeExistsAsync(treeId, ct);

        var people = await db.People
            .Where(p => p.TreeId == treeId)
            .OrderBy(p => p.Surname).ThenBy(p => p.GivenName).ThenBy(p => p.Id)
            .ToListAsync(ct);
        return people.Select(PersonResponse.From).ToList();
    }

    public async Task<PersonResponse> GetAsync(int id, CancellationToken ct)
    {
        var person = await db.People.FindAsync([id], ct)
            ?? throw new NotFoundException($"Person {id} does not exist.");
        return PersonResponse.From(person);
    }

    public async Task<SaveResponse<PersonResponse>> CreateAsync(
        int treeId, SavePersonRequest request, CancellationToken ct)
    {
        await EnsureTreeExistsAsync(treeId, ct);

        var person = new Person { TreeId = treeId, GivenName = "" };
        Apply(request, person);
        db.People.Add(person);
        await db.SaveChangesAsync(ct);

        var warnings = DateSanity.CheckLifespan(person).ToList();
        return new SaveResponse<PersonResponse>(PersonResponse.From(person), warnings);
    }

    public async Task<SaveResponse<PersonResponse>> UpdateAsync(
        int id, SavePersonRequest request, CancellationToken ct)
    {
        // Load parents and children too: changing a birth date can make an existing edge look wrong.
        var person = await db.People
            .Include(p => p.ParentEdges).ThenInclude(e => e.Parent)
            .Include(p => p.ChildEdges).ThenInclude(e => e.Child)
            .SingleOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException($"Person {id} does not exist.");

        Apply(request, person);
        await db.SaveChangesAsync(ct);

        var warnings = DateSanity.CheckLifespan(person)
            .Concat(person.ParentEdges.SelectMany(e => DateSanity.CheckParentChild(e.Parent, person)))
            .Concat(person.ChildEdges.SelectMany(e => DateSanity.CheckParentChild(person, e.Child)))
            .ToList();
        return new SaveResponse<PersonResponse>(PersonResponse.From(person), warnings);
    }

    /// <summary>Deletes the person and every edge that touches them, in one transaction.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // The foreign keys are DeleteBehavior.Restrict, so the edges must go first.
        await db.ParentChildren
            .Where(e => e.ParentId == id || e.ChildId == id)
            .ExecuteDeleteAsync(ct);
        await db.Partnerships
            .Where(p => p.Person1Id == id || p.Person2Id == id)
            .ExecuteDeleteAsync(ct);
        var deleted = await db.People.Where(p => p.Id == id).ExecuteDeleteAsync(ct);

        if (deleted == 0)
        {
            // Nothing else was deleted either, but roll back explicitly for clarity.
            await transaction.RollbackAsync(ct);
            throw new NotFoundException($"Person {id} does not exist.");
        }

        await transaction.CommitAsync(ct);
    }

    private async Task EnsureTreeExistsAsync(int treeId, CancellationToken ct)
    {
        if (!await db.FamilyTrees.AnyAsync(t => t.Id == treeId, ct))
        {
            throw new NotFoundException($"Tree {treeId} does not exist.");
        }
    }

    private static void Apply(SavePersonRequest request, Person person)
    {
        person.GivenName = request.GivenName.Trim();
        person.Surname = request.Surname?.Trim() ?? "";
        person.BirthSurname = request.BirthSurname?.Trim();
        person.Sex = request.Sex;
        person.BirthDate = request.BirthDate?.ToEntity();
        person.BirthPlace = request.BirthPlace?.Trim();
        person.DeathDate = request.DeathDate?.ToEntity();
        person.Notes = request.Notes;
    }
}
