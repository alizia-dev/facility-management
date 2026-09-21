using FacilitiesMgmt.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilitiesMgmt.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>
    /// Exchanges organisation slug + email + password for a bearer token.
    /// The only anonymous endpoint in the API.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto, CancellationToken ct) =>
        Ok(await authService.LoginAsync(dto, ct));
}
