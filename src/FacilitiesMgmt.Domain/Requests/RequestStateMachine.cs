using FacilitiesMgmt.Domain.Enums;
using FacilitiesMgmt.Domain.Exceptions;

namespace FacilitiesMgmt.Domain.Requests;

/// <summary>
/// The legal edges of the request lifecycle, as data.
/// </summary>
/// <remarks>
/// <para>
/// Why a static transition table and not a workflow library (Stateless, MassTransit
/// Automatonymous, Elsa): this graph has five nodes and six edges and will not grow
/// on its own. A library would add a dependency, a DSL to learn, and a layer of
/// indirection between "what transitions are legal" and the line of code that says so
/// — in exchange for features this system does not use (hierarchical states,
/// triggers, persistence of workflow instances, compensation). The whole rule set is
/// visible in one screen below, and it is exhaustively unit-testable by iterating
/// every (from, to) pair. That is the cheapest thing that makes an illegal transition
/// a test failure rather than a production incident.
/// </para>
/// <para>
/// Why not the GoF State pattern: it would turn five statuses into five classes to
/// express the same table, and the statuses have no per-state behaviour beyond
/// "which edges leave me".
/// </para>
/// </remarks>
public static class RequestStateMachine
{
    private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> LegalTransitions =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            // Raised is transient: the service routes it immediately on creation —
            // to Approved when below the org threshold, to PendingApproval when above.
            // A requester may also withdraw by rejecting before anyone picks it up.
            [RequestStatus.Raised] = [RequestStatus.PendingApproval, RequestStatus.Approved, RequestStatus.Rejected],

            [RequestStatus.PendingApproval] = [RequestStatus.Approved, RequestStatus.Rejected],

            // Only approved work can be completed. This is the edge that makes
            // "money was spent without anyone approving it" unrepresentable.
            [RequestStatus.Approved] = [RequestStatus.Completed],

            // Terminal. No resurrection, no re-approval of completed work —
            // see DECISIONS.md on threshold-exceeded-after-approval.
            [RequestStatus.Rejected] = [],
            [RequestStatus.Completed] = []
        };

    public static IReadOnlyCollection<RequestStatus> LegalNextStates(RequestStatus from) =>
        LegalTransitions.TryGetValue(from, out var next) ? next : [];

    public static bool CanTransition(RequestStatus from, RequestStatus to) =>
        LegalTransitions.TryGetValue(from, out var next) && next.Contains(to);

    /// <summary>
    /// Guard clause used by every mutating method on <c>MaintenanceRequest</c>.
    /// Nothing else in the codebase is allowed to assign Status directly.
    /// </summary>
    public static void EnsureCanTransition(RequestStatus from, RequestStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new IllegalStateTransitionException(from, to);
        }
    }
}
