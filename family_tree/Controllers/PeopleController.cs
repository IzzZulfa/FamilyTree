using family_tree.Dtos;
using family_tree.Services;
using Microsoft.AspNetCore.Mvc;

namespace family_tree.Controllers;

[ApiController]
[Route("api")]
public class PeopleController(PersonService people) : ControllerBase
{
    [HttpGet("trees/{treeId:int}/people")]
    public async Task<ActionResult<List<PersonResponse>>> ListInTree(int treeId, CancellationToken ct)
    {
        return await people.ListInTreeAsync(treeId, ct);
    }

    [HttpPost("trees/{treeId:int}/people")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<SaveResponse<PersonResponse>>> Create(
        int treeId, SavePersonRequest request, CancellationToken ct)
    {
        var result = await people.CreateAsync(treeId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Data.Id }, result);
    }

    [HttpGet("people/{id:int}")]
    public async Task<ActionResult<PersonResponse>> Get(int id, CancellationToken ct)
    {
        return await people.GetAsync(id, ct);
    }

    [HttpPut("people/{id:int}")]
    public async Task<ActionResult<SaveResponse<PersonResponse>>> Update(
        int id, SavePersonRequest request, CancellationToken ct)
    {
        return await people.UpdateAsync(id, request, ct);
    }

    [HttpDelete("people/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await people.DeleteAsync(id, ct);
        return NoContent();
    }
}
