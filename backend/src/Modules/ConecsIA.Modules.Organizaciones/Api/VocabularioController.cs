using ConecsIA.Modules.Organizaciones.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Organizaciones.Api;

/// <summary>HU-012: vocabulario de la organización del usuario.</summary>
[ApiController]
[Authorize]
[Route("api/v1/vocabulario")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class VocabularioController(ServicioVocabulario servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<VocabularioDto>(StatusCodes.Status200OK)]
    public Task<VocabularioDto> Obtener(CancellationToken cancelacion) => servicio.ObtenerAsync(cancelacion);

    [HttpPut]
    [ProducesResponseType<VocabularioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<VocabularioDto> Reemplazar(SolicitudVocabulario solicitud, CancellationToken cancelacion) =>
        servicio.ReemplazarAsync(solicitud, cancelacion);
}
