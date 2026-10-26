using ConecsIA.Application.Seguridad;
using ApiConecsia.Helpers;
using ConecsIA.Application.Interfaces.Repositories;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ConecsiaDbContext _db;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioController(IUnitOfWork unitOfWork, ConecsiaDbContext db, IPasswordHasher<Usuario> passwordHasher)
    {
        _unitOfWork = unitOfWork;
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
        if (!PoliticaContrasenaValida(request.NuevaContrasena)) return BadRequest("La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un número.");
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
    private static bool PoliticaContrasenaValida(string value) => value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit);

    [HttpGet]
    [RequiereModulo(CodigosModulo.Usuarios)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Usuario>>> Get()
    {
        var usuarios = await _unitOfWork.UsuarioRepository.GetAllAsync();

        return Ok(usuarios);
    }

    [HttpGet("{id:int}")]
    [RequiereModulo(CodigosModulo.Usuarios)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Usuario>> Get(int id)
    {
        try
        {
            var usuario = await _unitOfWork.UsuarioRepository.GetByIdAsync(id);

            return Ok(usuario);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Usuario with id {id} was not found.");
        }
    }

    [HttpPost]
    [RequiereModulo(CodigosModulo.Usuarios)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Usuario>> Post([FromBody] Usuario entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.UsuarioRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [RequiereModulo(CodigosModulo.Usuarios)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Usuario entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.UsuarioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Usuario with id {id} was not found.");
        }

        _unitOfWork.UsuarioRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [RequiereModulo(CodigosModulo.Usuarios)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Usuario entity;

        try
        {
            entity = await _unitOfWork.UsuarioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Usuario with id {id} was not found.");
        }

        _unitOfWork.UsuarioRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
