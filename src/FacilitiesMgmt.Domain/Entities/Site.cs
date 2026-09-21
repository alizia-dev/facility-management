
namespace FacilitiesMgmt.Domain.Entities;

/// <summary>A location belonging to exactly one organisation.</summary>
public class Site
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = [];
}
