namespace FacilitiesMgmt.Application.Common;

/// <summary>
/// The resource does not exist <em>as far as this caller is concerned</em>.
/// <para>
/// This is the exception a cross-tenant probe produces, and it is why the API answers
/// <b>404 and not 403</b>. A 403 says "this exists, you just can't have it" — which
/// hands another tenant a working existence oracle: guess IDs, watch the status codes,
/// and you can enumerate a competitor's site and request IDs without ever reading a
/// row. Indistinguishable 404s leak nothing.
/// </para>
/// <para>
/// Note that nothing special has to be written to produce this: the global query
/// filter makes another tenant's row simply not be there, so the ordinary
/// "not found" path is also the security path.
/// </para>
/// </summary>
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");

/// <summary>Authentication failed. Never says which half was wrong.</summary>
public sealed class AuthenticationFailedException()
    : Exception("Invalid organisation, email address, or password.");
