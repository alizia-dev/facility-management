using System.Reflection;
using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FacilitiesMgmt.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant tenant)
    : DbContext(options), IAppDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    /// <summary>
    /// Read by the global query filters below. Exposed as a property rather than a
    /// captured local because EF Core rebinds a DbContext reference inside a query
    /// filter to the <em>currently executing</em> context, which is what makes one
    /// cached model safe to share across requests with different tenants. There is an
    /// integration test that asserts exactly this, because it is the single assumption
    /// in this codebase whose failure would be silent and catastrophic.
    /// </summary>
    public Guid CurrentOrganizationId => tenant.OrganizationId;

    /// <summary>
    /// Last line of defence for audit integrity inside the process.
    /// </summary>
    /// <remarks>
    /// There is no update or delete path for <see cref="AuditLogEntry"/> in any service
    /// or controller, and the entity has no public setters — so reaching here at all
    /// means someone found a way that was not meant to exist. Throwing turns that from a
    /// silent tampering into a loud failure. The real guarantee is at the database
    /// (DENY UPDATE, DELETE on dbo.AuditLogs for the application login); this catches
    /// the mistake earlier and names it.
    /// </remarks>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var tampering = ChangeTracker.Entries<AuditLogEntry>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (tampering)
        {
            throw new InvalidOperationException(
                "The audit trail is append-only. Audit entries cannot be modified or deleted.");
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);


        base.OnModelCreating(modelBuilder);
    }
}
