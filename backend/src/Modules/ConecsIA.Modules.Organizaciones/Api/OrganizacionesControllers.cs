using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Organizaciones.Api;

/// <summary>HU-009 en el plano SaaS. No necesita organización resuelta; exige permisos SaaS.</summary>
[ApiController]
[Authorize]
[SinOrganizacionResuelta]
[Route("api/v1/saas/organizaciones")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class OrganizacionesSaasController(ServicioOrganizacionesSaas servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PaginaOrganizacionesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PaginaOrganizacionesDto> Listar([FromQuery] ConsultaOrganizacionesSaas consulta, CancellationToken cancelacion) =>
        servicio.ListarAsync(consulta, cancelacion);

    [HttpGet("tipos")]
    [ProducesResponseType<IReadOnlyList<TipoOrganizacion>>(StatusCodes.Status200OK)]
    public IReadOnlyList<TipoOrganizacion> Tipos() => servicio.Tipos();

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<OrganizacionDto> Obtener(Guid id, CancellationToken cancelacion) => servicio.ObtenerAsync(id, cancelacion);

    [HttpPost]
    [ProducesResponseType<OrganizacionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(SolicitudCrearOrganizacion solicitud, CancellationToken cancelacion)
    {
        var organizacion = await servicio.CrearAsync(solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = organizacion.Id }, organizacion);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<OrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<OrganizacionDto> Modificar(Guid id, SolicitudModificarOrganizacion solicitud, CancellationToken cancelacion) =>
        servicio.ModificarAsync(id, solicitud, cancelacion);

    [HttpPost("{id:guid}/inactivar")]
    [ProducesResponseType<OrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<OrganizacionDto> Inactivar(Guid id, CancellationToken cancelacion) => servicio.InactivarAsync(id, cancelacion);

    [HttpPost("{id:guid}/reactivar")]
    [ProducesResponseType<OrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<OrganizacionDto> Reactivar(Guid id, CancellationToken cancelacion) => servicio.ReactivarAsync(id, cancelacion);
}

/// <summary>HU-009: información general de la propia organización. Otro identificador = 404.</summary>
[ApiController]
[Authorize]
[Route("api/v1/organizaciones")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class MiOrganizacionController(ServicioMiOrganizacion servicio) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<OrganizacionDto> Obtener(Guid id, CancellationToken cancelacion) => servicio.ObtenerAsync(id, cancelacion);

    [HttpPut("{id:guid}")]
    [ProducesResponseType<OrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<OrganizacionDto> Modificar(Guid id, SolicitudModificarMiOrganizacion solicitud, CancellationToken cancelacion) =>
        servicio.ModificarAsync(id, solicitud, cancelacion);
}
