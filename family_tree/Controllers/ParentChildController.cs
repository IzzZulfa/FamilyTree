using family_tree.Dtos;
using family_tree.Services;
using Microsoft.AspNetCore.Mvc;

namespace family_tree.Controllers;

[ApiController]
[Route("api")]
public class ParentChildController(ParentChildService parentChild) : ControllerBase
{
    /// <summary>Records <c>parentId</c> as a parent of person <c>id</c>.</summary>
    [HttpPost("people/{id:int}/parents")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<SaveResponse<ParentChildResponse>>> AddParent(
        int id, AddParentRequest request, CancellationToken ct)
    {
        var result = await parentChild.AddParentAsync(id, request, ct);
        // There's no GET for a single edge, so no Location header.
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("parent-child/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await parentChild.DeleteAsync(id, ct);
        return NoContent();
    }
}
