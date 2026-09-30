namespace family_tree.Entities;

public class FamilyTree
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public List<Person> People { get; set; } = [];
}
