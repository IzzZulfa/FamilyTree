using family_tree.Entities;

namespace family_tree.Dtos;

public record AddParentRequest(int ParentId, ParentChildType Type);

public record ParentChildResponse(int Id, int ParentId, int ChildId, ParentChildType Type)
{
    public static ParentChildResponse From(ParentChild edge) =>
        new(edge.Id, edge.ParentId, edge.ChildId, edge.Type);
}
