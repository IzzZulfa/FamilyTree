using family_tree.Entities;

namespace family_tree.Dtos;

public record AddParentRequest(int ParentId, ParentChildType Type);

public record ParentChildResponse(int Id, int ParentId, int ChildId, ParentChildType Type)
{
    public static ParentChildResponse From(ParentChild edge) =>
        new(edge.Id, edge.ParentId, edge.ChildId, edge.Type);
}

public record AddPartnershipRequest(
    int PartnerId,
    PartnershipType Type,
    FuzzyDateDto? StartDate = null,
    FuzzyDateDto? EndDate = null,
    PartnershipEndReason? EndReason = null);

public record PartnershipResponse(
    int Id,
    int Person1Id,
    int Person2Id,
    PartnershipType Type,
    FuzzyDateDto? StartDate,
    FuzzyDateDto? EndDate,
    PartnershipEndReason? EndReason)
{
    public static PartnershipResponse From(Partnership partnership) => new(
        partnership.Id,
        partnership.Person1Id,
        partnership.Person2Id,
        partnership.Type,
        FuzzyDateDto.From(partnership.StartDate),
        FuzzyDateDto.From(partnership.EndDate),
        partnership.EndReason);
}
