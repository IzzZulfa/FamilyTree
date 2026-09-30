namespace family_tree.Tests.Infrastructure;

/// <summary>
/// All tests that touch the database belong to this collection. xUnit runs tests in the
/// same collection one at a time, which stops them from truncating each other's data.
/// </summary>
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Database";
}
