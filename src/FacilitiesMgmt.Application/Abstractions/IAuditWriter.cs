using FacilitiesMgmt.Domain.Entities;
using FacilitiesMgmt.Domain.Enums;

namespace FacilitiesMgmt.Application.Abstractions;

/// <summary>
/// Appends to the audit trail.
/// <para>
/// Implementations <b>stage</b> the entry on the same <see cref="IAppDbContext"/> the
/// caller is already using and do not save. The service's single
/// <c>SaveChangesAsync</c> then commits the state change and its audit row in one
/// transaction: either both land or neither does. An audit writer that saved
/// independently — or that was called from a separate endpoint afterwards — would
/// allow a state change with no record of it, which is precisely the failure mode the
/// compliance requirement exists to prevent.
/// </para>
/// </summary>
public interface IAuditWriter
{
    void Record(
        string entityType,
        Guid entityId,
        AuditAction action,
        object? details = null);

    /// <summary>Convenience overload for the most common event: a lifecycle move.</summary>
    void RecordTransition(MaintenanceRequest request, StatusTransition transition, object? extra = null);
}
