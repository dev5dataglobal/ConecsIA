namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Contexto verificado de la petición (ADR-005): usuario autenticado, organización derivada de una
/// membresía activa, permisos efectivos y alcance por sede. Nunca se construye con datos del cliente.
/// </summary>
public interface IContextoSolicitud
{
    /// <summary>Procesos internos (migración, semillas): sin filtro de organización. Nunca en peticiones HTTP.</summary>
    bool EsSistema { get; }

    Guid? UsuarioId { get; }

    Guid? OrganizacionId { get; }

    IReadOnlySet<string> Permisos { get; }

    bool AlcanceTotalSedes { get; }

    IReadOnlySet<Guid> SedesAsignadas { get; }

    /// <summary>Permisos del plano SaaS del usuario (HU-009). Solo se cargan en endpoints sin organización resuelta.</summary>
    IReadOnlySet<string> PermisosSaas { get; }

    bool TienePermiso(string permiso) => EsSistema || Permisos.Contains(permiso);

    bool TienePermisoSaas(string permiso) => EsSistema || PermisosSaas.Contains(permiso);

    bool PuedeOperarEnSede(Guid sedeId) => EsSistema || AlcanceTotalSedes || SedesAsignadas.Contains(sedeId);
}
