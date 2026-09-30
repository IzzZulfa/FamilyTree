namespace family_tree.Entities;

/// <summary>
/// A date that may only be partly known, e.g. "March 1921" or "circa 1850".
/// Mapped as an EF Core owned type: it has no table or key of its own and its
/// properties become columns on the owner (e.g. people.birth_date_value).
/// </summary>
/// <remarks>
/// <see cref="Value"/> always holds a full date. For partial dates, use the first
/// day of the known period (1921-03-01 for "March 1921", 1850-01-01 for "circa 1850").
/// </remarks>
public class FuzzyDate
{
    public DateOnly Value { get; set; }
    public DatePrecision Precision { get; set; }
}
