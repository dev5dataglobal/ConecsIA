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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto request,
        CancellationToken cancellationToken)
    {
        var response = await userService.AuthenticateAsync(request, cancellationToken);
        if (response.Response is not null)
        {
            return Ok(response.Response);
        }

        var detail = response.FailureReason switch
        {
            AuthenticationFailureReason.UserNotRegistered => "El usuario ingresado no está registrado",
            AuthenticationFailureReason.InvalidPassword => "La contraseña ingresada es incorrecta",
            AuthenticationFailureReason.UserLocked => "El usuario está temporalmente bloqueado. Inténtalo más tarde",
            AuthenticationFailureReason.UserInactive => "El usuario está inactivo. Comunícate con el administrador de tu organización",
            _ => "No se pudo iniciar sesión"
        };

        return Unauthorized(new ProblemDetails
        {
            Type = "https://httpstatuses.com/401",
            Title = "No se pudo iniciar sesión",
            Status = StatusCodes.Status401Unauthorized,
            Detail = detail
        });
    }
}
