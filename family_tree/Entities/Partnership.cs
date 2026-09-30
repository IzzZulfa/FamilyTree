namespace family_tree.Entities;

/// <summary>
/// An undirected edge between two partners. Which person is Person1 and which is
/// Person2 carries no meaning; queries must check both orders.
/// </summary>
public class Partnership
{
    public int Id { get; set; }

    public int Person1Id { get; set; }
    public Person Person1 { get; set; } = null!;

    public int Person2Id { get; set; }
    public Person Person2 { get; set; } = null!;

    public PartnershipType Type { get; set; }
    public FuzzyDate? StartDate { get; set; }
    public FuzzyDate? EndDate { get; set; }
    public PartnershipEndReason? EndReason { get; set; }
}
