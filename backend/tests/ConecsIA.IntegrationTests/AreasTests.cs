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

/// <summary>HU-014: áreas o destinos por sede con responsables y autorizadores con vigencia.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class AreasTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    private async Task<JsonElement> CrearAreaAsync(Guid? sede = null, string nombre = "Torre 1", Guid? como = null)
    {
        var respuesta = await fabrica.Como(como ?? e.AdminA).PostAsJsonAsync($"/api/v1/sedes/{sede ?? e.SedeA1}/areas", new { nombre, tipo = "Torre" });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Guid Id(JsonElement area) => area.GetProperty("id").GetGuid();

    [Fact]
    public async Task Crear_un_area_responde_201_queda_en_su_sede_y_se_audita()
    {
        var area = await CrearAreaAsync();

        var lista = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/sedes/{e.SedeA1}/areas");
        Assert.Contains(Id(area), lista.EnumerateArray().Select(a => a.GetProperty("id").GetGuid()));
        Assert.Equal(e.SedeA1, area.GetProperty("sedeId").GetGuid());
        await EventoAsync(Id(area), "area.crear");
    }

    [Fact]
    public async Task El_nombre_es_unico_por_sede_pero_se_repite_en_otra_sede()
    {
        await CrearAreaAsync(e.SedeA1, "Recepción");

        var repetida = await fabrica.Como(e.AdminA).PostAsJsonAsync($"/api/v1/sedes/{e.SedeA1}/areas", new { nombre = "  RECEPCIÓN " });
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        await CrearAreaAsync(e.SedeA2, "Recepción");
    }

    [Fact]
    public async Task Una_sede_de_otra_organizacion_responde_404()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync($"/api/v1/sedes/{e.SedeB1}/areas", new { nombre = "Intrusa" });
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).GetAsync($"/api/v1/sedes/{e.SedeB1}/areas")).StatusCode);
    }

    [Fact]
    public async Task Un_area_de_otra_organizacion_responde_404_al_leer_modificar_inactivar_o_asignar()
    {
        var areaB = await CrearAreaAsync(e.SedeB1, "Área B", e.AdminB);
        var cliente = fabrica.Como(e.AdminA);

        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/v1/areas/{Id(areaB)}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsJsonAsync($"/api/v1/areas/{Id(areaB)}", new { nombre = "X", version = 1u })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync($"/api/v1/areas/{Id(areaB)}/inactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await cliente.PutAsJsonAsync($"/api/v1/areas/{Id(areaB)}/autorizadores", new { usuarios = new[] { e.OperadorA } })).StatusCode);
    }

    [Fact]
    public async Task PostgreSQL_rechaza_un_area_que_apunta_a_una_sede_de_otra_organizacion()
    {
        await using var conexion = new NpgsqlConnection(baseDatos.CadenaApp);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(
            """
            INSERT INTO area (id, organizacion_id, sede_id, nombre, nombre_normalizado, estado, creada_en, actualizada_en)
            VALUES (gen_random_uuid(), @a, @sedeB, 'Cruzada', 'cruzada', 'Activa', now(), now())
            """,
            conexion);
        comando.Parameters.AddWithValue("a", e.OrgA);
        comando.Parameters.AddWithValue("sedeB", e.SedeB1);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
    }

    [Fact]
    public async Task Un_usuario_de_otra_organizacion_no_puede_ser_autorizador()
    {
        var area = await CrearAreaAsync();

        var respuesta = await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/areas/{Id(area)}/autorizadores", new { usuarios = new[] { e.AdminB } });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_usuario_inactivo_no_se_puede_asignar()
    {
        var area = await CrearAreaAsync();

        var respuesta = await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/areas/{Id(area)}/responsables", new { usuarios = new[] { e.UsuarioInactivo } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Retirar_un_autorizador_conserva_su_asignacion_en_el_historial_con_vigencia()
    {
        var area = await CrearAreaAsync();
        var ruta = $"/api/v1/areas/{Id(area)}/autorizadores";
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PutAsJsonAsync(ruta, new { usuarios = new[] { e.OperadorA, e.SinPermisosA } })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PutAsJsonAsync(ruta, new { usuarios = new[] { e.SinPermisosA } })).StatusCode);

        var historial = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/areas/{Id(area)}/asignaciones");
        var operador = Assert.Single(historial.EnumerateArray(), a => a.GetProperty("usuarioId").GetGuid() == e.OperadorA);
        Assert.Equal("Autorizador", operador.GetProperty("funcion").GetString());
        Assert.NotEqual(JsonValueKind.Null, operador.GetProperty("hastaEn").ValueKind);
        var vigente = Assert.Single(historial.EnumerateArray(), a => a.GetProperty("usuarioId").GetGuid() == e.SinPermisosA);
        Assert.Equal(JsonValueKind.Null, vigente.GetProperty("hastaEn").ValueKind);
        await EventoAsync(Id(area), "area.autorizadores");
    }

    [Fact]
    public async Task Ser_autorizador_no_concede_permisos()
    {
        var area = await CrearAreaAsync();
        await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/areas/{Id(area)}/autorizadores", new { usuarios = new[] { e.SinPermisosA } });

        var me = await fabrica.Como(e.SinPermisosA).GetFromJsonAsync<JsonElement>("/api/v1/me");

        Assert.DoesNotContain(me.GetProperty("permisos").EnumerateArray(), p => p.GetString()!.StartsWith("areas.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Inactivar_conserva_el_area_y_sus_asignaciones_consultables()
    {
        var area = await CrearAreaAsync();
        await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/areas/{Id(area)}/responsables", new { usuarios = new[] { e.OperadorA } });

        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PostAsync($"/api/v1/areas/{Id(area)}/inactivar", null)).StatusCode);

        var consultada = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/areas/{Id(area)}");
        Assert.Equal("Inactiva", consultada.GetProperty("estado").GetString());
        var historial = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/areas/{Id(area)}/asignaciones");
        Assert.Single(historial.EnumerateArray());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await fabrica.Como(e.AdminA).PostAsync($"/api/v1/areas/{Id(area)}/inactivar", null)).StatusCode);
    }

    [Fact]
    public async Task Un_operador_fuera_de_la_sede_no_ve_ni_administra_sus_areas()
    {
        var areaA2 = await CrearAreaAsync(e.SedeA2, "Bodega");
        var limitado = await UsuarioAsync([e.SedeA1], Permisos.AreasConsultar, Permisos.AreasModificar);

        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(limitado).GetAsync($"/api/v1/sedes/{e.SedeA2}/areas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(limitado).GetAsync($"/api/v1/areas/{Id(areaA2)}")).StatusCode);
    }

    [Fact]
    public async Task Modificar_con_version_obsoleta_responde_409()
    {
        var area = await CrearAreaAsync();
        var cuerpo = new { nombre = "Torre 1 Norte", version = area.GetProperty("version").GetUInt32() };

        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/areas/{Id(area)}", cuerpo)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/areas/{Id(area)}", cuerpo)).StatusCode);
    }

    private async Task<Guid> UsuarioAsync(Guid[] sedes, params string[] permisos)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var usuario = Usuario.Crear($"area.{Guid.NewGuid():N}@ejemplo.test", "Usuario de área");
        contexto.Add(usuario);
        contexto.Add(Membresia.Crear(usuario.Id, e.OrgA, permisos, alcanceTotalSedes: false, sedesAsignadas: sedes));
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    private async Task EventoAsync(Guid objeto, string accion)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        Assert.True(await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .AnyAsync(a => a.ObjetoId == objeto && a.Accion == accion), $"Falta el evento {accion}.");
    }
}
