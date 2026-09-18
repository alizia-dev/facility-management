using System.ComponentModel.DataAnnotations;

namespace FacilitiesMgmt.WebApi.Auth;

/// <summary>
/// JWT settings. <see cref="SigningKey"/> is a secret and is deliberately absent from
/// appsettings.json — it arrives from <c>dotnet user-secrets</c> locally or from the
/// environment elsewhere. Startup fails loudly if it is missing or too short rather than
/// falling back to a default, because a hard-coded fallback key is the kind of
/// convenience that ships to production and signs tokens anyone can forge.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "facilities-mgmt";

    [Required]
    public string Audience { get; set; } = "facilities-mgmt-spa";

    /// <summary>At least 32 bytes — HMAC-SHA256 gains nothing from a shorter key.</summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
