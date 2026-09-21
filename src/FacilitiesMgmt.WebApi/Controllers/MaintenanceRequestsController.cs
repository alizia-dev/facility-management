using FacilitiesMgmt.Application.MaintenanceRequests;
using FacilitiesMgmt.Domain.Enums;
using FacilitiesMgmt.WebApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilitiesMgmt.WebApi.Controllers;

/// <summary>
/// The request lifecycle.
/// </summary>
/// <remarks>
/// Note how thin these actions are, and note what is missing from every one of them:
/// there is no organisation id in any route, query string, or body. The caller cannot
/// name a tenant, so the caller cannot name the wrong one.
/// <para>
/// The <c>[Authorize(Policy = ...)]</c> attributes below are a first gate, not the
/// enforcement. The decision rules — Approver role, and not your own request — are
/// enforced inside <c>MaintenanceRequest.Approve</c>, which is reached whether or not
/// anyone remembered an attribute. The attribute produces a clean 403 early; the domain
/// makes the rule true.
/// </para>
/// </remarks>
[ApiController]
[Authorize]
[Route("api/requests")]
public sealed class MaintenanceRequestsController(MaintenanceRequestService requests) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MaintenanceRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MaintenanceRequestDto>>> List(
        [FromQuery] RequestStatus? status,
        [FromQuery] Guid? siteId,
        CancellationToken ct) =>
        Ok(await requests.ListAsync(status, siteId, ct));

    /// <summary>
    /// A request by id. Another organisation's id returns 404 — see the note on
    /// NotFoundException for why this is a 404 and not a 403.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MaintenanceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRequestDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await requests.GetAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<MaintenanceRequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRequestDto>> Create(
        CreateMaintenanceRequestDto dto,
        CancellationToken ct)
    {
        var created = await requests.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.ApproverOnly)]
    [ProducesResponseType<MaintenanceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDto>> Approve(Guid id, CancellationToken ct) =>
        Ok(await requests.ApproveAsync(id, ct));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.ApproverOnly)]
    [ProducesResponseType<MaintenanceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDto>> Reject(
        Guid id,
        RejectMaintenanceRequestDto dto,
        CancellationToken ct) =>
        Ok(await requests.RejectAsync(id, dto.Reason, ct));

    /// <summary>
    /// Records the money actually spent and closes the request.
    /// </summary>
    /// <remarks>
    /// Restricted to Approvers. The brief does not say who may record an actual cost;
    /// this is the assumption, documented in DECISIONS.md — recording a financial figure
    /// that lands in the spend report is a finance action, and the role that already
    /// commits the organisation to spending is the one that should confirm what was
    /// spent. Revisit if site staff should close out their own work.
    /// </remarks>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = AuthorizationPolicies.ApproverOnly)]
    [ProducesResponseType<MaintenanceRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDto>> Complete(
        Guid id,
        CompleteMaintenanceRequestDto dto,
        CancellationToken ct) =>
        Ok(await requests.CompleteAsync(id, dto.ActualCost, ct));

    /// <summary>
    /// The request's audit trail. Read-only — and note there is no PUT, PATCH, or DELETE
    /// counterpart anywhere in this API for any role. That absence is the audit
    /// integrity control at this layer.
    /// </summary>
    [HttpGet("{id:guid}/audit")]
    [ProducesResponseType<IReadOnlyList<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AuditEntryDto>>> AuditTrail(Guid id, CancellationToken ct) =>
        Ok(await requests.GetAuditTrailAsync(id, ct));
}
