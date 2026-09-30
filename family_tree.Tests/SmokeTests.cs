using family_tree.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Tests;

public class SmokeTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task App_uses_the_test_database()
    {
        var databaseName = await WithDbAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));

        Assert.Equal("familytree_test", databaseName);
    }
}
