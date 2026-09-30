using family_tree.Dtos;
using family_tree.Services;
using Microsoft.AspNetCore.Mvc;

namespace family_tree.Controllers;

[ApiController]
[Route("api/trees")]
public class TreesController(TreeService trees) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TreeResponse>>> List(CancellationToken ct)
    {
        return await trees.ListAsync(ct);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TreeResponse>> Create(CreateTreeRequest request, CancellationToken ct)
    {
        var tree = await trees.CreateAsync(request, ct);
        // The spec has no GET /api/trees/{id}, so there's no URL to put in a Location header.
        return StatusCode(StatusCodes.Status201Created, tree);
    }
}
