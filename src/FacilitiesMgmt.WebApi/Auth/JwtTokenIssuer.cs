using System.Security.Claims;
using System.Text;
using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FacilitiesMgmt.WebApi.Auth;

/// <summary>
/// Mints the bearer token. The token carries the organisation id, and that claim is the
/// only statement of tenancy the server will accept — which is why it has to be signed
/// and why the signing key is validated at startup.
/// </summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, IClock clock) : ITokenIssuer
{
    private readonly JwtOptions _options = options.Value;
    private static readonly JsonWebTokenHandler Handler = new();

    public (string Token, DateTime ExpiresAtUtc) Issue(User user)
    {
        var expiresAt = clock.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [ClaimTypes.NameIdentifier] = user.Id.ToString(),
                [ClaimTypes.Role] = user.Role.ToString()
            }
        };

        return (Handler.CreateToken(descriptor), expiresAt);
    }
}
