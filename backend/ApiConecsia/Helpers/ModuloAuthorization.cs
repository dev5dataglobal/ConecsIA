using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ApiConecsia.Helpers;

/// <summary>
/// Exige que el usuario tenga asignado el módulo (app_modulos.codigo) en su organización actual, con la acción indicada.
/// Sin acción explícita se deduce del verbo HTTP: GET/HEAD → Consultar, POST → Añadir, PUT/PATCH/DELETE → Editar.
/// Una acción explícita en el método reemplaza la deducida del atributo de la clase para el mismo módulo.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiereModuloAttribute : AuthorizeAttribute
{
    public const string Prefijo = "modulo:";

    public RequiereModuloAttribute(string codigoModulo)
    {
        CodigoModulo = codigoModulo;
        Policy = Prefijo + codigoModulo;
    }

    public RequiereModuloAttribute(string codigoModulo, AccionModulo accion)
    {
        CodigoModulo = codigoModulo;
        Accion = accion;
        Policy = $"{Prefijo}{codigoModulo}:{accion}";
    }

    public string CodigoModulo { get; }
    public AccionModulo? Accion { get; }
}

public sealed class ModuloRequirement(string codigoModulo, AccionModulo? accion) : IAuthorizationRequirement
{
    public string CodigoModulo { get; } = codigoModulo;
    /// <summary>null = se deduce del verbo HTTP.</summary>
    public AccionModulo? Accion { get; } = accion;
}

/// <summary>Crea bajo demanda las políticas "modulo:{codigo}[:{accion}]"; el resto lo resuelve el proveedor por defecto.</summary>
public sealed class ModuloPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _porDefecto = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequiereModuloAttribute.Prefijo, StringComparison.Ordinal))
            return _porDefecto.GetPolicyAsync(policyName);

        var partes = policyName[RequiereModuloAttribute.Prefijo.Length..].Split(':');
        AccionModulo? accion = partes.Length > 1 && Enum.TryParse<AccionModulo>(partes[1], out var valor) ? valor : null;
        return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ModuloRequirement(partes[0], accion))
            .Build());
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _porDefecto.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _porDefecto.GetFallbackPolicyAsync();
}

/// <summary>Consulta modulos_permisos (con caché invalidable). Sin organización, sin asignación o sin la acción ⇒ deniega.</summary>
public sealed class ModuloAuthorizationHandler(IModuloAccesoService accesos, IOrganizationContext organizationContext, IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<ModuloRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ModuloRequirement requirement)
    {
        var idUsuario = context.User.IdUsuario();
        var idOrganizacion = organizationContext.OrganizationId;
        if (idUsuario is null || idOrganizacion is null) return;

        var http = httpContextAccessor.HttpContext;
        var accion = requirement.Accion;
        if (accion is null)
        {
            // Si el método declara su propia acción para este módulo, la deducida del verbo no aplica.
            var explicita = http?.GetEndpoint()?.Metadata.GetOrderedMetadata<RequiereModuloAttribute>()
                .Any(x => x.Accion is not null && x.CodigoModulo == requirement.CodigoModulo) ?? false;
            if (explicita)
            {
                if (await accesos.TieneAccesoAsync(idUsuario.Value, idOrganizacion.Value, context.User.EsAdministradorGeneral(), requirement.CodigoModulo))
                    context.Succeed(requirement);
                return;
            }
            accion = AccionPorVerbo(http?.Request.Method);
        }

        if (await accesos.PuedeAsync(idUsuario.Value, idOrganizacion.Value, context.User.EsAdministradorGeneral(), requirement.CodigoModulo, accion.Value))
            context.Succeed(requirement);
    }

    public static AccionModulo AccionPorVerbo(string? metodo) => metodo?.ToUpperInvariant() switch
    {
        "POST" => AccionModulo.Anadir,
        "PUT" or "PATCH" or "DELETE" => AccionModulo.Editar,
        _ => AccionModulo.Consultar,
    };
}
