using ConecsIA.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ApiConecsia.Helpers;

/// <summary>Exige que el usuario tenga asignado el módulo (app_modulos.codigo) en su organización actual.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiereModuloAttribute : AuthorizeAttribute
{
    public const string Prefijo = "modulo:";

    public RequiereModuloAttribute(string codigoModulo)
    {
        CodigoModulo = codigoModulo;
        Policy = Prefijo + codigoModulo;
    }

    public string CodigoModulo { get; }
}

public sealed class ModuloRequirement(string codigoModulo) : IAuthorizationRequirement
{
    public string CodigoModulo { get; } = codigoModulo;
}

/// <summary>Crea bajo demanda las políticas "modulo:{codigo}"; el resto lo resuelve el proveedor por defecto.</summary>
public sealed class ModuloPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _porDefecto = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequiereModuloAttribute.Prefijo, StringComparison.Ordinal))
            return _porDefecto.GetPolicyAsync(policyName);

        var codigo = policyName[RequiereModuloAttribute.Prefijo.Length..];
        return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new ModuloRequirement(codigo))
            .Build());
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _porDefecto.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _porDefecto.GetFallbackPolicyAsync();
}

/// <summary>Consulta modulos_permisos (con caché invalidable). Sin organización o sin asignación ⇒ deniega.</summary>
public sealed class ModuloAuthorizationHandler(IModuloAccesoService accesos, IOrganizationContext organizationContext)
    : AuthorizationHandler<ModuloRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ModuloRequirement requirement)
    {
        var idUsuario = context.User.IdUsuario();
        var idOrganizacion = organizationContext.OrganizationId;
        if (idUsuario is null || idOrganizacion is null) return;

        if (await accesos.TieneAccesoAsync(idUsuario.Value, idOrganizacion.Value, context.User.EsAdministradorGeneral(), requirement.CodigoModulo))
            context.Succeed(requirement);
    }
}
