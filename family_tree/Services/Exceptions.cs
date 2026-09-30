namespace family_tree.Services;

/// <summary>
/// Thrown by services when a request breaks a business rule. Turned into a 400 ProblemDetails
/// response by <see cref="Infrastructure.DomainExceptionHandler"/>.
/// </summary>
/// <param name="code">A stable, machine-readable identifier for the rule, e.g. "cycle".</param>
/// <remarks>
/// Not to be confused with System.ComponentModel.DataAnnotations.ValidationException, which
/// MVC uses for attribute validation. The services never import that namespace.
/// </remarks>
public class ValidationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Thrown when the resource named in the URL doesn't exist. Turned into a 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Stable codes for each validation rule, returned in the ProblemDetails "code" field.</summary>
public static class ValidationCodes
{
    public const string Cycle = "cycle";
    public const string TooManyBiologicalParents = "too-many-biological-parents";
    public const string SelfRelationship = "self-relationship";
    public const string OverlappingPartnership = "overlapping-partnership";
    public const string DifferentTrees = "different-trees";
    public const string DuplicateParentChild = "duplicate-parent-child";
    public const string PersonNotFound = "person-not-found";
    public const string InvalidDateRange = "invalid-date-range";
}
