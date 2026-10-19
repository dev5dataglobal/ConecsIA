using System.Reflection;
using ConecsIA.Api.Composicion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Api.Sistema;

/// <summary>Información técnica pública del servicio. No expone configuración ni entorno.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/sistema")]
public sealed class SistemaController : ControllerBase
{
    public sealed record InformacionSistema(string Version, IReadOnlyList<string> Modulos);

    [HttpGet("version")]
    [ProducesResponseType<InformacionSistema>(StatusCodes.Status200OK)]
    public InformacionSistema ObtenerVersion()
    {
        var version = typeof(SistemaController).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        return new InformacionSistema(version, CatalogoModulos.Todos.Select(m => m.Nombre).ToList());
    }
}
