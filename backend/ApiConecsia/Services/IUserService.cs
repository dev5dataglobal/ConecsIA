using ConecsIA.Domain.Entities;

namespace ApiConecsia.Services;

public interface IUserService
{
    Task<Usuario?> AuthenticateAsync(
        string correo,
        string password,
        CancellationToken cancellationToken = default);

    Task<Usuario> RegisterAsync(
        string correo,
        string password,
        CancellationToken cancellationToken = default);
}
