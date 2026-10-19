using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Identidad.Domain;

/// <summary>
/// Vínculo usuario–organización con sus permisos efectivos y su alcance por sede (ADR-005).
/// Provisional: guarda las claves de permiso directamente. Los roles configurables que las agrupan
/// (HU-017) y la administración del alcance (HU-018) reemplazarán este almacenamiento sin cambiar
/// el contrato IContextoSolicitud.
/// </summary>
[SinFiltroDeOrganizacion("Se consulta por usuario para derivar el contexto antes de conocer la organización (HU-007).")]
public sealed class Membresia
{
    private Membresia()
    {
        Permisos = [];
        SedesAsignadas = [];
    }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public bool Activa { get; private set; }
    public List<string> Permisos { get; private set; }
    public bool AlcanceTotalSedes { get; private set; }
    public List<Guid> SedesAsignadas { get; private set; }

    public static Membresia Crear(
        Guid usuarioId,
        Guid organizacionId,
        IEnumerable<string> permisos,
        bool alcanceTotalSedes,
        IEnumerable<Guid> sedesAsignadas) => new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            OrganizacionId = organizacionId,
            Activa = true,
            Permisos = permisos.Distinct(StringComparer.Ordinal).ToList(),
            AlcanceTotalSedes = alcanceTotalSedes,
            SedesAsignadas = sedesAsignadas.Distinct().ToList(),
        };

    /// <summary>Compara claves de permiso exactas; un nombre de rol nunca concede nada.</summary>
    public bool TienePermiso(string permiso) => Permisos.Contains(permiso, StringComparer.Ordinal);

    public void Inactivar() => Activa = false;
}
