using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Application.Common;
using FacilitiesMgmt.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FacilitiesMgmt.Application.Auth;

/// <summary>
/// Login carries the organisation slug because <c>Users.Email</c> is unique
/// <em>per organisation</em>, not globally — two tenants may legitimately both employ
/// <c>ops@contoso.com</c>. See DECISIONS.md §5.
/// </summary>
public sealed record LoginDto(string OrganizationSlug, string Email, string Password);

public sealed record LoginResultDto(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string FullName,
    UserRole Role,
    Guid OrganizationId,
    string OrganizationName,
    decimal CostThreshold);

public sealed class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.OrganizationSlug).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class AuthService(
    IAppDbContext db,
    ICurrentTenant tenant,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer)
{
    /// <summary>
    /// The one flow in the system that runs before a tenant exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Order matters, and this is the part the supplied schema got wrong. Its comments
    /// say the user lookup "runs outside the RLS filter by design" — but there is a
    /// FILTER PREDICATE on <c>dbo.Users</c>, so a lookup with no session context returns
    /// zero rows and login can never succeed. The fix is not to grant a bypass. It is to
    /// resolve the organisation first, from <c>dbo.Organizations</c>, which is
    /// deliberately the one tenant table left out of the security policy precisely
    /// because it is the tenant. Once the org is known we establish the tenant context
    /// and query Users normally, under both the EF filter and RLS.
    /// </para>
    /// <para>
    /// The result: there is no privileged, filter-exempt code path anywhere in this
    /// application. Nothing to audit, nothing to accidentally reuse.
    /// </para>
    /// </remarks>
    public async Task<LoginResultDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        // Step 1 — resolve the tenant. Organizations is not tenant-filtered (it is the
        // tenant) and carries no RLS predicate, so this query works with no context set.
        var slug = dto.OrganizationSlug.Trim().ToLowerInvariant();
        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Slug == slug, ct);

        if (organization is null)
        {
            // Deliberately the same exception as a wrong password: revealing that an
            // organisation slug exists lets an outsider enumerate the client list.
            throw new AuthenticationFailedException();
        }

        // Step 2 — establish the tenant for the rest of this request. From here on the
        // EF global query filter and the SQL Server session context both apply.
        tenant.ResolveForLogin(organization.Id);

        // Step 3 — now a normal, fully filtered query. Email is lowercased on write and
        // on read so UQ_Users_Org_Email cannot be defeated by casing.
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        // Verify unconditionally. Skipping the hash when the user does not exist makes
        // "no such user" return in microseconds and "wrong password" in milliseconds —
        // a timing oracle that lets an outsider enumerate a tenant's staff list without
        // ever logging in. Hashing a throwaway value keeps both paths the same shape.
        var hashToVerify = user?.PasswordHash ?? passwordHasher.NeverMatchingHash;
        var passwordMatches = passwordHasher.Verify(hashToVerify, dto.Password);

        if (user is null || !passwordMatches)
        {
            throw new AuthenticationFailedException();
        }

        var (token, expiresAt) = tokenIssuer.Issue(user);

        return new LoginResultDto(
            token,
            expiresAt,
            user.Id,
            user.FullName,
            user.Role,
            organization.Id,
            organization.Name,
            organization.CostThreshold);
    }
}
