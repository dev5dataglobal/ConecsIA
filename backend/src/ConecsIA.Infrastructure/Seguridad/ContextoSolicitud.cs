using ConecsIA.SharedKernel.Seguridad;

namespace ConecsIA.Infrastructure.Seguridad;

/// <summary>
/// Implementación con ámbito por petición. Solo lo establece el middleware de contexto a partir de
/// una membresía verificada, o el código interno marcado como sistema (semillas, migraciones).
/// </summary>
public sealed class ContextoSolicitud : IContextoSolicitud
{
    private static readonly IReadOnlySet<string> SinPermisos = new HashSet<string>();
    private static readonly IReadOnlySet<Guid> SinSedes = new HashSet<Guid>();

    public bool EsSistema { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public Guid? OrganizacionId { get; private set; }
    public IReadOnlySet<string> Permisos { get; private set; } = SinPermisos;
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

    /// <summary>Usuario autenticado cuya organización todavía no se resolvió (o fue rechazada).</summary>
    public void EstablecerUsuario(Guid usuarioId) => UsuarioId = usuarioId;

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
}
