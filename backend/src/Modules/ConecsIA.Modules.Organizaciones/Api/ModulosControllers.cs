using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.SharedKernel.Modulos;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Organizaciones.Api;

/// <summary>HU-011: catálogo y habilitación de módulos desde el plano SaaS.</summary>
[ApiController]
[Authorize]
[SinOrganizacionResuelta]
[Route("api/v1/saas")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ModulosSaasController(ServicioModulos servicio) : ControllerBase
{
    [HttpGet("modulos")]
    [ProducesResponseType<IReadOnlyList<ModuloOpcional>>(StatusCodes.Status200OK)]
    public IReadOnlyList<ModuloOpcional> Catalogo() => servicio.Catalogo();

    [HttpGet("organizaciones/{id:guid}/modulos")]
    [ProducesResponseType<IReadOnlyList<ModuloOrganizacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ModuloOrganizacionDto>> Obtener(Guid id, CancellationToken cancelacion) =>
        servicio.ObtenerAsync(id, cancelacion);

    [HttpPut("organizaciones/{id:guid}/modulos")]
    [ProducesResponseType<IReadOnlyList<ModuloOrganizacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ModuloOrganizacionDto>> Cambiar(Guid id, SolicitudModulos solicitud, CancellationToken cancelacion) =>
        servicio.CambiarAsync(id, solicitud, cancelacion);
}

/// <summary>HU-011: módulos habilitados de la organización del usuario (la SPA oculta menús; solo UX).</summary>
[ApiController]
[Authorize]
[Route("api/v1/me/modulos")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class MisModulosController(IModulosHabilitados modulos) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<string>> Listar(CancellationToken cancelacion) => modulos.HabilitadosAsync(cancelacion);
}
