namespace family_tree.Entities;

public class Person
{
    public int Id { get; set; }

    public int TreeId { get; set; }
    public FamilyTree Tree { get; set; } = null!;

    public required string GivenName { get; set; }
    public string Surname { get; set; } = "";
    public string? BirthSurname { get; set; }
    public Sex Sex { get; set; } = Sex.Unknown;
    public FuzzyDate? BirthDate { get; set; }
    public string? BirthPlace { get; set; }
    public FuzzyDate? DeathDate { get; set; }
    public string? Notes { get; set; }

    /// <summary>Edges to this person's parents (this person is the child).</summary>
    public List<ParentChild> ParentEdges { get; set; } = [];

    /// <summary>Edges to this person's children (this person is the parent).</summary>
    public List<ParentChild> ChildEdges { get; set; } = [];
}
