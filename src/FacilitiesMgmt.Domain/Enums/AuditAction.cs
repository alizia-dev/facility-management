namespace FacilitiesMgmt.Domain.Enums;

/// <summary>
/// The vocabulary of the audit trail. Kept as an enum rather than free-text strings
/// so a compliance query can never miss an event because someone wrote "approve"
/// instead of "Approved".
/// </summary>
public enum AuditAction
{
    /// <summary>A request was created.</summary>
    RequestRaised,

    /// <summary>Any status change; Details carries the from/to pair.</summary>
    StatusChanged,

    /// <summary>An approver accepted a request above threshold.</summary>
    Approved,

    /// <summary>An approver rejected a request; Details carries the reason.</summary>
    Rejected,

    /// <summary>Below-threshold request routed straight to Approved with no human in the loop.</summary>
    AutoApproved,

    /// <summary>Work finished and an actual cost was recorded.</summary>
    Completed,

    /// <summary>
    /// Actual cost came in above the threshold the request was approved under.
    /// The approval still stands (DECISIONS.md, option (a)) — this entry is how
    /// the overrun stays visible and reportable.
    /// </summary>
    ActualCostExceededThreshold
}
