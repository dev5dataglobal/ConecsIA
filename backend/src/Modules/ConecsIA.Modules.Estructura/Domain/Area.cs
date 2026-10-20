using System.Text.RegularExpressions;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Estructura.Domain;

public enum FuncionEnArea
{
    Responsable,
    Autorizador,
}

/// <summary>
/// Área o destino de una sede (RF-04, HU-014): unidad receptora de visitas. Su sede pertenece a la
/// misma organización (FK compuesta). Inactivar conserva área y asignaciones para el historial (CA-03).
/// Ser autorizador de un área no concede el permiso de decidir (RN-10); eso lo define D-04.
/// </summary>
public sealed partial class Area : IEntidadDeOrganizacion
{
    public const string CodigoYaInactiva = "AREA_YA_INACTIVA";
    public const int LargoNombre = 150;
    public const int LargoTipo = 50;

    private readonly List<AsignacionArea> asignaciones = [];

    private Area()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Guid SedeId { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public string? Tipo { get; private set; }
    public EstadoSede Estado { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset ActualizadaEn { get; private set; }
    public DateTimeOffset? InactivadaEn { get; private set; }
    public uint Version { get; private set; }

    public IReadOnlyList<AsignacionArea> Asignaciones => asignaciones;

    public static Area Crear(Guid organizacionId, Guid sedeId, string nombre, string? tipo, DateTimeOffset ahora)
    {
        var area = new Area
        {
            Id = Guid.CreateVersion7(ahora),
            OrganizacionId = organizacionId,
            SedeId = sedeId,
            Estado = EstadoSede.Activa,
            CreadaEn = ahora,
        };
        area.Modificar(nombre, tipo, ahora);
        return area;
    }

    public void Modificar(string nombre, string? tipo, DateTimeOffset ahora)
    {
        Nombre = Espacios().Replace(nombre.Trim(), " ");
        NombreNormalizado = NormalizarNombre(nombre);
        Tipo = string.IsNullOrWhiteSpace(tipo) ? null : tipo.Trim();
        ActualizadaEn = ahora;
    }

    public void Inactivar(DateTimeOffset ahora)
    {
        if (Estado == EstadoSede.Inactiva) throw new ReglaNegocioException(CodigoYaInactiva, "El área ya está inactiva.");
        Estado = EstadoSede.Inactiva;
        InactivadaEn = ahora;
        ActualizadaEn = ahora;
    }

    /// <summary>
    /// Deja vigentes exactamente <paramref name="usuarios"/> en la función: retira (con fecha) a los que
    /// sobran y agrega a los nuevos. Nunca borra: el historial conserva quién fue y cuándo.
    /// </summary>
    public (IReadOnlyList<Guid> Agregados, IReadOnlyList<Guid> Retirados) AsignarVigentes(
        FuncionEnArea funcion, IReadOnlyCollection<Guid> usuarios, Guid? actor, DateTimeOffset ahora)
    {
        var vigentes = asignaciones.Where(a => a.Funcion == funcion && a.HastaEn is null).ToList();
        var retirados = new List<Guid>();
        foreach (var asignacion in vigentes.Where(a => !usuarios.Contains(a.UsuarioId)))
        {
            asignacion.Retirar(actor, ahora);
            retirados.Add(asignacion.UsuarioId);
        }

        var agregados = usuarios.Distinct().Where(u => vigentes.All(a => a.UsuarioId != u)).ToList();
        foreach (var usuario in agregados)
        {
            asignaciones.Add(AsignacionArea.Crear(OrganizacionId, Id, usuario, funcion, actor, ahora));
        }

        ActualizadaEn = ahora;
        return (agregados, retirados);
    }

    public static string NormalizarNombre(string nombre) => Espacios().Replace(nombre.Trim(), " ").ToLowerInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}

/// <summary>Responsable o autorizador de un área con vigencia (alta y baja) para conservar historial.</summary>
public sealed class AsignacionArea : IEntidadDeOrganizacion
{
    private AsignacionArea()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Guid AreaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public FuncionEnArea Funcion { get; private set; }
    public DateTimeOffset DesdeEn { get; private set; }
    public Guid? AsignadaPor { get; private set; }
    public DateTimeOffset? HastaEn { get; private set; }
    public Guid? RetiradaPor { get; private set; }

    internal static AsignacionArea Crear(Guid organizacionId, Guid areaId, Guid usuarioId, FuncionEnArea funcion, Guid? actor, DateTimeOffset ahora) => new()
    {
        Id = Guid.CreateVersion7(ahora),
        OrganizacionId = organizacionId,
        AreaId = areaId,
        UsuarioId = usuarioId,
        Funcion = funcion,
        DesdeEn = ahora,
        AsignadaPor = actor,
    };

    internal void Retirar(Guid? actor, DateTimeOffset ahora)
    {
        HastaEn = ahora;
        RetiradaPor = actor;
    }
}
