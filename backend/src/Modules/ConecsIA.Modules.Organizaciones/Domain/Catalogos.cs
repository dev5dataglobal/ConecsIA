using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Organizaciones.Domain;

/// <summary>
/// Elemento de un catálogo de la organización (HU-010). Viene de una plantilla (<c>plantilla:{tipo}:v{n}</c>)
/// o es personalizado. Es una copia propia de la organización: editarlo o inactivarlo no afecta a
/// otras, y reaplicar una plantilla nunca lo modifica (CA-26). Inactivar conserva la referencia (CA-03).
/// </summary>
public sealed class ElementoCatalogo : IEntidadDeOrganizacion
{
    public const string OrigenPersonalizado = "personalizado";
    public const int LargoClave = 50;
    public const int LargoNombre = 100;

    private ElementoCatalogo()
    {
        Catalogo = string.Empty;
        Clave = string.Empty;
        Nombre = string.Empty;
        Origen = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public string Catalogo { get; private set; }
    public string Clave { get; private set; }
    public string Nombre { get; private set; }
    public string Origen { get; private set; }
    public bool Activo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    public static ElementoCatalogo Crear(Guid organizacionId, string catalogo, string clave, string nombre, string origen, DateTimeOffset ahora) => new()
    {
        Id = Guid.CreateVersion7(ahora),
        OrganizacionId = organizacionId,
        Catalogo = catalogo,
        Clave = clave.Trim().ToLowerInvariant(),
        Nombre = nombre.Trim(),
        Origen = origen,
        Activo = true,
        CreadoEn = ahora,
    };

    public void Renombrar(string nombre) => Nombre = nombre.Trim();

    public void Inactivar() => Activo = false;
}

public sealed record ElementoPlantilla(string Catalogo, string Clave, string Nombre);

/// <summary>
/// Plantilla básica por tipo de organización (HU-010). <b>Contenido propuesto</b>, pendiente de
/// validar por el PO (D-23). Los catálogos de persona y documento los aporta loop_personas (HU-020).
/// </summary>
public static class PlantillasOrganizacion
{
    public const int Version = 1;
    public const string TiposArea = "tipos-area";

    public static IReadOnlyList<string> Catalogos { get; } = [TiposArea];

    private static readonly Dictionary<string, (string Clave, string Nombre)[]> TiposAreaPorTipo = new()
    {
        ["empresa"] = [("oficina", "Oficina"), ("recepcion", "Recepción"), ("bodega", "Bodega")],
        ["conjunto-residencial"] = [("torre", "Torre"), ("apartamento", "Apartamento"), ("porteria", "Portería")],
        ["club"] = [("salon", "Salón"), ("cancha", "Cancha"), ("recepcion", "Recepción")],
        ["universidad"] = [("facultad", "Facultad"), ("edificio", "Edificio"), ("laboratorio", "Laboratorio")],
        ["coworking"] = [("sala", "Sala"), ("puesto", "Puesto de trabajo"), ("recepcion", "Recepción")],
        ["parque-industrial"] = [("planta", "Planta"), ("bodega", "Bodega"), ("porteria", "Portería")],
        ["otro"] = [("general", "Área general")],
    };

    public static string Origen(string tipo) => $"plantilla:{tipo}:v{Version}";

    public static IReadOnlyList<ElementoPlantilla> Elementos(string tipo) =>
        (TiposAreaPorTipo.GetValueOrDefault(tipo) ?? TiposAreaPorTipo["otro"])
        .Select(e => new ElementoPlantilla(TiposArea, e.Clave, e.Nombre)).ToList();
}
