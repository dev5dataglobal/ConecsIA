using ConecsIA.Api.Seguridad;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ConecsIA.UnitTests.Seguridad;

/// <summary>La identidad por cabecera es solo un sustituto local de HU-006: nunca puede llegar a producción.</summary>
public class AutenticacionDesarrolloTests
{
    private static IConfiguration Configuracion(bool habilitada) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [AutenticacionDesarrollo.ClaveHabilitada] = habilitada.ToString() })
            .Build();

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Habilitada_fuera_de_Development_o_Testing_detiene_el_arranque(string entorno)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => AutenticacionDesarrollo.ValidarEntorno(Configuracion(habilitada: true), new Entorno(entorno)));

        Assert.Contains(entorno, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Habilitada_en_Development_o_Testing_se_permite(string entorno)
    {
        AutenticacionDesarrollo.ValidarEntorno(Configuracion(habilitada: true), new Entorno(entorno));
    }

    [Fact]
    public void Deshabilitada_se_permite_en_produccion()
    {
        AutenticacionDesarrollo.ValidarEntorno(Configuracion(habilitada: false), new Entorno("Production"));
    }

    [Theory]
    [InlineData("Production", Microsoft.AspNetCore.Http.CookieSecurePolicy.Always)]
    [InlineData("Staging", Microsoft.AspNetCore.Http.CookieSecurePolicy.Always)]
    [InlineData("Testing", Microsoft.AspNetCore.Http.CookieSecurePolicy.Always)]
    [InlineData("Development", Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest)]
    public void Las_cookies_de_sesion_y_csrf_son_Secure_fuera_de_Development(
        string entorno, Microsoft.AspNetCore.Http.CookieSecurePolicy esperada)
    {
        Assert.Equal(esperada, AutenticacionCookie.PoliticaSecure(new Entorno(entorno)));
    }

    private sealed class Entorno(string nombre) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nombre;
        public string ApplicationName { get; set; } = "Pruebas";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
