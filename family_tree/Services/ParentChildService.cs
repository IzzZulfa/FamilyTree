using family_tree.Data;
using family_tree.Dtos;
using family_tree.Entities;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Services;

public class ParentChildService(FamilyTreeDbContext db)
{
    private const int MaxBiologicalParents = 2;

    public async Task<SaveResponse<ParentChildResponse>> AddParentAsync(
        int childId, AddParentRequest request, CancellationToken ct)
    {
        var child = await db.People.FindAsync([childId], ct)
            ?? throw new NotFoundException($"Person {childId} does not exist.");

        // Rule 3: no self-relationships.
        if (request.ParentId == childId)
        {
            throw new ValidationException(ValidationCodes.SelfRelationship, "A person cannot be their own parent.");
        }

        var parent = await db.People.FindAsync([request.ParentId], ct)
            ?? throw new ValidationException(ValidationCodes.PersonNotFound,
                $"Parent {request.ParentId} does not exist.");

        // Rule 6: both people must belong to the same tree.
        if (parent.TreeId != child.TreeId)
        {
            throw new ValidationException(ValidationCodes.DifferentTrees,
                "The parent and child belong to different trees.");
        }

        // The unique index on (ParentId, ChildId) would also catch this, but as an opaque
        // database error; checking first gives the caller a clear message.
        if (await db.ParentChildren.AnyAsync(e => e.ParentId == parent.Id && e.ChildId == childId, ct))
        {
            throw new ValidationException(ValidationCodes.DuplicateParentChild,
                "This person is already recorded as a parent of the child.");
        }

        // Rule 2: at most two biological parents. Other types are unlimited.
        if (request.Type == ParentChildType.Biological)
        {
            var biologicalParents = await db.ParentChildren
                .CountAsync(e => e.ChildId == childId && e.Type == ParentChildType.Biological, ct);
            if (biologicalParents >= MaxBiologicalParents)
            {
                throw new ValidationException(ValidationCodes.TooManyBiologicalParents,
                    $"A person can have at most {MaxBiologicalParents} biological parents.");
            }
        }

        // Rule 1: no cycles. If the proposed parent already descends from the child,
        // the new edge would make the child their own ancestor.
        if (await IsDescendantAsync(parent.Id, ofPersonId: childId, child.TreeId, ct))
        {
            throw new ValidationException(ValidationCodes.Cycle,
                "This would make the person their own ancestor: the proposed parent is a descendant of the child.");
        }

        var edge = new ParentChild { ParentId = parent.Id, ChildId = childId, Type = request.Type };
        db.ParentChildren.Add(edge);
        await db.SaveChangesAsync(ct);

        // Rule 5 (warning only).
        var warnings = DateSanity.CheckParentChild(parent, child).ToList();
        return new SaveResponse<ParentChildResponse>(ParentChildResponse.From(edge), warnings);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await db.ParentChildren.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0)
        {
            throw new NotFoundException($"Parent-child relationship {id} does not exist.");
        }
    }

    /// <summary>
    /// Breadth-first search down from <paramref name="ofPersonId"/> through every child edge,
    /// returning true if <paramref name="personId"/> is reached.
    /// </summary>
    /// <remarks>
    /// Loads all of the tree's edges (just the two ids each) and walks them in memory,
    /// which is one query regardless of depth. Phase 2 will compare this with a recursive CTE.
    /// </remarks>
    private async Task<bool> IsDescendantAsync(int personId, int ofPersonId, int treeId, CancellationToken ct)
    {
        var edges = await db.ParentChildren
            .Where(e => e.Child.TreeId == treeId)
            .Select(e => new { e.ParentId, e.ChildId })
            .ToListAsync(ct);
        var childrenOf = edges.ToLookup(e => e.ParentId, e => e.ChildId);

        // The visited set matters: with pedigree collapse, the same person is reachable by several paths.
        var visited = new HashSet<int> { ofPersonId };
        var queue = new Queue<int>([ofPersonId]);
        while (queue.TryDequeue(out var current))
        {
            foreach (var next in childrenOf[current])
            {
                if (next == personId)
                {
                    return true;
                }
                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }
        return false;
    }
}
