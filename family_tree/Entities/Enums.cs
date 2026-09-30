namespace family_tree.Entities;

// All enums are stored as strings in the database (see FamilyTreeDbContext.ConfigureConventions),
// so reordering or inserting members is safe.

public enum Sex
{
    Male,
    Female,
    Unknown,
}

public enum ParentChildType
{
    Biological,
    Adoptive,
    Step,
    Foster,
}

public enum PartnershipType
{
    Married,
    Partner,
    Engaged,
}

public enum PartnershipEndReason
{
    Divorce,
    Death,
}

public enum DatePrecision
{
    Exact,
    Month,
    Year,
    Circa,
}
