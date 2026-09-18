using System.Text.Json;
using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Domain.Entities;
using FacilitiesMgmt.Domain.Enums;

namespace FacilitiesMgmt.Application.Audit;

/// <inheritdoc cref="IAuditWriter"/>
public sealed class AuditWriter(IAppDbContext db, ICurrentTenant tenant, IClock clock) : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Record(string entityType, Guid entityId, AuditAction action, object? details = null)
    {
        var entry = AuditLogEntry.Record(
            organizationId: tenant.OrganizationId,
            entityType: entityType,
            entityId: entityId,
            action: action,
            performedByUserId: tenant.UserId,
            detailsJson: details is null ? null : JsonSerializer.Serialize(details, JsonOptions),
            utcNow: clock.UtcNow);

        // Staged only. The caller's SaveChangesAsync commits this alongside the state
        // change it describes — one transaction, so an audit row can never be missing
        // for a change that happened, nor present for one that rolled back.
        db.AuditLogs.Add(entry);
    }

    public void RecordTransition(MaintenanceRequest request, StatusTransition transition, object? extra = null)
    {
        Record(
            nameof(MaintenanceRequest),
            request.Id,
            AuditAction.StatusChanged,
            new
            {
                From = transition.From.ToString(),
                To = transition.To.ToString(),
                request.SiteId,
                request.EstimatedCost,
                request.ThresholdAtSubmission,
                Extra = extra
            });
    }
}
