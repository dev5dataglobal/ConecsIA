using ConecsIA.Modules.Estructura.Application;
using ConecsIA.Modules.Estructura.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Estructura.Api;

/// <summary>HU-014 Áreas o destinos. Permisos y alcance de sede en <see cref="ServicioAreas"/>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/areas")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AreasController(ServicioAreas servicio) : ControllerBase
{
    [HttpGet("~/api/v1/sedes/{sedeId:guid}/areas")]
    [ProducesResponseType<IReadOnlyList<AreaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<AreaDto>> Listar(Guid sedeId, CancellationToken cancelacion) => servicio.ListarAsync(sedeId, cancelacion);

    [HttpPost("~/api/v1/sedes/{sedeId:guid}/areas")]
    [ProducesResponseType<AreaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(Guid sedeId, SolicitudCrearArea solicitud, CancellationToken cancelacion)
    {
        var area = await servicio.CrearAsync(sedeId, solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = area.Id }, area);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AreaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AreaDto> Obtener(Guid id, CancellationToken cancelacion) => servicio.ObtenerAsync(id, cancelacion);

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AreaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<AreaDto> Modificar(Guid id, SolicitudModificarArea solicitud, CancellationToken cancelacion) =>
        servicio.ModificarAsync(id, solicitud, cancelacion);

    [HttpPost("{id:guid}/inactivar")]
    [ProducesResponseType<AreaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<AreaDto> Inactivar(Guid id, CancellationToken cancelacion) => servicio.InactivarAsync(id, cancelacion);

    [HttpGet("{id:guid}/asignaciones")]
    [ProducesResponseType<IReadOnlyList<AsignacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<AsignacionDto>> Historial(Guid id, CancellationToken cancelacion) => servicio.HistorialAsync(id, cancelacion);

    [HttpPut("{id:guid}/responsables")]
    [ProducesResponseType<AreaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<AreaDto> Responsables(Guid id, SolicitudAsignacion solicitud, CancellationToken cancelacion) =>
        servicio.AsignarAsync(id, FuncionEnArea.Responsable, solicitud, cancelacion);

    [HttpPut("{id:guid}/autorizadores")]
    [ProducesResponseType<AreaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<AreaDto> Autorizadores(Guid id, SolicitudAsignacion solicitud, CancellationToken cancelacion) =>
        servicio.AsignarAsync(id, FuncionEnArea.Autorizador, solicitud, cancelacion);
}
