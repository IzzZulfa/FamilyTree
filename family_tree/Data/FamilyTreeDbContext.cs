using family_tree.Entities;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Data;

public class FamilyTreeDbContext(DbContextOptions<FamilyTreeDbContext> options) : DbContext(options)
{
    public DbSet<FamilyTree> FamilyTrees => Set<FamilyTree>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<ParentChild> ParentChildren => Set<ParentChild>();
    public DbSet<Partnership> Partnerships => Set<Partnership>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store every enum (including those inside owned FuzzyDates) as its name, e.g. 'Biological'.
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FamilyTree>(tree =>
        {
            tree.Property(t => t.Name).IsRequired();
        });

        modelBuilder.Entity<Person>(person =>
        {
            person.Property(p => p.GivenName).IsRequired();
            person.Property(p => p.Surname).IsRequired();

            // Deleting a tree must not silently cascade through its people and their edges.
            person.HasOne(p => p.Tree)
                .WithMany(t => t.People)
                .HasForeignKey(p => p.TreeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Owned types: columns birth_date_value, birth_date_precision, death_date_value, ...
            person.OwnsOne(p => p.BirthDate);
            person.OwnsOne(p => p.DeathDate);
        });

        modelBuilder.Entity<ParentChild>(edge =>
        {
            edge.HasOne(e => e.Parent)
                .WithMany(p => p.ChildEdges)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            edge.HasOne(e => e.Child)
                .WithMany(p => p.ParentEdges)
                .HasForeignKey(e => e.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            edge.HasIndex(e => new { e.ParentId, e.ChildId }).IsUnique();
        });

        modelBuilder.Entity<Partnership>(partnership =>
        {
            // The naming convention would produce "person1id"; spell these out for readability.
            partnership.Property(p => p.Person1Id).HasColumnName("person1_id");
            partnership.Property(p => p.Person2Id).HasColumnName("person2_id");

            // No inverse collections on Person: a partnership can reference a person from
            // either side, so a single "Partnerships" navigation wouldn't map cleanly.
            partnership.HasOne(p => p.Person1)
                .WithMany()
                .HasForeignKey(p => p.Person1Id)
                .OnDelete(DeleteBehavior.Restrict);

            partnership.HasOne(p => p.Person2)
                .WithMany()
                .HasForeignKey(p => p.Person2Id)
                .OnDelete(DeleteBehavior.Restrict);

            partnership.OwnsOne(p => p.StartDate);
            partnership.OwnsOne(p => p.EndDate);
        });
    }
}
