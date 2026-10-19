using ConecsIA.Modules.Identidad.Application;
using ConecsIA.SharedKernel.Organizaciones;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ConecsIA.Modules.Identidad.Api;

/// <summary>Contexto efectivo del usuario, derivado en el servidor (HU-007). La SPA lo usa solo para presentar.</summary>
[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController(
    IContextoSolicitud contexto,
    IRepositorioIdentidad repositorio,
    IConsultaOrganizaciones organizaciones) : ControllerBase
{
    public sealed record UsuarioDto(Guid Id, string Nombre, string Correo);

    public sealed record OrganizacionDto(Guid Id, string Nombre);

    public sealed record ContextoDto(
        UsuarioDto Usuario,
        OrganizacionDto Organizacion,
        IReadOnlyList<string> Permisos,
        bool AlcanceTotalSedes,
        IReadOnlyList<Guid> SedesAsignadas);

    [HttpGet]
    [ProducesResponseType<ContextoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ContextoDto> Obtener(CancellationToken cancelacion)
    {
        var usuario = (await repositorio.ObtenerUsuarioAsync(contexto.UsuarioId!.Value, cancelacion))!;
        var organizacion = (await organizaciones.ObtenerAsync(contexto.OrganizacionId!.Value, cancelacion))!;

        return new ContextoDto(
            new UsuarioDto(usuario.Id, usuario.Nombre, usuario.Correo),
            new OrganizacionDto(organizacion.Id, organizacion.Nombre),
            contexto.Permisos.Order(StringComparer.Ordinal).ToList(),
            contexto.AlcanceTotalSedes,
            contexto.SedesAsignadas.ToList());
    }
}

/// <summary>
/// Solo con la autenticación de desarrollo habilitada (Development/Testing): lista los usuarios
/// sintéticos para elegir identidad. Sustituto temporal de HU-006 mientras D-03 está abierta.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/desarrollo")]
public sealed class DesarrolloController(
    IConfiguration configuracion,
    IRepositorioIdentidad repositorio,
    Infrastructure.ConsultaUsuariosDesarrollo consulta) : ControllerBase
{
    public const string ClaveHabilitada = "Autenticacion:Desarrollo:Habilitada";

    public sealed record UsuarioDesarrolloDto(Guid Id, string Nombre, string Correo, IReadOnlyList<string> Organizaciones);

    [HttpGet("usuarios")]
    [ProducesResponseType<IReadOnlyList<UsuarioDesarrolloDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Usuarios(CancellationToken cancelacion)
    {
        if (!configuracion.GetValue<bool>(ClaveHabilitada)) return NotFound();

        var organizaciones = await consulta.OrganizacionesPorUsuarioAsync(cancelacion);
        var usuarios = await repositorio.UsuariosActivosAsync(cancelacion);
        return Ok(usuarios.Select(u => new UsuarioDesarrolloDto(
            u.Id,
            u.Nombre,
            u.Correo,
            organizaciones.GetValueOrDefault(u.Id) ?? [])).ToList());
    }
}
