using family_tree.Entities;

namespace family_tree.Dtos;

public record FuzzyDateDto(DateOnly Value, DatePrecision Precision = DatePrecision.Exact)
{
    public static FuzzyDateDto? From(FuzzyDate? date) =>
        date is null ? null : new FuzzyDateDto(date.Value, date.Precision);

    public FuzzyDate ToEntity() => new() { Value = Value, Precision = Precision };
}

/// <summary>
/// Wraps the result of a create/update together with any non-blocking date-sanity warnings.
/// </summary>
public record SaveResponse<T>(T Data, IReadOnlyList<string> Warnings);
