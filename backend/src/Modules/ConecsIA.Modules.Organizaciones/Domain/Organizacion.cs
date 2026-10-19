using System.Text.RegularExpressions;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Tiempo;

namespace ConecsIA.Modules.Organizaciones.Domain;

public enum EstadoOrganizacion
{
    Activa,
    Inactiva,
}

/// <summary>
/// Frontera de aislamiento (RN-01), administrada desde el plano SaaS (HU-009). Inactivar no borra
/// datos; mientras D-16 no defina sus efectos, una organización inactiva no admite operaciones
/// nuevas ni sesiones útiles de sus usuarios (el contexto no se resuelve).
/// </summary>
public sealed partial class Organizacion
{
    public const string CodigoYaInactiva = "ORGANIZACION_YA_INACTIVA";
    public const string CodigoYaActiva = "ORGANIZACION_YA_ACTIVA";
    public const string CodigoZonaHorariaInvalida = "ZONA_HORARIA_INVALIDA";
    public const int LargoNombre = 200;
    public const int LargoIdentificacion = 50;
    public const int LargoContacto = 200;

    private Organizacion()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
        Tipo = TiposOrganizacion.Otro;
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public string Tipo { get; private set; }
    public string? IdentificacionLegal { get; private set; }
    public string? ZonaHoraria { get; private set; }
    public string? ContactoAdministrativo { get; private set; }
    public EstadoOrganizacion Estado { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset ActualizadaEn { get; private set; }
    public DateTimeOffset? InactivadaEn { get; private set; }

    /// <summary>Token de concurrencia optimista (xmin de PostgreSQL).</summary>
    public uint Version { get; private set; }

    public static Organizacion Crear(string nombre, DateTimeOffset ahora) =>
        Crear(nombre, TiposOrganizacion.Otro, null, null, null, ahora);

    public static Organizacion Crear(
        string nombre, string tipo, string? identificacionLegal, string? zonaHoraria, string? contacto, DateTimeOffset ahora)
    {
        var organizacion = new Organizacion
        {
            Id = Guid.CreateVersion7(ahora),
            Estado = EstadoOrganizacion.Activa,
            CreadaEn = ahora,
        };
        organizacion.Modificar(nombre, tipo, identificacionLegal, zonaHoraria, contacto, ahora);
        return organizacion;
    }

    /// <summary>Solo para semillas sintéticas con identificadores estables.</summary>
    public static Organizacion CrearConId(Guid id, string nombre, DateTimeOffset ahora)
    {
        var organizacion = Crear(nombre, ahora);
        organizacion.Id = id;
        return organizacion;
    }

    public void Modificar(string nombre, string tipo, string? identificacionLegal, string? zonaHoraria, string? contacto, DateTimeOffset ahora)
    {
        if (!TiposOrganizacion.Existe(tipo))
        {
            throw new ReglaNegocioException(TiposOrganizacion.CodigoTipoDesconocido, "El tipo de organización no existe.");
        }

        if (!string.IsNullOrWhiteSpace(zonaHoraria) && !ZonaHorariaIana.EsValida(zonaHoraria))
        {
            throw new ReglaNegocioException(CodigoZonaHorariaInvalida, "La zona horaria no es un identificador IANA válido.");
        }

        Nombre = Espacios().Replace(nombre.Trim(), " ");
        NombreNormalizado = NormalizarNombre(nombre);
        Tipo = tipo;
        IdentificacionLegal = Limpiar(identificacionLegal);
        ZonaHoraria = Limpiar(zonaHoraria);
        ContactoAdministrativo = Limpiar(contacto);
        ActualizadaEn = ahora;
    }

    /// <summary>Información general que puede editar el administrador de la organización (sin tipo ni estado).</summary>
    public void ModificarInformacionGeneral(string nombre, string? identificacionLegal, string? zonaHoraria, string? contacto, DateTimeOffset ahora) =>
        Modificar(nombre, Tipo, identificacionLegal, zonaHoraria, contacto, ahora);

    public void Inactivar(DateTimeOffset ahora)
    {
        if (Estado == EstadoOrganizacion.Inactiva) throw new ReglaNegocioException(CodigoYaInactiva, "La organización ya está inactiva.");

        Estado = EstadoOrganizacion.Inactiva;
        InactivadaEn = ahora;
        ActualizadaEn = ahora;
    }

    public void Reactivar(DateTimeOffset ahora)
    {
        if (Estado == EstadoOrganizacion.Activa) throw new ReglaNegocioException(CodigoYaActiva, "La organización ya está activa.");

        Estado = EstadoOrganizacion.Activa;
        InactivadaEn = null;
        ActualizadaEn = ahora;
    }

    /// <summary>Unicidad global (propuesta): sin espacios sobrantes y en minúsculas. Conserva tildes.</summary>
    public static string NormalizarNombre(string nombre) => Espacios().Replace(nombre.Trim(), " ").ToLowerInvariant();

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}

public sealed record TipoOrganizacion(string Clave, string Nombre);

/// <summary>
/// Tipos de organización (HU-010, PRD §1). Las etiquetas sectoriales definitivas dependen de D-01;
/// el núcleo no cambia por tipo.
/// </summary>
public static class TiposOrganizacion
{
    public const string CodigoTipoDesconocido = "TIPO_ORGANIZACION_DESCONOCIDO";
    public const string Otro = "otro";

    public static IReadOnlyList<TipoOrganizacion> Todos { get; } =
    [
        new("empresa", "Empresa"),
        new("conjunto-residencial", "Conjunto residencial"),
        new("club", "Club"),
        new("universidad", "Universidad"),
        new("coworking", "Coworking"),
        new("parque-industrial", "Parque industrial"),
        new(Otro, "Otro"),
    ];

    public static bool Existe(string? clave) => Todos.Any(t => t.Clave == clave);
}
