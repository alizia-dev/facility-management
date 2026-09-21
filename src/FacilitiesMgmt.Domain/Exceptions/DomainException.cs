namespace FacilitiesMgmt.Domain.Exceptions;

/// <summary>
/// Base for rule violations the caller could reasonably have avoided.
/// Mapped centrally to ProblemDetails in the API — one place owns the HTTP contract,
/// so the domain never references status codes.
/// </summary>
public abstract class DomainException(string message) : Exception(message);

/// <summary>
/// An attempt to move a request along an edge the state machine does not have.
/// Maps to 409 Conflict: the request is well-formed, the resource is in the wrong state.
/// </summary>
public sealed class IllegalStateTransitionException(Enums.RequestStatus from, Enums.RequestStatus to)
    : DomainException($"A request cannot move from {from} to {to}.")
{
    public Enums.RequestStatus From { get; } = from;
    public Enums.RequestStatus To { get; } = to;
}

/// <summary>
/// A business rule said no — wrong role, self-approval, negative cost.
/// Maps to 403 Forbidden or 409 Conflict depending on <see cref="IsAuthorizationFailure"/>.
/// </summary>
public sealed class BusinessRuleViolationException(string message, bool isAuthorizationFailure = false)
    : DomainException(message)
{
    /// <summary>
    /// True when the caller is not permitted to perform the action at all (role,
    /// self-approval). False when the action is permitted but the state forbids it.
    /// </summary>
    public bool IsAuthorizationFailure { get; } = isAuthorizationFailure;
}
