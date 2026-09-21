namespace FacilitiesMgmt.Domain.Enums;

/// <summary>
/// Persisted as NVARCHAR(20) to match the schema's CHECK constraint — the names
/// here are the constraint's alphabet, so renaming one is a migration, not a rename.
/// </summary>
public enum RequestStatus
{
    Raised,
    PendingApproval,
    Approved,
    Rejected,
    Completed
}
