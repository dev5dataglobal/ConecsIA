using ConecsIA.Application.Seguridad;
using ApiConecsia.Helpers;
using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class UsuarioController : BaseApiController
{
    private readonly ConecsiaDbContext _db;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioController(ConecsiaDbContext db, IPasswordHasher<Usuario> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioPerfilDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioPerfilDto>> GetMe(CancellationToken cancellationToken)
    {
        var usuario = await UsuarioAutenticado(cancellationToken);
        if (usuario is null) return Unauthorized();
        return Ok(MapPerfil(usuario));
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UsuarioPerfilDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioPerfilDto>> UpdateMe([FromBody] ActualizarPerfilDto request, CancellationToken cancellationToken)
    {
        var usuario = await UsuarioAutenticado(cancellationToken);
        if (usuario is null) return Unauthorized();
        var correo = request.Correo.Trim();
        var normalizado = correo.ToUpperInvariant();
        var ocupado = await _db.Usuarios.AnyAsync(x => x.IdUsuario != usuario.IdUsuario && x.CorreoNormalizado == normalizado, cancellationToken);
        if (ocupado) return Conflict("El correo ya está asociado a otro usuario.");
        usuario.Correo = correo;
        usuario.CorreoNormalizado = normalizado;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapPerfil(usuario));
    }

    [HttpPut("me/contrasena")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword([FromBody] CambiarContrasenaDto request, CancellationToken cancellationToken)
    {
        var usuario = await UsuarioAutenticado(cancellationToken);
        if (usuario is null) return Unauthorized();
        if (request.NuevaContrasena != request.ConfirmacionContrasena) return BadRequest("La confirmación no coincide con la nueva contraseña.");
        if (!PoliticaContrasena.Cumple(request.NuevaContrasena)) return BadRequest(PoliticaContrasena.Mensaje);
        var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.ContrasenaActual);
        if (resultado == PasswordVerificationResult.Failed) return BadRequest("La contraseña actual no es correcta.");
        usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.NuevaContrasena);
        usuario.SelloSeguridad = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<Usuario?> UsuarioAutenticado(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(subject, out var id) ? await _db.Usuarios.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.IdUsuario == id && x.Activo, cancellationToken) : null;
    }

    private static UsuarioPerfilDto MapPerfil(Usuario usuario) => new() { IdUsuario = usuario.IdUsuario, Correo = usuario.Correo, Activo = usuario.Activo, UltimoAcceso = usuario.UltimoAcceso, CreadoEn = usuario.CreadoEn };
}
