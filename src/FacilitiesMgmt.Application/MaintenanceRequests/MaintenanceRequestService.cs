using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Application.Common;
using FacilitiesMgmt.Domain.Entities;
using FacilitiesMgmt.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FacilitiesMgmt.Application.MaintenanceRequests;

/// <summary>
/// The request lifecycle use cases.
/// <para>
/// Every query below looks tenant-unaware on purpose. There is no
/// <c>.Where(r =&gt; r.OrganizationId == ...)</c> anywhere in this file, and no
/// organisation id is passed in as an argument. The global query filter appends the
/// predicate to all of them. That is the design: a new method added here is scoped
/// whether or not its author thought about tenancy.
/// </para>
/// </summary>
public sealed class MaintenanceRequestService(
    IAppDbContext db,
    ICurrentTenant tenant,
    IAuditWriter audit,
    IClock clock)
{
    public async Task<MaintenanceRequestDto> CreateAsync(CreateMaintenanceRequestDto dto, CancellationToken ct = default)
    {
        // The organisation row is not tenant-filtered (it IS the tenant), so this is
        // one of the few places an explicit organisation predicate is correct.
        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == tenant.OrganizationId, ct)
            ?? throw new NotFoundException("Organization", tenant.OrganizationId);

        // Filtered. Handing in another tenant's SiteId lands here as "not found",
        // which is the correct answer and also the secure one — see NotFoundException.
        var siteExists = await db.Sites.AnyAsync(s => s.Id == dto.SiteId, ct);
        if (!siteExists)
        {
            throw new NotFoundException("Site", dto.SiteId);
        }

        var request = MaintenanceRequest.Raise(
            organizationId: tenant.OrganizationId,
            siteId: dto.SiteId,
            requestedByUserId: tenant.UserId,
            description: dto.Description,
            estimatedCost: dto.EstimatedCost,
            organizationThreshold: organization.CostThreshold,
            utcNow: clock.UtcNow);

        db.MaintenanceRequests.Add(request);
        audit.Record(nameof(MaintenanceRequest), request.Id, AuditAction.RequestRaised, new
        {
            request.SiteId,
            request.EstimatedCost,
            request.ThresholdAtSubmission
        });

        // Routing is its own audited transition rather than a silent initial status,
        // so the trail shows *why* a cheap request never saw an approver.
        var routing = request.RouteForApproval(clock.UtcNow);
        audit.RecordTransition(request, routing);

        if (routing.To == RequestStatus.Approved)
        {
            audit.Record(nameof(MaintenanceRequest), request.Id, AuditAction.AutoApproved, new
            {
                Reason = "Estimated cost is below the organisation's cost threshold.",
                request.EstimatedCost,
                request.ThresholdAtSubmission
            });
        }

        // One SaveChanges: the request, the routing, and all three audit rows commit
        // together or not at all.
        await db.SaveChangesAsync(ct);

        return await GetAsync(request.Id, ct);
    }

    public async Task<IReadOnlyList<MaintenanceRequestDto>> ListAsync(
        RequestStatus? status = null,
        Guid? siteId = null,
        CancellationToken ct = default)
    {
        var query = db.MaintenanceRequests.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        if (siteId is not null)
        {
            query = query.Where(r => r.SiteId == siteId);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(Projection)
            .ToListAsync(ct);
    }

    public async Task<MaintenanceRequestDto> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(Projection)
            .FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException(nameof(MaintenanceRequest), id);

    public async Task<MaintenanceRequestDto> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var (request, approver) = await LoadForDecisionAsync(id, ct);

        // Role check and self-approval check both happen inside Approve, against this
        // one approver object. Not two checks in two places that can drift apart.
        var transition = request.Approve(approver, clock.UtcNow);

        audit.RecordTransition(request, transition);
        audit.Record(nameof(MaintenanceRequest), request.Id, AuditAction.Approved, new
        {
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            request.EstimatedCost,
            request.ThresholdAtSubmission
        });

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<MaintenanceRequestDto> RejectAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var (request, approver) = await LoadForDecisionAsync(id, ct);

        var transition = request.Reject(approver, clock.UtcNow);

        audit.RecordTransition(request, transition);
        audit.Record(nameof(MaintenanceRequest), request.Id, AuditAction.Rejected, new
        {
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Reason = reason
        });

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<MaintenanceRequestDto> CompleteAsync(Guid id, decimal actualCost, CancellationToken ct = default)
    {
        var request = await db.MaintenanceRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(MaintenanceRequest), id);

        var (transition, exceeded) = request.Complete(actualCost, clock.UtcNow);

        audit.RecordTransition(request, transition, new { ActualCost = actualCost });
        audit.Record(nameof(MaintenanceRequest), request.Id, AuditAction.Completed, new
        {
            ActualCost = actualCost,
            request.EstimatedCost
        });

        if (exceeded)
        {
            // DECISIONS.md option (a): the approval stands. The overrun is recorded
            // rather than re-routed, so it is visible to compliance without inventing
            // a re-approval sub-flow the brief did not ask for.
            audit.Record(nameof(MaintenanceRequest), request.Id, AuditAction.ActualCostExceededThreshold, new
            {
                ActualCost = actualCost,
                ApprovedUnderThreshold = request.ThresholdAtSubmission,
                Overrun = actualCost - request.ThresholdAtSubmission
            });
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>
    /// The compliance view of one request. Read-only by construction: there is no
    /// corresponding update or delete method here, in the controller, or anywhere else.
    /// </summary>
    public async Task<IReadOnlyList<AuditEntryDto>> GetAuditTrailAsync(Guid requestId, CancellationToken ct = default)
    {
        // Confirms the request is visible to this tenant before returning its history,
        // so the audit endpoint cannot be used as an existence oracle for other tenants.
        var exists = await db.MaintenanceRequests.AnyAsync(r => r.Id == requestId, ct);
        if (!exists)
        {
            throw new NotFoundException(nameof(MaintenanceRequest), requestId);
        }

        return await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == nameof(MaintenanceRequest) && a.EntityId == requestId)
            .OrderBy(a => a.Id)
            .Select(a => new AuditEntryDto(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.PerformedByUser.FullName,
                a.Details,
                a.OccurredAt))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Loads the request and the acting user together. Both come out of tenant-filtered
    /// sets, so a request belonging to another organisation is simply absent.
    /// </summary>
    private async Task<(MaintenanceRequest Request, User Approver)> LoadForDecisionAsync(Guid id, CancellationToken ct)
    {
        var request = await db.MaintenanceRequests.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(MaintenanceRequest), id);

        var approver = await db.Users.FirstOrDefaultAsync(u => u.Id == tenant.UserId && u.IsActive, ct)
            ?? throw new NotFoundException(nameof(User), tenant.UserId);

        return (request, approver);
    }

    /// <summary>
    /// Shared projection so list and detail cannot drift. Expressed as an expression
    /// tree rather than a method call so EF translates it to SQL instead of pulling
    /// entities into memory.
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<MaintenanceRequest, MaintenanceRequestDto>> Projection =>
        r => new MaintenanceRequestDto(
            r.Id,
            r.SiteId,
            r.Site.Name,
            r.Description,
            r.EstimatedCost,
            r.ActualCost,
            r.Status,
            r.ThresholdAtSubmission,
            r.RequestedByUserId,
            r.RequestedByUser.FullName,
            r.ApprovedByUser != null ? r.ApprovedByUser.FullName : null,
            r.ApprovedAt,
            r.CompletedAt,
            r.EstimatedCost >= r.ThresholdAtSubmission,
            r.CreatedAt);
}
