namespace family_tree.Entities;

/// <summary>A directed edge in the tree: <see cref="Parent"/> is a parent of <see cref="Child"/>.</summary>
public class ParentChild
{
    public int Id { get; set; }

    public int ParentId { get; set; }
    public Person Parent { get; set; } = null!;

    public int ChildId { get; set; }
    public Person Child { get; set; } = null!;

    public ParentChildType Type { get; set; }
}
