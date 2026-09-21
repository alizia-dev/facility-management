using FacilitiesMgmt.Domain.Enums;

namespace FacilitiesMgmt.Domain.Entities;

/// <summary>
/// An append-only record of something that happened.
/// <para>
/// Every property has a private setter and the only way to make one is
/// <see cref="Record"/>. There is no mutator, no <c>Update</c>, and no repository
/// method that returns a tracked instance for editing — so "modify an audit row" is
/// not something a caller can express, let alone expose through an endpoint.
/// EF Core's change tracker is additionally told this entity is insert-only.
/// </para>
/// </summary>
public class AuditLogEntry
{
    private AuditLogEntry() { }

    /// <summary>BIGINT IDENTITY — a gap in this sequence is itself evidence.</summary>
    public long Id { get; private set; }

    public Guid OrganizationId { get; set; }

    public string EntityType { get; private set; } = null!;

    public Guid EntityId { get; private set; }

    public AuditAction Action { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    /// <summary>JSON; the database enforces ISJSON. Null for events with no extra context.</summary>
    public string? Details { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public User PerformedByUser { get; set; } = null!;

    public static AuditLogEntry Record(
        Guid organizationId,
        string entityType,
        Guid entityId,
        AuditAction action,
        Guid performedByUserId,
        string? detailsJson,
        DateTime utcNow) => new()
        {
            OrganizationId = organizationId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PerformedByUserId = performedByUserId,
            Details = detailsJson,
            OccurredAt = utcNow
        };
}
