using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Organizaciones.Domain;

public sealed record Termino(string Singular, string Plural);

/// <summary>
/// Vocabulario de una organización (HU-012, RN-11): etiquetas sobre un catálogo cerrado de claves del
/// núcleo. Solo cambia lo que ve la interfaz; la API, los permisos y los códigos no cambian.
/// Las etiquetas por defecto de cada vertical dependen de D-01.
/// </summary>
public sealed class VocabularioOrganizacion : IEntidadDeOrganizacion
{
    public const int LargoMaximo = 60;

    /// <summary>Claves del núcleo personalizables.</summary>
    public static IReadOnlyList<string> Claves { get; } =
        ["organizacion", "sede", "area", "zona", "puntoAcceso", "persona", "visita", "autorizacion"];

    private VocabularioOrganizacion()
    {
        Terminos = [];
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Dictionary<string, Termino> Terminos { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }
    public uint Version { get; private set; }

    public static VocabularioOrganizacion Crear(Guid organizacionId, DateTimeOffset ahora) => new()
    {
        Id = Guid.CreateVersion7(ahora),
        OrganizacionId = organizacionId,
        ActualizadoEn = ahora,
    };

    public void Reemplazar(IReadOnlyDictionary<string, Termino> terminos, DateTimeOffset ahora)
    {
        Terminos = terminos.ToDictionary(t => t.Key, t => new Termino(t.Value.Singular.Trim(), t.Value.Plural.Trim()));
        ActualizadoEn = ahora;
    }

    /// <summary>Texto plano: sin marcado ni caracteres de control (la SPA además escapa al renderizar).</summary>
    public static bool EsEtiquetaValida(string? valor) =>
        !string.IsNullOrWhiteSpace(valor)
        && valor.Trim().Length <= LargoMaximo
        && valor.All(c => !char.IsControl(c) && c is not ('<' or '>' or '&' or '"'));
}
