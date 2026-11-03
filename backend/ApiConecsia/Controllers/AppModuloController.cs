using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

/// <summary>Catálogo global de módulos de la aplicación (app_modulos).</summary>
[Route("api/app-modulos")]
public sealed class AppModuloController(ModuloPermisoService servicio) : BaseApiController
{
    /// <summary>Módulos activos a los que el usuario autenticado tiene acceso en su organización (menú).</summary>
    [HttpGet("mios")]
    [ProducesResponseType(typeof(IEnumerable<AppModuloDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AppModuloDto>>> Mios(CancellationToken ct) =>
        Ok(await servicio.ListarPermitidosAsync(User, ct));

    /// <summary>Catálogo completo (administrador general o administración de la organización).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AppModuloDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<AppModuloDto>>> Get(CancellationToken ct)
    {
        if (!await servicio.PuedeConsultarAsync(User, ct)) return Prohibido();
        return Ok(await servicio.ListarCatalogoAsync(ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppModuloDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppModuloDto>> Post([FromBody] AppModuloGuardarDto dto, CancellationToken ct)
    {
        if (!User.EsAdministradorGeneral()) return Prohibido();
        var resultado = await servicio.CrearModuloAsync(User, dto, ct);
        return resultado.Estado == ResultadoGestion.Ok
            ? CreatedAtAction(nameof(Get), null, resultado.Valor)
            : this.Respuesta(resultado);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AppModuloDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppModuloDto>> Put(int id, [FromBody] AppModuloGuardarDto dto, CancellationToken ct)
    {
        if (!User.EsAdministradorGeneral()) return Prohibido();
        return this.Respuesta(await servicio.ActualizarModuloAsync(User, id, dto, ct));
    }

    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(typeof(AppModuloDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppModuloDto>> CambiarEstado(int id, [FromBody] AppModuloEstadoDto dto, CancellationToken ct)
    {
        if (!User.EsAdministradorGeneral()) return Prohibido();
        return this.Respuesta(await servicio.CambiarEstadoModuloAsync(User, id, dto.Activo!.Value, ct));
    }

    private ObjectResult Prohibido() =>
        Problem(statusCode: StatusCodes.Status403Forbidden, title: "Acceso denegado", detail: "No tienes permiso para gestionar el catálogo de módulos.");
}
