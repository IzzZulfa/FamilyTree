using family_tree.Data;
using family_tree.Dtos;
using family_tree.Entities;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Services;

public class TreeService(FamilyTreeDbContext db)
{
    public async Task<List<TreeResponse>> ListAsync(CancellationToken ct)
    {
        return await db.FamilyTrees
            .OrderBy(t => t.Name)
            .Select(t => new TreeResponse(t.Id, t.Name))
            .ToListAsync(ct);
    }

    public async Task<TreeResponse> CreateAsync(CreateTreeRequest request, CancellationToken ct)
    {
        var tree = new FamilyTree { Name = request.Name.Trim() };
        db.FamilyTrees.Add(tree);
        await db.SaveChangesAsync(ct);
        return TreeResponse.From(tree);
    }
}
