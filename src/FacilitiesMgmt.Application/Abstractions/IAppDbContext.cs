using FacilitiesMgmt.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacilitiesMgmt.Application.Abstractions;

/// <summary>
/// The Application layer's view of persistence.
/// <para>
/// This is deliberately <em>not</em> a repository-per-entity abstraction.
/// <c>DbContext</c> is already a Unit of Work and <c>DbSet&lt;T&gt;</c> is already a
/// repository; wrapping them in hand-written repositories would add a layer whose
/// only job is to forward calls, and would hide the LINQ the global query filter
/// attaches to. The interface exists for one reason: to keep the dependency arrow
/// pointing inward, so Application does not reference Infrastructure and can be
/// tested against a substitute.
/// </para>
/// <para>
/// Note that <c>Organizations</c> is the only set here that is NOT tenant-filtered —
/// it is the tenant. Every query against it must scope by
/// <see cref="ICurrentTenant.OrganizationId"/> explicitly, except the login lookup.
/// </para>
/// </summary>
public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Site> Sites { get; }
    DbSet<User> Users { get; }
    DbSet<MaintenanceRequest> MaintenanceRequests { get; }
    DbSet<AuditLogEntry> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
