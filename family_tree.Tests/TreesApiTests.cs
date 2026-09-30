using System.Net;
using family_tree.Dtos;
using family_tree.Tests.Infrastructure;

namespace family_tree.Tests;

public class TreesApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Create_then_list_returns_the_tree()
    {
        var response = await PostAsync("/api/trees", new CreateTreeRequest("  Smith family  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<TreeResponse>(response);
        Assert.Equal("Smith family", created.Name);

        var trees = await ReadAsync<List<TreeResponse>>(await Client.GetAsync("/api/trees"));
        Assert.Equal([created], trees);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "name": "" }""")]
    [InlineData("""{ "name": null }""")]
    public async Task Create_without_a_name_is_rejected(string json)
    {
        var response = await Client.PostAsync("/api/trees",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
