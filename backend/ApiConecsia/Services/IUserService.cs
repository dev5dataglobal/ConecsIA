using ConecsIA.Application.DTOs;

namespace ApiConecsia.Services;

public interface IUserService
{
    Task<AuthenticationResult> AuthenticateAsync(
        LoginDto request,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto> RegisterAsync(
        UsuarioCreateDto request,
        CancellationToken cancellationToken = default);
}

public enum AuthenticationFailureReason
{
    UserNotRegistered,
    InvalidPassword,
    UserLocked
}

public sealed record AuthenticationResult(
    AuthResponseDto? Response = null,
    AuthenticationFailureReason? FailureReason = null);
