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
    /// <summary>Margen en que una cookie recién rotada aún se acepta (pestañas que renuevan a la vez).</summary>
    private static readonly TimeSpan VentanaConcurrencia = TimeSpan.FromSeconds(30);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IMapper _mapper;
    private readonly ConecsiaDbContext _db;
    private readonly JWT _jwt;
    private readonly SesionOptions _sesion;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordHasher<Usuario> passwordHasher,
        IMapper mapper,
        ConecsiaDbContext db,
        IOptions<JWT> jwt,
        IOptions<SesionOptions> sesion)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _db = db;
        _jwt = jwt.Value;
        _sesion = sesion.Value;
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

        var acceso = await EmitirAccesoAsync(usuario, cancellationToken);
        if (acceso is null)
        {
            return new AuthenticationResult(FailureReason: AuthenticationFailureReason.UserInactive);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = DateTime.UtcNow;

        var expiraSesion = DateTime.UtcNow.AddHours(_sesion.HorasMaximasSesion);
        var (token, _) = NuevaSesion(usuario, request.Recordarme, expiraSesion);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(acceso, Sesion: new SesionEmitida(token, expiraSesion, request.Recordarme));
    }

    public async Task<AuthenticationResult> RenovarAsync(
        string? tokenRefresco,
        CancellationToken cancellationToken = default)
    {
        var expirada = new AuthenticationResult(FailureReason: AuthenticationFailureReason.SessionExpired);
        if (string.IsNullOrWhiteSpace(tokenRefresco)) return expirada;

        var ahora = DateTime.UtcNow;
        var hash = TokensSeguros.Hash(tokenRefresco);
        var sesion = await _db.SesionesRefresco.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (sesion is null || sesion.ExpiraEn <= ahora) return expirada;

        var usuario = await _db.Usuarios.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.IdUsuario == sesion.IdUsuario, cancellationToken);
        if (usuario is null || !usuario.Activo || usuario.SelloSeguridad != sesion.SelloSeguridad) return expirada;

        if (sesion.RevocadoEn is not null)
        {
            // La pestaña que llega tarde con la misma cookie recibe un acceso nuevo sin rotar otra vez
            // (el navegador ya guardó la cookie rotada). Fuera de esa ventana, el token no sirve.
            if (sesion.IdReemplazo is null || ahora - sesion.RevocadoEn.Value > VentanaConcurrencia) return expirada;
            var accesoConcurrente = await EmitirAccesoAsync(usuario, cancellationToken);
            return accesoConcurrente is null ? expirada : new AuthenticationResult(accesoConcurrente);
        }

        var acceso = await EmitirAccesoAsync(usuario, cancellationToken);
        if (acceso is null)
        {
            await RevocarAsync(sesion.IdSesionRefresco, ahora, cancellationToken);
            return new AuthenticationResult(FailureReason: AuthenticationFailureReason.UserInactive);
        }

        // Revocación condicional: dos renovaciones simultáneas no pueden rotar la misma fila dos veces.
        if (await RevocarAsync(sesion.IdSesionRefresco, ahora, cancellationToken) == 0)
        {
            return new AuthenticationResult(acceso);
        }

        // La sesión rotada conserva el vencimiento absoluto del inicio de sesión.
        var (token, nueva) = NuevaSesion(usuario, sesion.Recordar, sesion.ExpiraEn);
        await _db.SaveChangesAsync(cancellationToken);
        await _db.SesionesRefresco
            .Where(x => x.IdSesionRefresco == sesion.IdSesionRefresco)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.IdReemplazo, nueva.IdSesionRefresco), cancellationToken);

        return new AuthenticationResult(acceso, Sesion: new SesionEmitida(token, sesion.ExpiraEn, sesion.Recordar));
    }

    public async Task CerrarSesionAsync(
        string? tokenRefresco,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenRefresco)) return;
        var hash = TokensSeguros.Hash(tokenRefresco);
        await _db.SesionesRefresco
            .Where(x => x.TokenHash == hash && x.RevocadoEn == null)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.RevocadoEn, DateTime.UtcNow), cancellationToken);
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

    /// <summary>
    /// Token de acceso con las membresías, perfiles y permisos vigentes en este instante.
    /// null si el usuario no tiene ninguna membresía activa en una organización activa.
    /// </summary>
    private async Task<AuthResponseDto?> EmitirAccesoAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        // Solo cuentan membresías activas en organizaciones activas; sin ninguna, el usuario está inactivo.
        var organizationIds = await _db.MembresiasOrganizacion
            .IgnoreQueryFilters()
            .Where(x => x.IdUsuario == usuario.IdUsuario &&
                (x.Estado == "Activa" || x.Estado == "Aceptada") &&
                _db.Organizaciones.Any(o => o.IdOrganizacion == x.IdOrganizacion && o.Estado == "Activa"))
            .Select(x => x.IdOrganizacion)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (organizationIds.Count == 0 && !UserAuthorization.EsUsuarioAdministradorGeneral(usuario.IdUsuario))
        {
            return null;
        }

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

        // Solo los perfiles activos aportan permisos, igual que en el acceso por módulo.
        var permissions = await (from rp in _db.RolesPermisos.IgnoreQueryFilters()
                                 join p in _db.Permisos on rp.IdPermiso equals p.IdPermiso
                                 join r in _db.Roles.IgnoreQueryFilters() on rp.IdRol equals r.IdRol
                                 where roleIds.Contains(rp.IdRol) && p.Activo && r.Activo
                                 select p.Codigo).Distinct().ToListAsync(cancellationToken);

        if (UserAuthorization.EsUsuarioAdministradorGeneral(usuario.IdUsuario))
        {
            permissions = await _db.Permisos.IgnoreQueryFilters()
                .Where(x => x.Activo)
                .Select(x => x.Codigo)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

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
        if (UserAuthorization.EsUsuarioAdministradorGeneral(usuario.IdUsuario))
        {
            claims.Add(new Claim(UserAuthorization.Claims.PlatformAdmin, "true"));
        }

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

        return new AuthResponseDto
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires,
            Usuario = _mapper.Map<UsuarioDto>(usuario)
        };
    }

    private (string Token, SesionRefresco Sesion) NuevaSesion(Usuario usuario, bool recordar, DateTime expiraEn)
    {
        var token = TokensSeguros.Generar();
        var sesion = new SesionRefresco
        {
            IdUsuario = usuario.IdUsuario,
            TokenHash = TokensSeguros.Hash(token),
            Recordar = recordar,
            SelloSeguridad = usuario.SelloSeguridad ?? string.Empty,
            CreadoEn = DateTime.UtcNow,
            ExpiraEn = expiraEn
        };
        _db.SesionesRefresco.Add(sesion);
        return (token, sesion);
    }

    private Task<int> RevocarAsync(long idSesion, DateTime ahora, CancellationToken cancellationToken) =>
        _db.SesionesRefresco
            .Where(x => x.IdSesionRefresco == idSesion && x.RevocadoEn == null)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.RevocadoEn, ahora), cancellationToken);

    private static string NormalizeEmail(string correo)
    {
        return correo.Trim().ToUpperInvariant();
    }
}
