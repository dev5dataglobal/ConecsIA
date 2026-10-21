using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ApiConecsia.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<Usuario?> AuthenticateAsync(
        string correo,
        string password,
        CancellationToken cancellationToken = default)
    {
        var correoNormalizado = NormalizeEmail(correo);
        var usuario = _unitOfWork.UsuarioRepository
            .Find(x => x.CorreoNormalizado == correoNormalizado)
            .SingleOrDefault();

        if (usuario is null || !usuario.Activo)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        usuario.UltimoAcceso = DateTime.UtcNow;
        _unitOfWork.UsuarioRepository.Update(usuario);
        await _unitOfWork.SaveAsync();

        return usuario;
    }

    public async Task<Usuario> RegisterAsync(
        string correo,
        string password,
        CancellationToken cancellationToken = default)
    {
        var correoNormalizado = NormalizeEmail(correo);
        var exists = _unitOfWork.UsuarioRepository
            .Find(x => x.CorreoNormalizado == correoNormalizado)
            .Any();

        if (exists)
        {
            throw new InvalidOperationException(
                "Ya existe un usuario con el correo indicado.");
        }

        var usuario = new Usuario
        {
            Correo = correo.Trim(),
            CorreoNormalizado = correoNormalizado,
            CreadoEn = DateTime.UtcNow,
            Activo = true
        };

        usuario.PasswordHash = _passwordHasher.HashPassword(
            usuario,
            password);

        _unitOfWork.UsuarioRepository.Add(usuario);
        await _unitOfWork.SaveAsync();

        return usuario;
    }

    private static string NormalizeEmail(string correo)
    {
        return correo.Trim().ToUpperInvariant();
    }
}
