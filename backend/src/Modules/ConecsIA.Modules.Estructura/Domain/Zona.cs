using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Estructura.Domain;

/// <summary>En el contrato viaja como texto: "Entrada", "Salida" o "Ambos".</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ModoPuntoAcceso>))]
public enum ModoPuntoAcceso
{
    Entrada,
    Salida,
    Ambos,
}

/// <summary>
/// Zona de una sede (RF-05, HU-015, ADR-019 aprobado): sector físico que puede actuar como punto de
/// acceso con un modo. Modo solo si es punto de acceso (también un CHECK en PostgreSQL). Desmarcar o
/// inactivar no altera movimientos históricos (EP-10 referenciará la zona).
/// </summary>
public sealed partial class Zona : IEntidadDeOrganizacion
{
    public const string CodigoModoInconsistente = "ZONA_MODO_INCONSISTENTE";
    public const string CodigoYaInactiva = "ZONA_YA_INACTIVA";
    public const int LargoNombre = 150;
    public const int LargoDescripcion = 250;

    private Zona()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Guid SedeId { get; private set; }
    public string Nombre { get; private set; }
    public string NombreNormalizado { get; private set; }
    public string? Descripcion { get; private set; }
    public bool EsPuntoAcceso { get; private set; }
    public ModoPuntoAcceso? Modo { get; private set; }
    public EstadoSede Estado { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset ActualizadaEn { get; private set; }
    public DateTimeOffset? InactivadaEn { get; private set; }
    public uint Version { get; private set; }

    public static Zona Crear(Guid organizacionId, Guid sedeId, string nombre, string? descripcion, bool esPuntoAcceso, ModoPuntoAcceso? modo, DateTimeOffset ahora)
    {
        var zona = new Zona
        {
            Id = Guid.CreateVersion7(ahora),
            OrganizacionId = organizacionId,
            SedeId = sedeId,
            Estado = EstadoSede.Activa,
            CreadaEn = ahora,
        };
        zona.Modificar(nombre, descripcion, esPuntoAcceso, modo, ahora);
        return zona;
    }

    public void Modificar(string nombre, string? descripcion, bool esPuntoAcceso, ModoPuntoAcceso? modo, DateTimeOffset ahora)
    {
        if (esPuntoAcceso != modo.HasValue)
        {
            throw new ReglaNegocioException(CodigoModoInconsistente, "Un punto de acceso requiere modo y una zona que no lo es no lo admite.");
        }

        Nombre = Espacios().Replace(nombre.Trim(), " ");
        NombreNormalizado = NormalizarNombre(nombre);
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        EsPuntoAcceso = esPuntoAcceso;
        Modo = modo;
        ActualizadaEn = ahora;
    }

    public void Inactivar(DateTimeOffset ahora)
    {
        if (Estado == EstadoSede.Inactiva) throw new ReglaNegocioException(CodigoYaInactiva, "La zona ya está inactiva.");
        Estado = EstadoSede.Inactiva;
        InactivadaEn = ahora;
        ActualizadaEn = ahora;
    }

    /// <summary>¿Sirve hoy para registrar la operación indicada? (activa, punto y modo compatible).</summary>
    public bool Admite(ModoPuntoAcceso operacion) =>
        Estado == EstadoSede.Activa && EsPuntoAcceso && (Modo == ModoPuntoAcceso.Ambos || Modo == operacion);

    public static string NormalizarNombre(string nombre) => Espacios().Replace(nombre.Trim(), " ").ToLowerInvariant();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacios();
}
