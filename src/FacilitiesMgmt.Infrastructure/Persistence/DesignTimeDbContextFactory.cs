using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FacilitiesMgmt.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time.
/// </summary>
/// <remarks>
/// Without this, the tooling has to boot the whole web host to find a DbContext, which
/// drags configuration, secrets, and startup-time migration into a command whose only
/// job is to generate a C# file. This keeps scaffolding independent of how the app is
/// hosted. The connection string here only has to point at <em>a</em> SQL Server so EF
/// can read the provider's type mappings; it is never used to run the application.
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("FACILITIESMGMT_CONNECTION")
            ?? "Server=(localdb);";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options, new DesignTimeTenant());
    }

    /// <summary>
    /// An unresolved tenant. Query filters are baked into the model, not evaluated
    /// during scaffolding, so this never filters anything at design time — and
    /// <see cref="Guid.Empty"/> is the correct fail-closed value regardless.
    /// </summary>
    private sealed class DesignTimeTenant : ICurrentTenant
    {
        public bool IsResolved => false;
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public UserRole Role => UserRole.Requester;

        public void ResolveForLogin(Guid organizationId) =>
            throw new NotSupportedException("Tenants are not resolved at design time.");
    }
}
