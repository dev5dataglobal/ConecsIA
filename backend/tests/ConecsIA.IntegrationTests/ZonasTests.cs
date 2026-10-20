using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ConecsIA.IntegrationTests;

/// <summary>HU-015 (ADR-019) y HU-018 por punto de acceso.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ZonasTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    private async Task<Guid> CrearZonaAsync(Guid sede, string nombre, bool punto, string? modo, Guid? como = null)
    {
        var respuesta = await fabrica.Como(como ?? e.AdminA).PostAsJsonAsync($"/api/v1/sedes/{sede}/zonas",
            new { nombre, esPuntoAcceso = punto, modo });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // ---------- HU-015 ----------

    [Fact]
    public async Task Para_operar_entradas_solo_se_ofrecen_puntos_de_entrada_o_ambos()
    {
        var entrada = await CrearZonaAsync(e.SedeA1, "Portería peatonal", true, "Entrada");
        var salida = await CrearZonaAsync(e.SedeA1, "Salida vehicular", true, "Salida");
        var ambos = await CrearZonaAsync(e.SedeA1, "Portería principal", true, "Ambos");
        var interna = await CrearZonaAsync(e.SedeA1, "Piso 3", false, null);

        var puntos = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/sedes/{e.SedeA1}/zonas?operacion=Entrada");
        var ids = puntos.EnumerateArray().Select(z => z.GetProperty("id").GetGuid()).ToList();

        Assert.Contains(entrada, ids);
        Assert.Contains(ambos, ids);
        Assert.DoesNotContain(salida, ids);
        Assert.DoesNotContain(interna, ids);
    }

    [Theory]
    [InlineData(false, "Entrada")]
    [InlineData(true, null)]
    public async Task Un_modo_inconsistente_con_el_punto_de_acceso_responde_400(bool punto, string? modo)
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync($"/api/v1/sedes/{e.SedeA1}/zonas",
            new { nombre = "Inconsistente", esPuntoAcceso = punto, modo });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task PostgreSQL_rechaza_un_modo_sin_punto_de_acceso()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO zona (id, organizacion_id, sede_id, nombre, nombre_normalizado, es_punto_acceso, modo, estado, creada_en, actualizada_en)
            VALUES (gen_random_uuid(), @org, @sede, 'X', 'x', false, 'Entrada', 'Activa', now(), now())
            """,
            conexion);
        comando.Parameters.AddWithValue("org", e.OrgA);
        comando.Parameters.AddWithValue("sede", e.SedeA1);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task El_nombre_es_unico_por_sede_y_una_sede_ajena_responde_404()
    {
        await CrearZonaAsync(e.SedeA1, "Lobby", false, null);

        Assert.Equal(HttpStatusCode.Conflict, (await fabrica.Como(e.AdminA).PostAsJsonAsync($"/api/v1/sedes/{e.SedeA1}/zonas",
            new { nombre = " LOBBY ", esPuntoAcceso = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).PostAsJsonAsync($"/api/v1/sedes/{e.SedeB1}/zonas",
            new { nombre = "Intrusa", esPuntoAcceso = false })).StatusCode);
    }

    [Fact]
    public async Task Una_zona_de_otra_organizacion_responde_404()
    {
        var zonaB = await CrearZonaAsync(e.SedeB1, "Zona B", false, null, e.AdminB);

        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).GetAsync($"/api/v1/zonas/{zonaB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).PostAsync($"/api/v1/zonas/{zonaB}/inactivar", null)).StatusCode);
    }

    [Fact]
    public async Task Inactivar_conserva_la_zona_y_audita_el_cambio_de_estado()
    {
        var zona = await CrearZonaAsync(e.SedeA1, "Torniquete", true, "Ambos");

        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PostAsync($"/api/v1/zonas/{zona}/inactivar", null)).StatusCode);

        var consultada = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/zonas/{zona}");
        Assert.Equal("Inactiva", consultada.GetProperty("estado").GetString());
        var operables = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/sedes/{e.SedeA1}/zonas?operacion=Entrada");
        Assert.DoesNotContain(zona, operables.EnumerateArray().Select(z => z.GetProperty("id").GetGuid()));
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        Assert.True(await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .AnyAsync(a => a.ObjetoId == zona && a.Accion == "zona.inactivar"));
    }

    [Fact]
    public async Task Modificar_puede_desmarcar_el_punto_de_acceso_con_version()
    {
        var zona = await CrearZonaAsync(e.SedeA1, "Garita", true, "Entrada");
        var actual = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/zonas/{zona}");

        var respuesta = await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/zonas/{zona}", new
        {
            nombre = "Garita", esPuntoAcceso = false, version = actual.GetProperty("version").GetUInt32(),
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.False((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("esPuntoAcceso").GetBoolean());
    }

    // ---------- HU-018 por punto de acceso ----------

    private Task<HttpResponseMessage> AlcanceAsync(Guid como, Guid usuario, bool total, Guid[] sedes, Guid[] puntos) =>
        fabrica.Como(como).PutAsJsonAsync($"/api/v1/usuarios/{usuario}/alcance", new { alcanceTotalSedes = total, sedes, puntos });

    [Fact]
    public async Task Un_operador_con_un_punto_asignado_no_opera_en_otro_punto_de_la_misma_sede()
    {
        var norte = await CrearZonaAsync(e.SedeA1, "Portería norte", true, "Ambos");
        var sur = await CrearZonaAsync(e.SedeA1, "Portería sur", true, "Ambos");
        Assert.Equal(HttpStatusCode.OK, (await AlcanceAsync(e.AdminA, e.OperadorA, false, [e.SedeA1], [norte])).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await fabrica.Como(e.OperadorA).PostAsync($"/api/v1/soporte-pruebas/operacion-en-punto/{norte}", null)).StatusCode);
        var rechazo = await fabrica.Como(e.OperadorA).PostAsync($"/api/v1/soporte-pruebas/operacion-en-punto/{sur}", null);
        Assert.Equal(HttpStatusCode.Forbidden, rechazo.StatusCode);
        Assert.Equal("FUERA_DE_ALCANCE", (await rechazo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var me = await fabrica.Como(e.OperadorA).GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.Equal([norte], me.GetProperty("puntosAsignados").EnumerateArray().Select(p => p.GetGuid()));
    }

    [Fact]
    public async Task Sin_puntos_asignados_opera_en_cualquier_punto_de_sus_sedes()
    {
        var punto = await CrearZonaAsync(e.SedeA1, "Recepción", true, "Entrada");
        var otraSede = await CrearZonaAsync(e.SedeA2, "Bodega", true, "Entrada");

        Assert.Equal(HttpStatusCode.NoContent, (await fabrica.Como(e.OperadorA).PostAsync($"/api/v1/soporte-pruebas/operacion-en-punto/{punto}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.OperadorA).PostAsync($"/api/v1/soporte-pruebas/operacion-en-punto/{otraSede}", null)).StatusCode);
    }

    [Fact]
    public async Task Un_punto_fuera_de_las_sedes_asignadas_o_que_no_es_punto_se_rechaza()
    {
        var enA2 = await CrearZonaAsync(e.SedeA2, "Portería A2", true, "Ambos");
        var interna = await CrearZonaAsync(e.SedeA1, "Oficina", false, null);
        var deB = await CrearZonaAsync(e.SedeB1, "Portería B", true, "Ambos", e.AdminB);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await AlcanceAsync(e.AdminA, e.OperadorA, false, [e.SedeA1], [enA2])).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await AlcanceAsync(e.AdminA, e.OperadorA, false, [e.SedeA1], [interna])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AlcanceAsync(e.AdminA, e.OperadorA, false, [e.SedeA1], [deB])).StatusCode);
    }

    [Fact]
    public async Task Un_administrador_con_puntos_asignados_solo_asigna_esos_puntos()
    {
        var norte = await CrearZonaAsync(e.SedeA1, "Norte", true, "Ambos");
        var sur = await CrearZonaAsync(e.SedeA1, "Sur", true, "Ambos");
        var supervisor = await UsuarioAsync([e.SedeA1], [norte], Permisos.UsuariosConsultar, Permisos.UsuariosAdministrar);

        Assert.Equal(HttpStatusCode.Forbidden, (await AlcanceAsync(supervisor, e.OperadorA, false, [e.SedeA1], [sur])).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AlcanceAsync(supervisor, e.OperadorA, false, [e.SedeA1], [])).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AlcanceAsync(supervisor, e.OperadorA, false, [e.SedeA1], [norte])).StatusCode);
    }

    private async Task<Guid> UsuarioAsync(Guid[] sedes, Guid[] puntos, params string[] permisos)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var usuario = Usuario.Crear($"punto.{Guid.NewGuid():N}@ejemplo.test", "Supervisor de punto");
        contexto.Add(usuario);
        var membresia = Membresia.Crear(usuario.Id, e.OrgA, permisos, alcanceTotalSedes: false, sedesAsignadas: sedes);
        membresia.AsignarAlcance(false, sedes, puntos);
        contexto.Add(membresia);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }
}
