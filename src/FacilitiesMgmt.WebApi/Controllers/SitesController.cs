using FacilitiesMgmt.Application.Sites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilitiesMgmt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/sites")]
public sealed class SitesController(SiteService siteService) : ControllerBase
{
    /// <summary>
    /// The caller's sites. No organisation parameter — there is nothing for a caller to
    /// pass, because the tenant comes from the token and the query filter does the rest.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SiteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> List(CancellationToken ct) =>
        Ok(await siteService.ListAsync(ct));
}
