namespace FacilitiesMgmt.Domain.Entities;

/// <summary>
/// A client company. This is the tenant itself, so it does NOT implement
/// ITenantEntity and carries no discriminator — and it is deliberately the one
/// table left out of the SQL Server security policy, because login has to read it
/// before any tenant context can exist.
/// </summary>
public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = null!;

    /// <summary>
    /// Stable, URL-safe identifier supplied at login. Needed because Users.Email is
    /// unique per organisation, not globally — see DECISIONS.md §Login. Not in the
    /// supplied schema; added deliberately.
    /// </summary>
    public string Slug { get; set; } = null!;

    /// <summary>
    /// Requests estimated below this skip approval; at or above it they require an
    /// Approver. Mutable — which is exactly why each request snapshots it at
    /// submission time into ThresholdAtSubmission.
    /// </summary>
    public decimal CostThreshold { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Site> Sites { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
}
