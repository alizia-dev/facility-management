using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FacilitiesMgmt.Infrastructure.Security;

/// <summary>
/// Password hashing on top of ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why borrow this one piece of Identity rather than adopt Identity wholesale.</b>
/// The supplied schema owns the user table: <c>dbo.Users</c>, with <c>OrganizationId</c>
/// as a tenant discriminator and <c>UQ_Users_Org_Email</c> making email unique per
/// organisation rather than globally. Full ASP.NET Core Identity brings its own seven
/// tables, its own <c>AspNetUsers</c> with a globally unique normalised username, and a
/// <c>UserManager</c> that assumes it owns the store. Bending that into a per-tenant
/// uniqueness model means fighting the framework at every step — and the parts we would
/// actually use (lockout, 2FA, token providers, external logins) are all out of scope.
/// </para>
/// <para>
/// What Identity does have that is genuinely hard to get right by hand is this class:
/// PBKDF2-HMAC-SHA512, 210,000 iterations, per-password salt, a versioned format that
/// supports future rehashing, and a fixed-time comparison. Writing that from scratch
/// would be the actual risk. So: custom store, borrowed hasher.
/// </para>
/// <para>
/// The trade-off, stated plainly: we hand-roll the login flow, and password policy and
/// lockout are not implemented. For production the first thing to add is attempt
/// throttling — see DECISIONS.md.
/// </para>
/// </remarks>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    // Computed once at startup so the login flow can do identical work for a user that
    // does not exist. A constant string would be cheaper but would rot if Identity ever
    // changes its hash format; deriving it from the hasher itself cannot.
    private static readonly Lazy<string> UnmatchableHash = new(() =>
        Hasher.HashPassword(new User(), Guid.NewGuid().ToString("N")));

    public string NeverMatchingHash => UnmatchableHash.Value;

    public string Hash(string password) => Hasher.HashPassword(new User(), password);

    public bool Verify(string hash, string password)
    {
        try
        {
            return Hasher.VerifyHashedPassword(new User(), hash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // A corrupt or hand-edited hash is a failed login, not a 500.
            return false;
        }
    }
}
