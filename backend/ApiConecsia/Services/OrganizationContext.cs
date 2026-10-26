using System.Security.Claims;
using ConecsIA.Application.Interfaces;

namespace ApiConecsia.Services;

/// <summary>
/// Organización activa de la petición. Por defecto es la primera membresía del token; el administrador de
/// plataforma puede seleccionar otra con la cabecera <see cref="Cabecera"/>, validada en cada petición por
/// <see cref="ContextoOrganizacionMiddleware"/> (nunca se persiste en el servidor ni se comparte entre peticiones).
/// </summary>
public sealed class OrganizationContext(IHttpContextAccessor httpContextAccessor)
    : IOrganizationContext
{
    public const string Cabecera = "X-Organizacion-Id";
    internal const string ClaveSeleccion = "conecsia.organizacion.seleccionada";

    public int? OrganizationId
    {
        get
        {
            var http = httpContextAccessor.HttpContext;
            if (http?.Items[ClaveSeleccion] is int seleccionada) return seleccionada;
            var value = http?.User.FindFirstValue("organization_id");
            return int.TryParse(value, out var id) && id > 0 ? id : null;
        }
    }
}
