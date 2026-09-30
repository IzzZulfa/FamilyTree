using family_tree.Entities;

namespace family_tree.Services;

/// <summary>
/// Validation rule 5: date sanity. These checks produce warnings, not errors,
/// because real genealogical records are often inconsistent.
/// </summary>
/// <remarks>
/// Fuzzy dates are compared by their <see cref="FuzzyDate.Value"/> only; precision is ignored.
/// That can flag borderline cases (e.g. two "circa 1850" dates), which is acceptable for a warning.
/// </remarks>
public static class DateSanity
{
    public static IEnumerable<string> CheckLifespan(Person person)
    {
        if (person.BirthDate is not null && person.DeathDate is not null
            && person.DeathDate.Value < person.BirthDate.Value)
        {
            yield return $"{DisplayName(person)}'s death date ({person.DeathDate.Value:O}) "
                + $"is before their birth date ({person.BirthDate.Value:O}).";
        }
    }

    public static IEnumerable<string> CheckParentChild(Person parent, Person child)
    {
        if (parent.BirthDate is not null && child.BirthDate is not null
            && parent.BirthDate.Value >= child.BirthDate.Value)
        {
            yield return $"Parent {DisplayName(parent)} ({parent.BirthDate.Value:O}) "
                + $"was not born before child {DisplayName(child)} ({child.BirthDate.Value:O}).";
        }
    }

    private static string DisplayName(Person person) => $"{person.GivenName} {person.Surname}".Trim();
}
