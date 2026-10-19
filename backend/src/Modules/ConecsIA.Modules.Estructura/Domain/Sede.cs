using System.Text.RegularExpressions;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Estructura.Domain;

public enum EstadoSede
{
    Activa,
    Inactiva,
}

/// <summary>
/// Instalación física de una organización (RF-03, HU-013). La zona horaria IANA es obligatoria
/// porque los horarios de autorización se evalúan en hora local de la sede (RN-08, ADR-008).
/// Inactivar no borra: conserva las referencias históricas (CA-03).
/// </summary>
public sealed partial class Sede : IEntidadDeOrganizacion
{
    public const string CodigoZonaHorariaInvalida = "ZONA_HORARIA_INVALIDA";
    public const string CodigoYaInactiva = "SEDE_YA_INACTIVA";
    public const int LargoNombre = 150;
    public const int LargoDireccion = 250;
    public const int LargoCiudadPais = 100;

    private Sede()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
        ZonaHoraria = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public string? Direccion { get; private set; }
    public string? Ciudad { get; private set; }
    public string? Pais { get; private set; }
    public string ZonaHoraria { get; private set; }
    public EstadoSede Estado { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset ActualizadaEn { get; private set; }
    public DateTimeOffset? InactivadaEn { get; private set; }

    /// <summary>Token de concurrencia optimista (xmin de PostgreSQL).</summary>
    public uint Version { get; private set; }

    public static Sede Crear(
        Guid organizacionId,
        string nombre,
        string? direccion,
        string? ciudad,
        string? pais,
        string zonaHoraria,
        DateTimeOffset ahora)
    {
        var sede = new Sede
        {
            Id = Guid.CreateVersion7(ahora),
            OrganizacionId = organizacionId,
            Estado = EstadoSede.Activa,
            CreadaEn = ahora,
        };
        sede.Asignar(nombre, direccion, ciudad, pais, zonaHoraria, ahora);
        return sede;
    }

    /// <summary>
    /// Cambiar la zona horaria no recalcula nada todavía porque no existen autorizaciones; cuando
    /// existan, D-06 debe definir si se rechaza o se advierte (HU-013).
    /// </summary>
    public void Modificar(string nombre, string? direccion, string? ciudad, string? pais, string zonaHoraria, DateTimeOffset ahora) =>
        Asignar(nombre, direccion, ciudad, pais, zonaHoraria, ahora);

    /// <summary>
    /// La regla con personas dentro (permanencias abiertas) queda pendiente hasta que exista el
    /// módulo de Accesos (HU-013, propuesta: rechazar).
    /// </summary>
    public void Inactivar(DateTimeOffset ahora)
    {
        if (Estado == EstadoSede.Inactiva)
        {
            throw new ReglaNegocioException(CodigoYaInactiva, "La sede ya está inactiva.");
        }

        Estado = EstadoSede.Inactiva;
        InactivadaEn = ahora;
        ActualizadaEn = ahora;
    }

    /// <summary>Clave de unicidad por organización: sin espacios sobrantes y en minúsculas. Conserva tildes.</summary>
    public static string NormalizarNombre(string nombre) =>
        Espacios().Replace(nombre.Trim(), " ").ToLowerInvariant();

    private void Asignar(string nombre, string? direccion, string? ciudad, string? pais, string zonaHoraria, DateTimeOffset ahora)
    {
        if (!Domain.ZonaHoraria.EsIanaValida(zonaHoraria))
        {
            throw new ReglaNegocioException(CodigoZonaHorariaInvalida, "La zona horaria no es un identificador IANA válido.");
        }

        Nombre = Espacios().Replace(nombre.Trim(), " ");
        NombreNormalizado = NormalizarNombre(nombre);
        Direccion = Limpiar(direccion);
        Ciudad = Limpiar(ciudad);
        Pais = Limpiar(pais);
        ZonaHoraria = zonaHoraria;
        ActualizadaEn = ahora;
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}

public static class ZonaHoraria
{
    /// <summary>
    /// Acepta solo identificadores IANA conocidos por la base de zonas del sistema (ICU/tzdata).
    /// Rechaza identificadores de Windows y desfases fijos. NodaTime (ADR-008) sigue en Propuesta.
    /// </summary>
    public static bool EsIanaValida(string? zona) =>
        !string.IsNullOrWhiteSpace(zona)
        && zona == zona.Trim()
        && TimeZoneInfo.TryFindSystemTimeZoneById(zona, out _)
        && TimeZoneInfo.TryConvertIanaIdToWindowsId(zona, out _);
}
