using System.Text.RegularExpressions;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Identidad.Domain;

/// <summary>
/// Rol configurable de una organización (HU-017, ADR-005): agrupa claves del catálogo cerrado. El
/// nombre es solo una etiqueta; nunca se evalúa para conceder acceso (RN-10). Inactivar conserva el
/// historial y deja de aportar permisos a las membresías que lo tengan.
/// </summary>
public sealed partial class Rol : IEntidadDeOrganizacion
{
    public const string CodigoPermisoDesconocido = "PERMISO_DESCONOCIDO";
    public const string CodigoYaInactivo = "ROL_YA_INACTIVO";
    public const int LargoNombre = 100;
    public const int LargoDescripcion = 250;

    private Rol()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
        Permisos = [];
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public string? Descripcion { get; private set; }
    public List<string> Permisos { get; private set; }

    /// <summary>Plantilla de la que se copió (solo informativo: la copia es independiente).</summary>
    public string? Plantilla { get; private set; }

    public bool Activo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }
    public DateTimeOffset? InactivadoEn { get; private set; }

    /// <summary>Token de concurrencia optimista (xmin de PostgreSQL).</summary>
    public uint Version { get; private set; }

    public static Rol Crear(
        Guid organizacionId, string nombre, string? descripcion, IEnumerable<string> permisos, string? plantilla, DateTimeOffset ahora)
    {
        var rol = new Rol
        {
            Id = Guid.CreateVersion7(ahora),
            OrganizacionId = organizacionId,
            Plantilla = plantilla,
            Activo = true,
            CreadoEn = ahora,
        };
        rol.Asignar(nombre, descripcion, permisos, ahora);
        return rol;
    }

    public void Modificar(string nombre, string? descripcion, IEnumerable<string> permisos, DateTimeOffset ahora) =>
        Asignar(nombre, descripcion, permisos, ahora);

    public void Inactivar(DateTimeOffset ahora)
    {
        if (!Activo) throw new ReglaNegocioException(CodigoYaInactivo, "El rol ya está inactivo.");

        Activo = false;
        InactivadoEn = ahora;
        ActualizadoEn = ahora;
    }

    public bool Concede(string permiso) => Activo && Permisos.Contains(permiso, StringComparer.Ordinal);

    public static string NormalizarNombre(string nombre) => Espacios().Replace(nombre.Trim(), " ").ToLowerInvariant();

    private void Asignar(string nombre, string? descripcion, IEnumerable<string> permisos, DateTimeOffset ahora)
    {
        var claves = permisos.Distinct(StringComparer.Ordinal).ToList();
        var desconocido = claves.FirstOrDefault(p => !CatalogoPermisos.Existe(p));
        if (desconocido is not null)
        {
            throw new ReglaNegocioException(CodigoPermisoDesconocido, $"El permiso «{desconocido}» no existe en el catálogo.");
        }

        Nombre = Espacios().Replace(nombre.Trim(), " ");
        NombreNormalizado = NormalizarNombre(nombre);
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        Permisos = claves.Order(StringComparer.Ordinal).ToList();
        ActualizadoEn = ahora;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}

/// <summary>Perfil inicial copiable (HU-017, PRD §5.1). El Superadministrador SaaS es del plano SaaS y no está aquí.</summary>
public sealed record PlantillaRol(string Clave, string Nombre, string Descripcion, IReadOnlyList<string> Permisos);

/// <summary>
/// Plantillas propuestas (no aprobadas por el PO): se copian como roles editables. El Operador de
/// acceso no recibe permisos de decisión (PRD §6.2). Se amplían cuando cada módulo agregue sus claves.
/// </summary>
public static class PlantillasRol
{
    public static IReadOnlyList<PlantillaRol> Todas { get; } =
    [
        new("administrador-organizacion", "Administrador de organización", "Administra toda la organización.", Permisos.Todos),
        new("administrador-sede", "Administrador de sede", "Administra las sedes asignadas.",
            [Permisos.SedesConsultar, Permisos.SedesModificar,
             Permisos.AreasConsultar, Permisos.AreasCrear, Permisos.AreasModificar, Permisos.AreasInactivar, Permisos.AreasAsignar]),
        new("operador-acceso", "Operador de acceso", "Registra entradas y salidas en sus sedes.", [Permisos.SedesConsultar]),
        new("autorizador", "Autorizador", "Decide solicitudes de ingreso de sus destinos.", [Permisos.SedesConsultar]),
        new("auditor", "Auditor", "Consulta sin modificar.", [Permisos.SedesConsultar, Permisos.RolesConsultar]),
    ];
}
