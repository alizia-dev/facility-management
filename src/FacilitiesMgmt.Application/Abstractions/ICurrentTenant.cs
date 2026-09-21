using FacilitiesMgmt.Domain.Enums;

namespace FacilitiesMgmt.Application.Abstractions;

/// <summary>
/// Who is calling, and which tenant they belong to, for the duration of one request.
/// <para>
/// <b>Registered scoped — never <c>AsyncLocal&lt;T&gt;</c>.</b> Kestrel does not
/// guarantee one thread per request end-to-end; a request can resume on a different
/// thread after any await, and an <c>AsyncLocal</c> written in middleware can leak
/// across async continuations in ways that single-request manual testing will never
/// reveal. A scoped service is bound to the DI request scope itself, which is the
/// thing that actually corresponds to "this request". There is a concurrency test
/// asserting two tenants hammering the API simultaneously never see each other's data.
/// </para>
/// <para>
/// Populated <b>only</b> from validated JWT claims. Never from a header, query string,
/// route value, or body. If you are reading an organisation id off the request, that
/// is the bug.
/// </para>
/// </summary>
public interface ICurrentTenant
{
    /// <summary>False before authentication and during the login exchange.</summary>
    bool IsResolved { get; }

    /// <summary>
    /// The tenant. <see cref="Guid.Empty"/> when unresolved — which makes the global
    /// query filter match nothing rather than everything. Fails closed by construction.
    /// </summary>
    Guid OrganizationId { get; }

    Guid UserId { get; }

    UserRole Role { get; }

    /// <summary>
    /// The one sanctioned way to establish a tenant without a JWT: the login exchange,
    /// after the organisation has been resolved from its slug against the unfiltered
    /// <c>Organizations</c> table. Called exactly once, from <c>AuthService</c>.
    /// </summary>
    /// <remarks>
    /// The supplied schema's comments assume the user lookup can simply "run outside
    /// the RLS filter". It cannot — there is a FILTER PREDICATE on dbo.Users. Resolving
    /// the organisation first means login needs no privileged bypass at all, so there
    /// is no exempt code path to audit. See DECISIONS.md §5.
    /// </remarks>
    void ResolveForLogin(Guid organizationId);
}
