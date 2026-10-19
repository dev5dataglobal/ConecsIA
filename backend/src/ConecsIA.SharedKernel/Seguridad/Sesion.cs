namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Política de autenticación aprobada en ADR-006 (D-03, 2026-10-19). Un solo lugar para los valores
/// que comparten el caso de uso de ingreso y la cookie de sesión.
/// </summary>
public static class PoliticaAutenticacion
{
    public const int IntentosAntesDeBloquear = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan DuracionMaximaSesion = TimeSpan.FromHours(8);
    public static readonly TimeSpan CierrePorInactividad = TimeSpan.FromMinutes(30);
    public const int LongitudMinimaContrasena = 12;
}

/// <summary>
/// Emite y retira la sesión del usuario. Los módulos no conocen el esquema concreto (cookie de
/// Identity hoy, OIDC externo si se añade): lo implementa la composición de la API.
/// </summary>
public interface ISesionUsuario
{
    /// <summary>Claim con el id de la sesión registrada en el servidor.</summary>
    public const string ClaimSesion = "conecsia:sesion";

    Task IniciarAsync(Guid usuarioId, Guid sesionId, string selloSeguridad);

    Task CerrarAsync();
}

/// <summary>Credenciales inválidas: 401 con mensaje genérico que no revela si el usuario existe.</summary>
public sealed class CredencialesInvalidasException() : Exception("Correo o contraseña incorrectos.")
{
    public const string Codigo = "CREDENCIALES_INVALIDAS";
}

/// <summary>Cuenta bloqueada temporalmente por intentos fallidos: 423.</summary>
public sealed class CuentaBloqueadaException()
    : Exception("Demasiados intentos fallidos. Inténtalo de nuevo más tarde.")
{
    public const string Codigo = "CUENTA_BLOQUEADA";
}
