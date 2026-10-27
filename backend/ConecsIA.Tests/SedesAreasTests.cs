using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Sedes y áreas. Escenario: organizaciones A y B; en A un administrador con el módulo «sedes» completo
/// (sin «configuracion») y un lector solo con consultar.
/// </summary>
public sealed class SedesAreasTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _ciudad;
    private string _sufijo = "";

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");
        var admin = await api.CrearRolAsync(_orgA, "Administrador de sedes");
        var lector = await api.CrearRolAsync(_orgA, "Lector de sedes");
        var adminB = await api.CrearRolAsync(_orgB, "Administrador B");
        await api.AsignarModuloAsync(_orgA, admin, "sedes", true, true, true);
        await api.AsignarModuloAsync(_orgA, lector, "sedes", true, false, false);
        await api.AsignarModuloAsync(_orgB, adminB, "sedes", true, true, true);
        await api.CrearUsuarioAsync(Correo("admin"), _orgA, admin);
        await api.CrearUsuarioAsync(Correo("lector"), _orgA, lector);
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, adminB);
        _ciudad = (await api.ScalarAsync<int?>(
            "INSERT INTO ciudad (nombre, departamento, pais, zona_horaria, activo) VALUES (@p0, 'Cundinamarca', 'Colombia', 'America/Bogota', true) RETURNING id_ciudad",
            $"Ciudad {_sufijo}"))!.Value;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<int> CrearSede(HttpClient cliente, string nombre)
    {
        var r = await cliente.PostAsJsonAsync("/api/sedes", new { idCiudad = _ciudad, nombre });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await Json(r)).GetProperty("idSede").GetInt32();
    }

    [Fact]
    public async Task El_catalogo_de_ciudades_esta_disponible_con_el_modulo_sedes_sin_configuracion()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/Ciudad")).StatusCode);
        var ciudades = await admin.GetFromJsonAsync<JsonElement>("/api/sedes/ciudades");
        Assert.Contains(ciudades.EnumerateArray(), c => c.GetProperty("idCiudad").GetInt32() == _ciudad
            && c.GetProperty("zonaHoraria").GetString() == "America/Bogota");
    }

    [Fact]
    public async Task El_catalogo_DIVIPOLA_queda_cargado_sin_codigos_repetidos()
    {
        Assert.Equal(1122, await api.ScalarAsync<int>("SELECT count(DISTINCT codigo_dane)::int FROM ciudad WHERE codigo_dane IS NOT NULL"));
        Assert.Equal("Medellín", await api.ScalarAsync<string>("SELECT nombre FROM ciudad WHERE codigo_dane = '05001'"));
        Assert.Equal("Bogotá, D.C.", await api.ScalarAsync<string>("SELECT departamento FROM ciudad WHERE codigo_dane = '11001'"));
    }

    [Fact]
    public async Task Crear_sede_toma_la_zona_horaria_de_la_ciudad_y_queda_aislada_por_organizacion()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var adminB = await api.ClienteAutenticadoAsync(Correo("adminb"));

        var id = await CrearSede(admin, "Sede principal");

        var sede = await admin.GetFromJsonAsync<JsonElement>($"/api/sedes/{id}");
        Assert.Equal("America/Bogota", sede.GetProperty("zonaHoraria").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/sedes/{id}")).StatusCode);
        Assert.DoesNotContain((await adminB.GetFromJsonAsync<JsonElement>("/api/sedes")).EnumerateArray(), s => s.GetProperty("idSede").GetInt32() == id);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/sedes", new { idCiudad = _ciudad, nombre = "Sede principal" })).StatusCode);
        // Otra organización puede usar el mismo nombre.
        await CrearSede(adminB, "Sede principal");
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE accion = 'sede.crear' AND id_registro = @p0", id.ToString()));
    }

    [Fact]
    public async Task Inactivar_y_activar_requiere_editar_y_se_audita()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var lector = await api.ClienteAutenticadoAsync(Correo("lector"));
        var id = await CrearSede(admin, "Sede norte");

        Assert.Equal(HttpStatusCode.OK, (await lector.GetAsync("/api/sedes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await lector.PostAsync($"/api/sedes/{id}/inactivar", null)).StatusCode);

        var inactiva = await Json(await admin.PostAsync($"/api/sedes/{id}/inactivar", null));
        Assert.False(inactiva.GetProperty("activo").GetBoolean());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsync($"/api/sedes/{id}/inactivar", null)).StatusCode);
        var activa = await Json(await admin.PostAsync($"/api/sedes/{id}/activar", null));
        Assert.True(activa.GetProperty("activo").GetBoolean());
        Assert.Equal(2, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE accion IN ('sede.inactivar', 'sede.activar') AND id_registro = @p0", id.ToString()));
    }

    [Fact]
    public async Task Areas_validan_sede_activa_nombre_unico_y_no_se_reactivan_con_la_sede_inactiva()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var adminB = await api.ClienteAutenticadoAsync(Correo("adminb"));
        var sede = await CrearSede(admin, "Sede sur");

        var r = await admin.PostAsJsonAsync("/api/areas", new { idSede = sede, nombre = "Recepción" });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var area = (await Json(r)).GetProperty("idArea").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/areas", new { idSede = sede, nombre = "Recepción" })).StatusCode);
        // La sede de otra organización no es visible: no se puede crear un área en ella.
        Assert.Equal(HttpStatusCode.BadRequest, (await adminB.PostAsJsonAsync("/api/areas", new { idSede = sede, nombre = "Intrusa" })).StatusCode);
        // Solo una sede con zona empresarial admite áreas comunes.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/areas", new { idSede = sede, nombre = "Común", esComunZonaEmpresarial = true })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/areas/{area}/inactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/sedes/{sede}/inactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsync($"/api/areas/{area}/activar", null)).StatusCode);
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE accion = 'area.crear' AND id_registro = @p0", area.ToString()));
    }
}
