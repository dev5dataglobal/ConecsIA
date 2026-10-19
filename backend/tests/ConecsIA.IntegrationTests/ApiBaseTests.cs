using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.IntegrationTests.Infraestructura;

namespace ConecsIA.IntegrationTests;

[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ApiBaseTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;
    private HttpClient cliente = null!;

    public Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        cliente = fabrica.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        cliente.Dispose();
        await fabrica.DisposeAsync();
    }

    [Fact]
    public async Task Salud_de_vida_responde_sin_consultar_dependencias()
    {
        var respuesta = await cliente.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", cuerpo.GetProperty("status").GetString());
        Assert.Empty(cuerpo.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task Salud_preparada_verifica_postgresql_real()
    {
        var respuesta = await cliente.GetAsync("/api/v1/health/ready");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var check = Assert.Single(cuerpo.GetProperty("checks").EnumerateArray());
        Assert.Equal("postgresql", check.GetProperty("name").GetString());
        Assert.Equal("Healthy", check.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Version_lista_los_modulos_compuestos()
    {
        var cuerpo = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/sistema/version");

        var modulos = cuerpo.GetProperty("modulos").EnumerateArray().Select(m => m.GetString()).ToList();
        Assert.Equal(7, modulos.Count);
        Assert.Contains("Accesos", modulos);
    }

    [Fact]
    public async Task Error_de_validacion_responde_400_con_errores_por_campo_y_traceId()
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/soporte-pruebas/validacion", new { nombre = "", cantidad = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var errores = cuerpo.GetProperty("errors");
        Assert.Equal("El nombre es obligatorio.", errores.GetProperty("nombre")[0].GetString());
        Assert.Equal("La cantidad debe ser mayor que cero.", errores.GetProperty("cantidad")[0].GetString());
        Assert.False(string.IsNullOrEmpty(cuerpo.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Solicitud_valida_pasa_el_filtro_de_validacion()
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/soporte-pruebas/validacion", new { nombre = "Ana", cantidad = 1 });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Regla_de_negocio_responde_422_con_codigo()
    {
        var respuesta = await cliente.GetAsync("/api/v1/soporte-pruebas/regla");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SEDE_INACTIVA", cuerpo.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Recurso_no_encontrado_responde_404_problem_details()
    {
        var respuesta = await cliente.GetAsync("/api/v1/soporte-pruebas/no-encontrado");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Ruta_inexistente_responde_404_problem_details()
    {
        var respuesta = await cliente.GetAsync("/api/v1/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Excepcion_no_controlada_responde_500_sin_detalles_internos_y_con_traceId()
    {
        var respuesta = await cliente.GetAsync("/api/v1/soporte-pruebas/no-controlada");
        var texto = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.DoesNotContain("SELECT", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1023456789", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", texto, StringComparison.Ordinal);

        using var cuerpo = JsonDocument.Parse(texto);
        var traceId = cuerpo.RootElement.GetProperty("traceId").GetString();
        Assert.False(string.IsNullOrEmpty(traceId));
        Assert.Equal(traceId, respuesta.Headers.GetValues("X-Trace-Id").Single());
    }
}
