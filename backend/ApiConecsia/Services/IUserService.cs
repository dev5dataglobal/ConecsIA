using ConecsIA.Application.DTOs;

namespace ApiConecsia.Services;

public interface IUserService
{
    Task<AuthResponseDto?> AuthenticateAsync(
        LoginDto request,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto> RegisterAsync(
        UsuarioCreateDto request,
        CancellationToken cancellationToken = default);
}
