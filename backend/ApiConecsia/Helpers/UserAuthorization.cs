using System.Security.Claims;

namespace ApiConecsia.Helpers;

public static class UserAuthorization
{
    public static class Claims
    {
        public const string UserId = "sub";
        public const string OrganizationId = "organization_id";
        public const string Permission = "permission";
        public const string PlatformAdmin = "platform_admin";
    }

    public static class Policies
    {
        public const string Authenticated = "authenticated";
    }

    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Receptionist = "Receptionist";
        public const string Security = "Security";
    }

    /// <summary>
    /// Regla vigente del administrador general (usuario 1), centralizada aquí.
    /// Pendiente D-23: reemplazarla por un rol/marca de plataforma persistida.
    /// </summary>
    public static bool EsUsuarioAdministradorGeneral(int idUsuario) => idUsuario == 1;

    public static int? IdUsuario(this ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(Claims.UserId);
        return int.TryParse(valor, out var id) ? id : null;
    }

    public static bool EsAdministradorGeneral(this ClaimsPrincipal user) =>
        user.HasClaim(Claims.PlatformAdmin, "true");
}
