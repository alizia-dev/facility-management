using FacilitiesMgmt.Domain.Enums;

namespace FacilitiesMgmt.Domain.Entities;

/// <summary>
/// A person belonging to exactly one organisation.
/// <para>
/// Email is unique <em>per organisation</em>, not globally (UQ_Users_Org_Email).
/// Normalised to lowercase by the application before every insert and lookup, so the
/// uniqueness constraint cannot be defeated by casing.
/// </para>
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public UserRole Role { get; set; }

    /// <summary>Deactivation is a soft delete: audit rows must keep resolving to a user.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
