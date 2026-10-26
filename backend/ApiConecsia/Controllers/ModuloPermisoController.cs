using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

/// <summary>Acceso de los roles de la organización autenticada a los módulos (modulos_permisos).</summary>
[Route("api/roles/{idRol:int}/modulos")]
public sealed class ModuloPermisoController(ModuloPermisoService servicio) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AppModuloDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<AppModuloDto>>> Get(int idRol, CancellationToken ct)
    {
        if (!await servicio.PuedeConsultarAsync(User, ct)) return Prohibido();
        return this.Respuesta(await servicio.ObtenerModulosDeRolAsync(idRol, ct));
    }

    /// <summary>Reemplaza todas las asignaciones del rol (operación atómica).</summary>
    [HttpPut]
    [ProducesResponseType(typeof(IEnumerable<AppModuloDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<AppModuloDto>>> Put(int idRol, [FromBody] ReemplazarModulosRolDto dto, CancellationToken ct)
    {
        if (!await servicio.PuedeAdministrarRolesAsync(User, ct)) return Prohibido();
        var asignaciones = dto.Asignaciones
            ?? dto.IdsModulos!.Select(id => new AsignacionModuloDto { IdAppModulo = id, Consultar = true, Anadir = true, Editar = true }).ToList();
        return this.Respuesta(await servicio.ReemplazarAsync(User, idRol, asignaciones, ct));
    }

    [HttpPost("{idModulo:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Asignar(int idRol, int idModulo, CancellationToken ct)
    {
        if (!await servicio.PuedeAdministrarRolesAsync(User, ct)) return Prohibido();
        var resultado = await servicio.AsignarAsync(User, idRol, idModulo, ct);
        return resultado.Estado == ResultadoGestion.Ok ? NoContent() : this.Respuesta(resultado);
    }

    [HttpDelete("{idModulo:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retirar(int idRol, int idModulo, CancellationToken ct)
    {
        if (!await servicio.PuedeAdministrarRolesAsync(User, ct)) return Prohibido();
        var resultado = await servicio.RetirarAsync(User, idRol, idModulo, ct);
        return resultado.Estado == ResultadoGestion.Ok ? NoContent() : this.Respuesta(resultado);
    }

    private ObjectResult Prohibido() =>
        Problem(statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado", detail: "No tienes permiso para gestionar los módulos de los roles.");
}
