namespace ConecsIA.Application.Interfaces;

/// <summary>
/// Resuelve el acceso a módulos a partir de membresías activas, roles activos y modulos_permisos.
/// Denegación por defecto: un módulo no asignado (o inactivo) no está permitido.
/// </summary>
public interface IModuloAccesoService
{
    Task<IReadOnlySet<string>> ObtenerCodigosPermitidosAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, CancellationToken cancellationToken = default);
    Task<bool> TieneAccesoAsync(int idUsuario, int idOrganizacion, bool esAdministradorGeneral, string codigoModulo, CancellationToken cancellationToken = default);
    /// <summary>Invalida la caché de una organización (cambio de asignaciones de sus roles).</summary>
    void InvalidarOrganizacion(int idOrganizacion);
    /// <summary>Invalida toda la caché (cambio en el catálogo global).</summary>
    void InvalidarTodo();
}
