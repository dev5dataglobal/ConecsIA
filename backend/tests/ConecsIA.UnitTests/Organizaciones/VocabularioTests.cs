using ConecsIA.Modules.Organizaciones.Domain;

namespace ConecsIA.UnitTests.Organizaciones;

/// <summary>HU-012: etiquetas de texto plano, acotadas, sobre claves fijas.</summary>
public class VocabularioTests
{
    [Theory]
    [InlineData("Campus", true)]
    [InlineData("Apartamento o local", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("<script>", false)]
    [InlineData("Torre & bodega", false)]
    [InlineData("con\nsalto", false)]
    public void Valida_las_etiquetas(string valor, bool esperado)
    {
        Assert.Equal(esperado, VocabularioOrganizacion.EsEtiquetaValida(valor));
    }

    [Fact]
    public void Rechaza_etiquetas_demasiado_largas()
    {
        Assert.False(VocabularioOrganizacion.EsEtiquetaValida(new string('a', VocabularioOrganizacion.LargoMaximo + 1)));
    }

    [Fact]
    public void Reemplazar_recorta_espacios()
    {
        var vocabulario = VocabularioOrganizacion.Crear(Guid.NewGuid(), DateTimeOffset.UtcNow);

        vocabulario.Reemplazar(new Dictionary<string, Termino> { ["sede"] = new(" Campus ", " Campus ") }, DateTimeOffset.UtcNow);

        Assert.Equal(new Termino("Campus", "Campus"), vocabulario.Terminos["sede"]);
    }
}
