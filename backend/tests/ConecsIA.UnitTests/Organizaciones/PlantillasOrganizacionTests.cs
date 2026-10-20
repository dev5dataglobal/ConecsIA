using ConecsIA.Modules.Organizaciones.Domain;

namespace ConecsIA.UnitTests.Organizaciones;

/// <summary>HU-010: cada tipo tiene plantilla, con claves únicas y origen versionado.</summary>
public class PlantillasOrganizacionTests
{
    [Fact]
    public void Todo_tipo_de_organizacion_tiene_elementos_con_claves_unicas()
    {
        Assert.All(TiposOrganizacion.Todos, tipo =>
        {
            var elementos = PlantillasOrganizacion.Elementos(tipo.Clave);
            Assert.NotEmpty(elementos);
            Assert.Equal(elementos.Count, elementos.Select(e => (e.Catalogo, e.Clave)).Distinct().Count());
            Assert.All(elementos, e => Assert.Contains(e.Catalogo, PlantillasOrganizacion.Catalogos));
        });
    }

    [Fact]
    public void El_origen_incluye_tipo_y_version()
    {
        Assert.Equal($"plantilla:club:v{PlantillasOrganizacion.Version}", PlantillasOrganizacion.Origen("club"));
    }
}
