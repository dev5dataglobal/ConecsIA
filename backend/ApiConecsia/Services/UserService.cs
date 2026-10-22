using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace ApiConecsia.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IMapper _mapper;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<Usuario> passwordHasher,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<UsuarioDto?> AuthenticateAsync(
        LoginDto request,
        CancellationToken cancellationToken = default)
    {
        var correoNormalizado = NormalizeEmail(request.Correo);
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
            request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        usuario.UltimoAcceso = DateTime.UtcNow;
        _unitOfWork.UsuarioRepository.Update(usuario);
        await _unitOfWork.SaveAsync();

        return _mapper.Map<UsuarioDto>(usuario);
    }

    public async Task<UsuarioDto> RegisterAsync(
        UsuarioCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var correoNormalizado = NormalizeEmail(request.Correo);
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
            Correo = request.Correo.Trim(),
            CorreoNormalizado = correoNormalizado,
            CreadoEn = DateTime.UtcNow,
            Activo = true
        };

        usuario.PasswordHash = _passwordHasher.HashPassword(
            usuario,
            request.Password);

        _unitOfWork.UsuarioRepository.Add(usuario);
        await _unitOfWork.SaveAsync();

        return _mapper.Map<UsuarioDto>(usuario);
    }

    private static string NormalizeEmail(string correo)
    {
        return correo.Trim().ToUpperInvariant();
    }
}
