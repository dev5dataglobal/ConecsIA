using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Organizaciones.Api;

/// <summary>HU-010: catálogos de la organización del usuario.</summary>
[ApiController]
[Authorize]
[Route("api/v1/catalogos")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class CatalogosController(ServicioCatalogos servicio) : ControllerBase
{
    [HttpGet("{catalogo}")]
    [ProducesResponseType<IReadOnlyList<ElementoCatalogoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ElementoCatalogoDto>> Listar(string catalogo, CancellationToken cancelacion) =>
        servicio.ListarAsync(catalogo, cancelacion);

    [HttpPost("{catalogo}")]
    [ProducesResponseType<ElementoCatalogoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Agregar(string catalogo, SolicitudElemento solicitud, CancellationToken cancelacion)
    {
        var elemento = await servicio.AgregarAsync(catalogo, solicitud, cancelacion);
        return Created($"/api/v1/catalogos/{catalogo}/{elemento.Id}", elemento);
    }

    [HttpPut("{catalogo}/{id:guid}")]
    [ProducesResponseType<ElementoCatalogoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ElementoCatalogoDto> Renombrar(string catalogo, Guid id, SolicitudRenombrar solicitud, CancellationToken cancelacion) =>
        servicio.RenombrarAsync(catalogo, id, solicitud, cancelacion);

    [HttpPost("{catalogo}/{id:guid}/inactivar")]
    [ProducesResponseType<ElementoCatalogoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ElementoCatalogoDto> Inactivar(string catalogo, Guid id, CancellationToken cancelacion) =>
        servicio.InactivarAsync(catalogo, id, cancelacion);
}

/// <summary>HU-010: aplicación de la plantilla desde el plano SaaS.</summary>
[ApiController]
[Authorize]
[SinOrganizacionResuelta]
[Route("api/v1/saas/organizaciones")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class PlantillasSaasController(ServicioPlantillasSaas servicio) : ControllerBase
{
    [HttpPost("{id:guid}/aplicar-plantilla")]
    [ProducesResponseType<ResultadoPlantilla>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ResultadoPlantilla> Aplicar(Guid id, CancellationToken cancelacion) => servicio.AplicarAsync(id, cancelacion);
}
