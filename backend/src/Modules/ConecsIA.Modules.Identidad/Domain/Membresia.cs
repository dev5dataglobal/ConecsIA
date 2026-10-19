using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Identidad.Domain;

/// <summary>
/// Vínculo usuario–organización con sus permisos efectivos y su alcance por sede (ADR-005).
/// Permisos efectivos = roles activos asignados (HU-017) ∪ claves directas. Las claves directas son
/// transitorias (semillas y pruebas previas a HU-017); la administración de usuarios (HU-016) asigna
/// roles. La administración del alcance llega con HU-018.
/// </summary>
[SinFiltroDeOrganizacion("Se consulta por usuario para derivar el contexto antes de conocer la organización (HU-007).")]
public sealed class Membresia
{
    private Membresia()
    {
        Permisos = [];
        Roles = [];
        SedesAsignadas = [];
    }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public bool Activa { get; private set; }
    public List<string> Permisos { get; private set; }
    public List<Guid> Roles { get; private set; }
    public bool AlcanceTotalSedes { get; private set; }
    public List<Guid> SedesAsignadas { get; private set; }

    public static Membresia Crear(
        Guid usuarioId,
        Guid organizacionId,
        IEnumerable<string> permisos,
        bool alcanceTotalSedes,
        IEnumerable<Guid> sedesAsignadas,
        IEnumerable<Guid>? roles = null) => new()
        {
            Roles = (roles ?? []).Distinct().ToList(),
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

    /// <summary>Reemplaza los roles asignados (HU-016/HU-017). El servicio verifica que sean de la organización.</summary>
    public void AsignarRoles(IEnumerable<Guid> roles) => Roles = roles.Distinct().ToList();

    public void Inactivar() => Activa = false;
}
