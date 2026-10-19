using ConecsIA.IntegrationTests.Soporte;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests.Infraestructura;

/// <summary>
/// API real contra PostgreSQL real. Agrega un controlador de soporte solo para pruebas, que
/// permite provocar cada tipo de error sin crear endpoints de prueba en la aplicación.
/// </summary>
public sealed class FabricaApi(BaseDatosFixture baseDatos) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ConecsIA", baseDatos.CadenaApp);
        builder.UseSetting("ConnectionStrings:ConecsIAMigraciones", baseDatos.CadenaMigrador);
        builder.UseSetting("Persistencia:AplicarMigracionesAlIniciar", "true");
        builder.UseSetting("Autenticacion:Desarrollo:Habilitada", "true");

        builder.ConfigureServices(servicios =>
        {
            servicios.AddControllers().AddApplicationPart(typeof(SoportePruebasController).Assembly);
            servicios.AddScoped<IValidator<SoportePruebasController.SolicitudEjemplo>, SoportePruebasController.ValidadorEjemplo>();
        });
    }
}
