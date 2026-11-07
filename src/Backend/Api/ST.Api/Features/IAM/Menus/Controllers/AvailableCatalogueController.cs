using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Modules;
namespace ST.Api.Features.IAM.Menus.Controllers;
[ApiController, ApiVersion("1.0"), Authorize, ST.Api.Common.Authorization.RequirePermission("menus.view")]
[ST.Api.Common.Authorization.RequireModule("iam")]
[Route("api/v{version:apiVersion}/modules/available")]
public sealed class AvailableCatalogueController(ISender sender) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => HandleResponse(await sender.Send(new GetAvailableCatalogueQuery(), cancellationToken).ConfigureAwait(false));
}
