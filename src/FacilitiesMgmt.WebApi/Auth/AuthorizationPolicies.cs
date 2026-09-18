using FacilitiesMgmt.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace FacilitiesMgmt.WebApi.Auth;

/// <summary>
/// Named policies, so a role name is spelled once in the codebase rather than as a
/// string literal on each attribute where a typo silently widens access.
/// </summary>
public static class AuthorizationPolicies
{
    public const string ApproverOnly = nameof(ApproverOnly);

    public static AuthorizationBuilder AddApplicationPolicies(this AuthorizationBuilder builder) =>
        builder
            // Every endpoint requires an authenticated caller unless it opts out with
            // [AllowAnonymous]. Opt-out beats opt-in: a new controller added without an
            // [Authorize] attribute is protected by default rather than wide open.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(ApproverOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(nameof(UserRole.Approver)));
}
