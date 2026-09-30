using family_tree.Dtos;
using family_tree.Services;
using Microsoft.AspNetCore.Mvc;

namespace family_tree.Controllers;

[ApiController]
[Route("api")]
public class PartnershipsController(PartnershipService partnerships) : ControllerBase
{
    /// <summary>Records a partnership between person <c>id</c> and <c>partnerId</c>.</summary>
    [HttpPost("people/{id:int}/partnerships")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<SaveResponse<PartnershipResponse>>> Add(
        int id, AddPartnershipRequest request, CancellationToken ct)
    {
        var result = await partnerships.AddAsync(id, request, ct);
        // There's no GET for a single partnership, so no Location header.
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("partnerships/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await partnerships.DeleteAsync(id, ct);
        return NoContent();
    }
}
