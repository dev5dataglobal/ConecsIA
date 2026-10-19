using ConecsIA.Modules.Estructura.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Estructura.Api;

/// <summary>HU-013 Gestionar sedes. Permisos y alcance se verifican en <see cref="ServicioSedes"/>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/sedes")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class SedesController(ServicioSedes servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaDto<SedeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PaginaDto<SedeDto>> Listar([FromQuery] ConsultaSedes consulta, CancellationToken cancelacion) =>
        servicio.ListarAsync(consulta, cancelacion);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SedeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<SedeDto> Obtener(Guid id, CancellationToken cancelacion) => servicio.ObtenerAsync(id, cancelacion);

    [HttpPost]
    [ProducesResponseType<SedeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(SolicitudCrearSede solicitud, CancellationToken cancelacion)
    {
        var sede = await servicio.CrearAsync(solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = sede.Id }, sede);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<SedeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<SedeDto> Modificar(Guid id, SolicitudModificarSede solicitud, CancellationToken cancelacion) =>
        servicio.ModificarAsync(id, solicitud, cancelacion);

    [HttpPost("{id:guid}/inactivar")]
    [ProducesResponseType<SedeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<SedeDto> Inactivar(Guid id, CancellationToken cancelacion) => servicio.InactivarAsync(id, cancelacion);
}

