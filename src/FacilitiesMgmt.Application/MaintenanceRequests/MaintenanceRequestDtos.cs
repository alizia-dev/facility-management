using FacilitiesMgmt.Domain.Enums;

namespace FacilitiesMgmt.Application.MaintenanceRequests;

/// <summary>
/// Note what is absent: there is no OrganizationId. The caller does not get to say
/// which tenant they are writing into — that comes from the JWT. An inbound DTO that
/// accepted a tenant id would be a mass-assignment hole with a nice name.
/// </summary>
public sealed record CreateMaintenanceRequestDto(Guid SiteId, string Description, decimal EstimatedCost);

public sealed record RejectMaintenanceRequestDto(string? Reason);

public sealed record CompleteMaintenanceRequestDto(decimal ActualCost);

public sealed record MaintenanceRequestDto(
    Guid Id,
    Guid SiteId,
    string SiteName,
    string Description,
    decimal EstimatedCost,
    decimal? ActualCost,
    RequestStatus Status,
    decimal ThresholdAtSubmission,
    Guid RequestedByUserId,
    string RequestedByName,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime? CompletedAt,
    bool RequiresApproval,
    DateTime CreatedAt);

public sealed record AuditEntryDto(
    long Id,
    string EntityType,
    Guid EntityId,
    AuditAction Action,
    string PerformedByName,
    string? Details,
    DateTime OccurredAt);
