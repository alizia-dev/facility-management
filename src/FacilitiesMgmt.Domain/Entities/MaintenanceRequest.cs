using FacilitiesMgmt.Domain.Enums;
using FacilitiesMgmt.Domain.Exceptions;
using FacilitiesMgmt.Domain.Requests;

namespace FacilitiesMgmt.Domain.Entities;

/// <summary>The result of a lifecycle move: what the status was, and what it became.</summary>
public readonly record struct StatusTransition(RequestStatus From, RequestStatus To);

/// <summary>
/// A maintenance request raised against a site.
/// <para>
/// Status has a private setter and every move goes through a method that consults
/// <see cref="RequestStateMachine"/> first. That is the whole point: there is no way
/// to reach an illegal state from a controller, a service, or a future contributor's
/// shortcut, because there is no public setter to reach it with.
/// </para>
/// </summary>
public class MaintenanceRequest
{
    // EF materialisation constructor.
    private MaintenanceRequest() { }

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }

    public Guid SiteId { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public string Description { get; private set; } = null!;

    public decimal EstimatedCost { get; private set; }

    /// <summary>Null until the work is completed. This is the number the spend report sums.</summary>
    public decimal? ActualCost { get; private set; }

    public RequestStatus Status { get; private set; } = RequestStatus.Raised;

    /// <summary>
    /// The organisation's CostThreshold copied in at submission time. Without this
    /// snapshot, changing the org threshold would silently rewrite the history of why
    /// past requests were routed the way they were — approvals would stop being
    /// explainable, which is a compliance problem, not a cosmetic one.
    /// </summary>
    public decimal ThresholdAtSubmission { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    /// <summary>
    /// When the work finished and money was actually spent. The spend report's date
    /// axis — "what did we spend at Site 12 last month" is a question about this
    /// field, not about CreatedAt. Not in the supplied schema; added deliberately.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public Site Site { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ApprovedByUser { get; set; }

    /// <summary>
    /// Creates a request in <see cref="RequestStatus.Raised"/>. Routing is a separate,
    /// audited step — see <see cref="RouteForApproval"/>.
    /// </summary>
    public static MaintenanceRequest Raise(
        Guid organizationId,
        Guid siteId,
        Guid requestedByUserId,
        string description,
        decimal estimatedCost,
        decimal organizationThreshold,
        DateTime utcNow)
    {
        if (estimatedCost < 0)
        {
            throw new BusinessRuleViolationException("Estimated cost cannot be negative.");
        }

        return new MaintenanceRequest
        {
            OrganizationId = organizationId,
            SiteId = siteId,
            RequestedByUserId = requestedByUserId,
            Description = description,
            EstimatedCost = estimatedCost,
            ThresholdAtSubmission = organizationThreshold,
            Status = RequestStatus.Raised,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>
    /// Routes a freshly raised request: below the snapshotted threshold it is approved
    /// with no human in the loop; at or above it, it waits for an Approver.
    /// </summary>
    /// <remarks>
    /// Boundary: <c>EstimatedCost &lt; threshold</c> auto-approves, so a request
    /// <em>exactly at</em> the threshold requires approval. The brief says "below it
    /// skip approval, above it require an Approver" and is silent on equality; when
    /// money is involved the safe reading of an ambiguous boundary is the one that
    /// asks a human. Covered by three unit tests at threshold ±0.01.
    /// </remarks>
    public StatusTransition RouteForApproval(DateTime utcNow)
    {
        var destination = EstimatedCost < ThresholdAtSubmission
            ? RequestStatus.Approved
            : RequestStatus.PendingApproval;

        return Transition(destination, utcNow);
    }

    /// <summary>True when this request was approved without a human decision.</summary>
    public bool WasAutoApproved => Status == RequestStatus.Approved && ApprovedByUserId is null;

    /// <summary>
    /// Approves the request.
    /// <para>
    /// The role check and the "an approver cannot approve their own request" check
    /// live in this one method, against the one <paramref name="approver"/> object,
    /// deliberately. Split across two call sites they can drift — a later refactor
    /// moves one and leaves the other, and the gap is invisible until someone
    /// approves their own £40k request. One method, one object, both rules.
    /// </para>
    /// </summary>
    public StatusTransition Approve(User approver, DateTime utcNow)
    {
        EnsureApproverIsEligible(approver);

        var transition = Transition(RequestStatus.Approved, utcNow);
        ApprovedByUserId = approver.Id;
        ApprovedAt = utcNow;
        return transition;
    }

    /// <summary>Rejects the request. Same eligibility rules as <see cref="Approve"/>.</summary>
    public StatusTransition Reject(User approver, DateTime utcNow)
    {
        EnsureApproverIsEligible(approver);

        var transition = Transition(RequestStatus.Rejected, utcNow);
        ApprovedByUserId = approver.Id;
        ApprovedAt = utcNow;
        return transition;
    }

    /// <summary>
    /// Records completion and the money actually spent. Only an Approved request can
    /// be completed, which is what makes "spent without approval" unrepresentable.
    /// </summary>
    /// <returns>
    /// The transition, and whether the actual cost overran the threshold this request
    /// was approved under. The approval stands either way (DECISIONS.md option (a));
    /// the caller writes an extra audit entry when the flag is set.
    /// </returns>
    public (StatusTransition Transition, bool ExceededApprovedThreshold) Complete(decimal actualCost, DateTime utcNow)
    {
        if (actualCost < 0)
        {
            throw new BusinessRuleViolationException("Actual cost cannot be negative.");
        }

        var transition = Transition(RequestStatus.Completed, utcNow);
        ActualCost = actualCost;
        CompletedAt = utcNow;

        return (transition, actualCost > ThresholdAtSubmission);
    }

    private void EnsureApproverIsEligible(User approver)
    {
        if (approver.Role != UserRole.Approver)
        {
            throw new BusinessRuleViolationException(
                "Only a user with the Approver role can decide a maintenance request.",
                isAuthorizationFailure: true);
        }

        if (approver.Id == RequestedByUserId)
        {
            throw new BusinessRuleViolationException(
                "An approver cannot decide their own request.",
                isAuthorizationFailure: true);
        }

        // Defence in depth. The approver comes from the JWT and the request comes from a
        // filtered query, so both are already this tenant's by construction — but the
        // cost of asserting it is one comparison, and the cost of being wrong is a
        // cross-tenant approval.
        if (approver.OrganizationId != OrganizationId)
        {
            throw new BusinessRuleViolationException(
                "Cross-organisation approval is not permitted.",
                isAuthorizationFailure: true);
        }
    }

    private StatusTransition Transition(RequestStatus destination, DateTime utcNow)
    {
        RequestStateMachine.EnsureCanTransition(Status, destination);

        var transition = new StatusTransition(Status, destination);
        Status = destination;
        UpdatedAt = utcNow;
        return transition;
    }
}
