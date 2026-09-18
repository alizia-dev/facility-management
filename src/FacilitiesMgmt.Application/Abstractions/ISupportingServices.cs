using FacilitiesMgmt.Domain.Entities;

namespace FacilitiesMgmt.Application.Abstractions;

/// <summary>
/// Time, as a dependency. Every timestamp in the system comes from here so that
/// threshold-and-date-range tests are deterministic rather than "usually passes".
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>
/// Password hashing, kept behind an interface so the algorithm is an Infrastructure
/// decision rather than something the Application layer hard-codes.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Constant-time verification; returns false rather than throwing on a malformed hash.</summary>
    bool Verify(string hash, string password);

    /// <summary>
    /// A structurally valid hash that no password matches. Used by the login flow to do
    /// the same work whether or not the user exists, so response time is not a user
    /// enumeration oracle.
    /// </summary>
    string NeverMatchingHash { get; }
}

/// <summary>Issues the bearer token that carries the tenant assertion.</summary>
public interface ITokenIssuer
{
    (string Token, DateTime ExpiresAtUtc) Issue(User user);
}
