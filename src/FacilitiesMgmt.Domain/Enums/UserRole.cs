namespace FacilitiesMgmt.Domain.Enums;

/// <summary>
/// Persisted as NVARCHAR(20); matches CHECK (Role IN ('Requester','Approver')).
/// A single role per user, per the brief — see DECISIONS.md if a user ever needs both.
/// </summary>
public enum UserRole
{
    Requester,
    Approver
}
