using System.Net;
using family_tree.Dtos;
using family_tree.Entities;
using family_tree.Services;
using family_tree.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Tests;

public class PartnershipApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private Task<HttpResponseMessage> AddPartnershipAsync(int personId, AddPartnershipRequest request) =>
        PostAsync($"/api/people/{personId}/partnerships", request);

    private async Task<int> AddPartnershipOkAsync(int personId, AddPartnershipRequest request)
    {
        var response = await AddPartnershipAsync(personId, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<SaveResponse<PartnershipResponse>>(response)).Data.Id;
    }

    private async Task<(int A, int B)> CreateCoupleAsync()
    {
        var treeId = await CreateTreeAsync();
        return (await CreatePersonAsync(treeId, "Alex"), await CreatePersonAsync(treeId, "Sam"));
    }

    [Fact]
    public async Task Add_partnership_creates_the_edge()
    {
        var (a, b) = await CreateCoupleAsync();
        var request = new AddPartnershipRequest(b, PartnershipType.Married,
            StartDate: Date(1970, 6, 1), EndDate: Date(1990), EndReason: PartnershipEndReason.Divorce);

        var response = await AddPartnershipAsync(a, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await ReadAsync<SaveResponse<PartnershipResponse>>(response);
        Assert.Equal(new PartnershipResponse(result.Data.Id, a, b, PartnershipType.Married,
            Date(1970, 6, 1), Date(1990), PartnershipEndReason.Divorce), result.Data);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Add_partnership_for_missing_person_returns_404()
    {
        var (a, _) = await CreateCoupleAsync();

        var response = await AddPartnershipAsync(999, new AddPartnershipRequest(a, PartnershipType.Partner));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Missing_partner_is_rejected()
    {
        var (a, _) = await CreateCoupleAsync();

        var response = await AddPartnershipAsync(a, new AddPartnershipRequest(999, PartnershipType.Partner));

        await AssertValidationErrorAsync(response, ValidationCodes.PersonNotFound);
    }

    // Rule 3: no self-relationships.
    [Fact]
    public async Task Person_cannot_partner_themselves()
    {
        var (a, _) = await CreateCoupleAsync();

        var response = await AddPartnershipAsync(a, new AddPartnershipRequest(a, PartnershipType.Married));

        await AssertValidationErrorAsync(response, ValidationCodes.SelfRelationship);
    }

    // Rule 6: same tree.
    [Fact]
    public async Task Partner_from_another_tree_is_rejected()
    {
        var a = await CreatePersonAsync(await CreateTreeAsync("A"), "Alex");
        var b = await CreatePersonAsync(await CreateTreeAsync("B"), "Sam");

        var response = await AddPartnershipAsync(a, new AddPartnershipRequest(b, PartnershipType.Married));

        await AssertValidationErrorAsync(response, ValidationCodes.DifferentTrees);
    }

    [Fact]
    public async Task End_before_start_is_rejected()
    {
        var (a, b) = await CreateCoupleAsync();

        var response = await AddPartnershipAsync(a, new AddPartnershipRequest(b, PartnershipType.Married,
            StartDate: Date(1990), EndDate: Date(1980)));

        await AssertValidationErrorAsync(response, ValidationCodes.InvalidDateRange);
    }

    // Rule 4: no duplicate partnerships between the same pair with overlapping dates.
    [Fact]
    public async Task Undated_duplicate_is_rejected()
    {
        var (a, b) = await CreateCoupleAsync();
        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married));

        var response = await AddPartnershipAsync(a, new AddPartnershipRequest(b, PartnershipType.Partner));

        await AssertValidationErrorAsync(response, ValidationCodes.OverlappingPartnership);
    }

    [Fact]
    public async Task Duplicate_is_detected_in_either_direction()
    {
        var (a, b) = await CreateCoupleAsync();
        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married));

        var response = await AddPartnershipAsync(b, new AddPartnershipRequest(a, PartnershipType.Married));

        await AssertValidationErrorAsync(response, ValidationCodes.OverlappingPartnership);
    }

    [Fact]
    public async Task Overlapping_dated_partnerships_are_rejected()
    {
        var (a, b) = await CreateCoupleAsync();
        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married,
            StartDate: Date(1970), EndDate: Date(1980)));

        var response = await AddPartnershipAsync(a, new AddPartnershipRequest(b, PartnershipType.Married,
            StartDate: Date(1975)));

        await AssertValidationErrorAsync(response, ValidationCodes.OverlappingPartnership);
    }

    [Fact]
    public async Task Remarrying_the_same_person_after_divorce_is_allowed()
    {
        var (a, b) = await CreateCoupleAsync();
        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married,
            StartDate: Date(1970), EndDate: Date(1980), EndReason: PartnershipEndReason.Divorce));

        await AddPartnershipOkAsync(b, new AddPartnershipRequest(a, PartnershipType.Married,
            StartDate: Date(1985)));
    }

    [Fact]
    public async Task Remarrying_on_the_day_of_the_divorce_is_allowed()
    {
        var (a, b) = await CreateCoupleAsync();
        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married,
            StartDate: Date(1970), EndDate: Date(1980, 5, 1)));

        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married,
            StartDate: Date(1980, 5, 1)));
    }

    [Fact]
    public async Task Partnerships_with_different_people_do_not_conflict()
    {
        var treeId = await CreateTreeAsync();
        var a = await CreatePersonAsync(treeId, "Alex");
        var b = await CreatePersonAsync(treeId, "Sam");
        var c = await CreatePersonAsync(treeId, "Jo");
        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married));

        await AddPartnershipOkAsync(a, new AddPartnershipRequest(c, PartnershipType.Married));
    }

    [Fact]
    public async Task Delete_partnership_removes_only_the_edge()
    {
        var (a, b) = await CreateCoupleAsync();
        var id = await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Engaged));

        var response = await Client.DeleteAsync($"/api/partnerships/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await WithDbAsync(db => db.Partnerships.CountAsync()));
        Assert.Equal(2, await WithDbAsync(db => db.People.CountAsync()));
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync($"/api/partnerships/{id}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_person_removes_their_partnerships()
    {
        var (a, b) = await CreateCoupleAsync();
        await AddPartnershipOkAsync(a, new AddPartnershipRequest(b, PartnershipType.Married));

        var response = await Client.DeleteAsync($"/api/people/{b}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await WithDbAsync(db => db.Partnerships.CountAsync()));
        Assert.Equal(1, await WithDbAsync(db => db.People.CountAsync()));
    }
}
