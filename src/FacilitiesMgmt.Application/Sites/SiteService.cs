using FacilitiesMgmt.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FacilitiesMgmt.Application.Sites;

public sealed record SiteDto(Guid Id, string Name, string? Address);

/// <summary>
/// Read-only. The brief does not ask for site management, so none is built — see
/// "Things I chose not to build" in DECISIONS.md. This exists because the create-request
/// form needs somewhere to get a site list, and that list must be tenant-scoped.
/// </summary>
public sealed class SiteService(IAppDbContext db)
{
    public async Task<IReadOnlyList<SiteDto>> ListAsync(CancellationToken ct = default) =>
        await db.Sites
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SiteDto(s.Id, s.Name, s.Address))
            .ToListAsync(ct);
}
