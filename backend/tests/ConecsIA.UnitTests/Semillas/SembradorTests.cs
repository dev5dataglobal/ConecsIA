using ConecsIA.Api.Semillas;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ConecsIA.UnitTests.Semillas;

public class SembradorTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Rechaza_sembrar_fuera_de_Development(string entorno)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Sembrador.ValidarEntorno(new EntornoFalso(entorno)));

        Assert.Contains(entorno, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Permite_sembrar_en_Development()
    {
        Sembrador.ValidarEntorno(new EntornoFalso(Environments.Development));
    }

    [Fact]
    public void Detecta_el_argumento_de_sembrado()
    {
        Assert.True(Sembrador.FueSolicitado(["--sembrar"]));
        Assert.False(Sembrador.FueSolicitado(["--otro"]));
    }

    private sealed class EntornoFalso(string nombre) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nombre;
        public string ApplicationName { get; set; } = "Pruebas";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
