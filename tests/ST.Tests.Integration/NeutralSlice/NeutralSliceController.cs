using Asp.Versioning;
using ST.Api.Common.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ST.Tests.Integration.NeutralSlice;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v1/certification/notes")]
public sealed class NeutralSliceController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("notes.create")]
    public async Task<IActionResult> Create([FromBody] string title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 100) return BadRequest();
        return Ok(await sender.Send(new CreateNeutralNote(title), cancellationToken));
    }
    [HttpGet("{id:guid}")]
    [RequirePermission("notes.view")]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken)
    {
        var title = await sender.Send(new ReadNeutralNote(id), cancellationToken);
        return title is null ? NotFound() : Ok(title);
    }
}
