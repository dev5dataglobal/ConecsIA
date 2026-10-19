using ConecsIA.SharedKernel.Seguridad;

namespace ConecsIA.Infrastructure.Seguridad;

/// <summary>
/// Implementación con ámbito por petición. Solo lo establece el middleware de contexto a partir de
/// una membresía verificada, o el código interno marcado como sistema (semillas, migraciones).
/// </summary>
public sealed class ContextoSolicitud : IContextoSolicitud, IContextoPlanoSaas
{
    private static readonly IReadOnlySet<string> SinPermisos = new HashSet<string>();
    private static readonly IReadOnlySet<Guid> SinSedes = new HashSet<Guid>();

    public bool EsSistema { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public Guid? OrganizacionId { get; private set; }
    public IReadOnlySet<string> Permisos { get; private set; } = SinPermisos;
    public IReadOnlySet<string> PermisosSaas { get; private set; } = SinPermisos;
    public bool AlcanceTotalSedes { get; private set; }
    public IReadOnlySet<Guid> SedesAsignadas { get; private set; } = SinSedes;

    public void Establecer(
        Guid usuarioId,
        Guid organizacionId,
        IEnumerable<string> permisos,
        bool alcanceTotalSedes,
        IEnumerable<Guid> sedesAsignadas)
    {
        UsuarioId = usuarioId;
        OrganizacionId = organizacionId;
        Permisos = permisos.ToHashSet(StringComparer.Ordinal);
        AlcanceTotalSedes = alcanceTotalSedes;
        SedesAsignadas = sedesAsignadas.ToHashSet();
    }

    /// <summary>
    /// Usuario autenticado sin organización resuelta (o rechazada). En endpoints
    /// <c>[SinOrganizacionResuelta]</c> lleva sus permisos del plano SaaS (HU-009).
    /// </summary>
    public void EstablecerUsuario(Guid usuarioId, IEnumerable<string>? permisosSaas = null)
    {
        UsuarioId = usuarioId;
        PermisosSaas = permisosSaas?.ToHashSet(StringComparer.Ordinal) ?? SinPermisos;
    }

    public void ComoSistema() => EsSistema = true;

    /// <summary>
    /// Proceso de fondo (HU-007): opera en una organización explícita, sin usuario y solo con los
    /// permisos indicados. El filtro global y la verificación al guardar siguen activos.
    /// </summary>
    public void ComoProceso(Guid organizacionId, IEnumerable<string> permisos)
    {
        OrganizacionId = organizacionId;
        Permisos = permisos.ToHashSet(StringComparer.Ordinal);
        AlcanceTotalSedes = true;
    }

    /// <summary>
    /// HU-009: el plano SaaS escribe en una organización concreta (aprovisionamiento). No concede
    /// permisos de organización; sin el permiso SaaS de administración se rechaza.
    /// </summary>
    public void OperarEnOrganizacion(Guid organizacionId)
    {
        if (!PermisosSaas.Contains(SharedKernel.Seguridad.PermisosSaas.OrganizacionesAdministrar))
        {
            throw new AccesoDenegadoException(AccesoDenegadoException.CodigoPermisoRequerido, "No tienes el permiso requerido para esta operación.");
        }

        OrganizacionId = organizacionId;
    }
}
