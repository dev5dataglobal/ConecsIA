using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.IntegrationTests.Infraestructura;

namespace ConecsIA.IntegrationTests;

/// <summary>
/// Recurso de cliente expuesto por la API. Cada historia que agregue uno lo registra en
/// <see cref="SuiteAislamientoTests.Recursos"/> y hereda las pruebas negativas Org A vs Org B (HU-005, CA-01).
/// </summary>
public sealed record RecursoDeOrganizacion(
    string Nombre,
    string Ruta,
    Func<Escenario, Guid> RecursoDeB,
    Func<object> CuerpoModificar,
    string? SufijoInactivar)
{
    public override string ToString() => Nombre;
}

/// <summary>
/// Suite parametrizada y obligatoria de aislamiento (HU-005): desde Org A, un recurso de Org B no
/// aparece al listar y responde 404 al leer, modificar o inactivar, sin alterarse.
/// </summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class SuiteAislamientoTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public static TheoryData<RecursoDeOrganizacion> Recursos() =>
    [
        new RecursoDeOrganizacion(
            "Sede",
            "/api/v1/sedes",
            e => e.SedeB1,
            () => new { nombre = "Robada", ciudad = "Lima", pais = "Perú", zonaHoraria = "America/Lima", version = 1u },
            "inactivar"),
        new RecursoDeOrganizacion(
            "Rol",
            "/api/v1/roles",
            e => e.RolB1,
            () => new { nombre = "Robado", permisos = Array.Empty<string>(), version = 1u },
            "inactivar"),
    ];

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    [Theory]
    [MemberData(nameof(Recursos))]
    public async Task Org_A_no_ve_el_recurso_de_Org_B_al_listar(RecursoDeOrganizacion recurso)
    {
        var cuerpo = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"{recurso.Ruta}?tamano=100");

        // Listados paginados ({ elementos }) o listas simples.
        var lista = cuerpo.ValueKind == JsonValueKind.Array ? cuerpo : cuerpo.GetProperty("elementos");
        var ids = lista.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToList();
        Assert.NotEmpty(ids);
        Assert.DoesNotContain(recurso.RecursoDeB(e), ids);
    }

    [Theory]
    [MemberData(nameof(Recursos))]
    public async Task Org_A_recibe_404_al_leer_modificar_o_inactivar_el_recurso_de_Org_B(RecursoDeOrganizacion recurso)
    {
        var cliente = fabrica.Como(e.AdminA);
        var ruta = $"{recurso.Ruta}/{recurso.RecursoDeB(e)}";
        var antes = await fabrica.Como(e.AdminB).GetStringAsync(ruta);

        var respuestas = new List<HttpResponseMessage>
        {
            await cliente.GetAsync(ruta),
            await cliente.PutAsJsonAsync(ruta, recurso.CuerpoModificar()),
        };
        if (recurso.SufijoInactivar is not null)
        {
            respuestas.Add(await cliente.PostAsync($"{ruta}/{recurso.SufijoInactivar}", null));
        }

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        foreach (var r in respuestas)
        {
            Assert.DoesNotContain(recurso.RecursoDeB(e).ToString(), await CuerpoSinInstanciaAsync(r));
        }
        Assert.Equal(antes, await fabrica.Como(e.AdminB).GetStringAsync(ruta));
    }

    /// <summary>
    /// Cuerpo Problem Details sin <c>instance</c>: ese campo solo repite la ruta que envió el cliente,
    /// el resto no debe revelar nada del recurso ajeno.
    /// </summary>
    internal static async Task<string> CuerpoSinInstanciaAsync(HttpResponseMessage respuesta)
    {
        var nodo = System.Text.Json.Nodes.JsonNode.Parse(await respuesta.Content.ReadAsStringAsync())!.AsObject();
        nodo.Remove("instance");
        return nodo.ToJsonString();
    }
}
