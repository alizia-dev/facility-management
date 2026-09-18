using FacilitiesMgmt.Application.Abstractions;

namespace FacilitiesMgmt.Infrastructure;

/// <summary>
/// The real clock. Everything is UTC — a facilities company operating across sites in
/// different time zones cannot store local times and hope, and a spend report whose
/// month boundaries depend on the server's locale is a report nobody can reconcile.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
