using System.ComponentModel.DataAnnotations;
using family_tree.Entities;

namespace family_tree.Dtos;

public record CreateTreeRequest([Required, StringLength(200)] string Name);

public record TreeResponse(int Id, string Name)
{
    public static TreeResponse From(FamilyTree tree) => new(tree.Id, tree.Name);
}
