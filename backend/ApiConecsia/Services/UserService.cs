using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using ConecsIA.Infrastructure.Data;
using ApiConecsia.Helpers;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ConecsiaDbContext _db;
    private readonly JWT _jwt;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<Usuario> passwordHasher,
        IMapper mapper,
        ConecsiaDbContext db,
        IOptions<JWT> jwt)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        LoginDto request,
        CancellationToken cancellationToken = default)
    {
        var correoNormalizado = NormalizeEmail(request.Correo);
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.CorreoNormalizado == correoNormalizado,
                cancellationToken);

        if (usuario is null || !usuario.Activo)
        {
            return new AuthenticationResult(FailureReason: AuthenticationFailureReason.UserNotRegistered);
        }

        if (usuario.BloqueadoHasta > DateTime.UtcNow)
        {
            return new AuthenticationResult(FailureReason: AuthenticationFailureReason.UserLocked);
        }

        var result = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= 5)
            {
                usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(15);
                usuario.IntentosFallidos = 0;
            }
            await _db.SaveChangesAsync(cancellationToken);
            return new AuthenticationResult(FailureReason: AuthenticationFailureReason.InvalidPassword);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var organizationIds = await _db.MembresiasOrganizacion
            .IgnoreQueryFilters()
            .Where(x => x.IdUsuario == usuario.IdUsuario &&
                (x.Estado == "Activa" || x.Estado == "Aceptada"))
            .Select(x => x.IdOrganizacion)
            .Distinct()
            .ToListAsync(cancellationToken);

        var roleIds = await _db.MembresiasRoles
            .IgnoreQueryFilters()
            .Where(x => organizationIds.Contains(x.IdOrganizacion) &&
                _db.MembresiasOrganizacion.Any(m => m.IdMembresia == x.IdMembresia &&
                    m.IdUsuario == usuario.IdUsuario &&
                    (m.Estado == "Activa" || m.Estado == "Aceptada")))
            .Select(x => x.IdRol).Distinct().ToListAsync(cancellationToken);

        var roles = await _db.Roles.IgnoreQueryFilters()
            .Where(x => roleIds.Contains(x.IdRol) && x.Activo)
            .Select(x => x.Nombre).ToListAsync(cancellationToken);

        var permissions = await (from rp in _db.RolesPermisos.IgnoreQueryFilters()
                                 join p in _db.Permisos on rp.IdPermiso equals p.IdPermiso
                                 where roleIds.Contains(rp.IdRol) && p.Activo
                                 select p.Codigo).Distinct().ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.DurationInMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new("security_stamp", usuario.SelloSeguridad ?? string.Empty)
        };
        claims.AddRange(organizationIds.Select(id => new Claim("organization_id", id.ToString())));
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var token = new JwtSecurityTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256)
        });

        return new AuthenticationResult(new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires,
            Usuario = _mapper.Map<UsuarioDto>(usuario)
        });
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
