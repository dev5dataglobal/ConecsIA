using ConecsIA.Application.DTOs;
using ApiConecsia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IUserService userService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto request,
        CancellationToken cancellationToken)
    {
        var response = await userService.AuthenticateAsync(request, cancellationToken);
        return response is null ? Unauthorized() : Ok(response);
    }
}
