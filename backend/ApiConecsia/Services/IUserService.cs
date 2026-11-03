using ConecsIA.Application.DTOs;

namespace ApiConecsia.Services;

public interface IUserService
{
    Task<AuthenticationResult> AuthenticateAsync(
        LoginDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rota la sesión renovable y emite un token de acceso nuevo con los permisos vigentes en ese momento.
    /// </summary>
    Task<AuthenticationResult> RenovarAsync(
        string? tokenRefresco,
        CancellationToken cancellationToken = default);

    Task CerrarSesionAsync(
        string? tokenRefresco,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto> RegisterAsync(
        UsuarioCreateDto request,
        CancellationToken cancellationToken = default);
}

public enum AuthenticationFailureReason
{
    UserNotRegistered,
    InvalidPassword,
    UserLocked,
    /// <summary>Contraseña correcta, pero sin ninguna membresía activa en una organización activa.</summary>
    UserInactive,
    /// <summary>Sesión renovable inexistente, vencida, revocada o invalidada por un cambio de seguridad.</summary>
    SessionExpired
}

/// <summary>Token renovable en claro (solo viaja en la cookie HttpOnly). null = conservar la cookie actual.</summary>
public sealed record SesionEmitida(string Token, DateTime ExpiraEn, bool Recordar);

public sealed record AuthenticationResult(
    AuthResponseDto? Response = null,
    AuthenticationFailureReason? FailureReason = null,
    SesionEmitida? Sesion = null);
