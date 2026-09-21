using FacilitiesMgmt.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilitiesMgmt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(SpendReportService spendReport) : ControllerBase
{
    /// <summary>
    /// Total spend per site over a date range, for the caller's organisation only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The organisation is not a parameter and cannot be made one — it comes from the
    /// token, and the query filter applies it. There is nothing here for an attacker to
    /// tamper with.
    /// </para>
    /// <para>
    /// The range is half-open: <c>fromUtc</c> inclusive, <c>toUtc</c> exclusive, so
    /// consecutive months partition cleanly. Bound as a model rather than as loose
    /// parameters so the registered validator runs through the global validation filter.
    /// </para>
    /// </remarks>
    [HttpGet("spend")]
    [ProducesResponseType<SpendReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpendReportDto>> Spend(
        [FromQuery] SpendReportQuery query,
        CancellationToken ct) =>
        Ok(await spendReport.GetSiteSpendAsync(query, ct));
}
