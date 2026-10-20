using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests;

/// <summary>HU-012: vocabulario por organización sobre claves fijas del núcleo (RN-11).</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class VocabularioTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string Ruta = "/api/v1/vocabulario";
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    private static object Campus(uint version = 0) => new
    {
        terminos = new Dictionary<string, object> { ["sede"] = new { singular = "Campus", plural = "Campus" } },
        version,
    };

    [Fact]
    public async Task Sin_personalizar_devuelve_el_catalogo_sin_terminos()
    {
        var vocabulario = await fabrica.Como(e.OperadorA).GetFromJsonAsync<JsonElement>(Ruta);

        Assert.Empty(vocabulario.GetProperty("terminos").EnumerateObject());
        Assert.Contains("sede", vocabulario.GetProperty("claves").EnumerateArray().Select(c => c.GetString()));
    }

    [Fact]
    public async Task Un_termino_personalizado_se_ve_en_la_organizacion_y_no_en_otra()
    {
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PutAsJsonAsync(Ruta, Campus())).StatusCode);

        var deA = await fabrica.Como(e.OperadorA).GetFromJsonAsync<JsonElement>(Ruta);
        Assert.Equal("Campus", deA.GetProperty("terminos").GetProperty("sede").GetProperty("singular").GetString());
        var deB = await fabrica.Como(e.AdminB).GetFromJsonAsync<JsonElement>(Ruta);
        Assert.Empty(deB.GetProperty("terminos").EnumerateObject());

        // La API conserva sus nombres de núcleo (RN-11).
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).GetAsync("/api/v1/sedes")).StatusCode);
    }

    [Fact]
    public async Task Una_clave_fuera_del_catalogo_o_con_HTML_responde_400()
    {
        var desconocida = new { terminos = new Dictionary<string, object> { ["nave"] = new { singular = "Nave", plural = "Naves" } }, version = 0u };
        var conHtml = new { terminos = new Dictionary<string, object> { ["sede"] = new { singular = "<b>Campus</b>", plural = "Campus" } }, version = 0u };

        Assert.Equal(HttpStatusCode.BadRequest, (await fabrica.Como(e.AdminA).PutAsJsonAsync(Ruta, desconocida)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fabrica.Como(e.AdminA).PutAsJsonAsync(Ruta, conHtml)).StatusCode);
    }

    [Fact]
    public async Task Sin_permiso_de_configuracion_responde_403()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.OperadorA).PutAsJsonAsync(Ruta, Campus())).StatusCode);
    }

    [Fact]
    public async Task Una_version_obsoleta_responde_409_y_el_cambio_se_audita()
    {
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PutAsJsonAsync(Ruta, Campus())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fabrica.Como(e.AdminA).PutAsJsonAsync(Ruta, Campus())).StatusCode);

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var evento = await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .SingleAsync(a => a.OrganizacionId == e.OrgA && a.Accion == "vocabulario.modificar");
        Assert.Contains("Campus", evento.ValoresNuevos, StringComparison.Ordinal);
    }
}
