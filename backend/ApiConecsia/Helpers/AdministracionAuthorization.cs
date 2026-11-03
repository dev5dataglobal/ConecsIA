using System.Security.Claims;
using ConecsIA.Application.Interfaces;

namespace ApiConecsia.Helpers;

public static class AdministracionAuthorization
{
    public const string UsuariosConsultar = "usuarios.consultar";
    public const string UsuariosCrear = "usuarios.crear";
    public const string UsuariosModificar = "usuarios.modificar";
    public const string OrganizacionesConsultar = "saas.organizaciones.consultar";
    public const string OrganizacionesAdministrar = "saas.organizaciones.administrar";
    public const string ConfiguracionConsultar = "configuracion.consultar";
    public const string RolesConsultar = "roles.consultar";
    public const string RolesAdministrar = "roles.administrar";
    public const string PermisosAdministrar = "permisos.administrar";
    public const string ZonasEmpresarialesConsultar = "zonas_empresariales.consultar";
    public const string ZonasEmpresarialesAdministrar = "zonas_empresariales.administrar";
    public const string SedesAdministrar = "sedes.administrar";
    public const string SedesConsultar = "sedes.consultar";
    public const string AutorizacionesAdministrar = "autorizaciones.administrar";

    public static bool HasPermission(ClaimsPrincipal user, string permission) => user.HasClaim("permission", permission);
    public static bool IsPlatform(ClaimsPrincipal user, string action) => HasPermission(user, $"saas.{action}");
    public static int? CurrentOrganization(IOrganizationContext context) => context.OrganizationId;
}
