using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.SharedKernel.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests;

/// <summary>
/// HU-013 de punta a punta contra PostgreSQL real: autenticación, contexto de organización,
/// permisos, alcance por sede (CA-02), aislamiento (CA-01), validación, concurrencia y auditoría.
/// </summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class SedesTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string Ruta = "/api/v1/sedes";
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    private static object NuevaSede(string nombre = "Sede Central", string zona = "America/Bogota") => new
    {
        nombre,
        direccion = "Carrera 7 # 10-20",
        ciudad = "Bogotá",
        pais = "Colombia",
        zonaHoraria = zona,
    };

    // ---------- Autenticación y contexto ----------

    [Fact]
    public async Task Sin_autenticacion_responde_401_problem_details()
    {
        var respuesta = await fabrica.CreateClient().GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Usuario_inactivo_responde_401()
    {
        var respuesta = await fabrica.Como(e.UsuarioInactivo).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Seleccionar_organizacion_ajena_responde_403_y_queda_auditado()
    {
        var respuesta = await fabrica.Como(e.AdminA, organizacion: e.OrgB).GetAsync(Ruta);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ORGANIZACION_NO_PERMITIDA", cuerpo.GetProperty("code").GetString());

        var eventos = await EventosAsync(e.AdminA);
        var evento = Assert.Single(eventos, ev => ev.Accion == "contexto.seleccionar_organizacion");
        Assert.Equal(ResultadoAuditoria.Denegado, evento.Resultado);
        Assert.Contains(e.OrgB.ToString(), evento.ValoresNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Me_devuelve_organizacion_permisos_y_alcance_derivados_del_servidor()
    {
        var cuerpo = await fabrica.Como(e.OperadorA).GetFromJsonAsync<JsonElement>("/api/v1/me");

        Assert.Equal(e.OrgA, cuerpo.GetProperty("organizacion").GetProperty("id").GetGuid());
        var permisos = cuerpo.GetProperty("permisos").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Contains("sedes.consultar", permisos);
        Assert.DoesNotContain("sedes.crear", permisos);
        Assert.False(cuerpo.GetProperty("alcanceTotalSedes").GetBoolean());
        Assert.Equal(e.SedeA1, Assert.Single(cuerpo.GetProperty("sedesAsignadas").EnumerateArray()).GetGuid());
    }

    // ---------- Crear ----------

    [Fact]
    public async Task Admin_crea_sede_201_y_la_auditoria_registra_actor_organizacion_y_valores_nuevos()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, NuevaSede());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var sede = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var id = sede.GetProperty("id").GetGuid();
        Assert.Equal("Sede Central", sede.GetProperty("nombre").GetString());
        Assert.Equal("America/Bogota", sede.GetProperty("zonaHoraria").GetString());
        Assert.Equal("Activa", sede.GetProperty("estado").GetString());
        Assert.EndsWith($"{Ruta}/{id}", respuesta.Headers.Location!.ToString(), StringComparison.Ordinal);

        var evento = Assert.Single(await EventosAsync(e.AdminA), ev => ev.Accion == "sede.crear");
        Assert.Equal(e.OrgA, evento.OrganizacionId);
        Assert.Equal(id, evento.ObjetoId);
        Assert.Equal(ResultadoAuditoria.Exitoso, evento.Resultado);
        Assert.Null(evento.ValoresAnteriores);
        Assert.Contains("America/Bogota", evento.ValoresNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task La_organizacion_de_la_sede_se_toma_del_contexto_no_del_cuerpo()
    {
        var cuerpo = new { nombre = "Intrusa", ciudad = "Lima", pais = "Perú", zonaHoraria = "America/Lima", organizacionId = e.OrgB };

        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, cuerpo);
        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminB).GetAsync($"{Ruta}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).GetAsync($"{Ruta}/{id}")).StatusCode);
    }

    [Fact]
    public async Task Zona_horaria_invalida_responde_400_con_error_de_campo()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, NuevaSede(zona: "SA Pacific Standard Time"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(cuerpo.GetProperty("errors").TryGetProperty("zonaHoraria", out _));
    }

    [Theory]
    [InlineData("{\"nombre\": \"Sede\", ")]                       // JSON truncado
    [InlineData("{\"nombre\":\"Bogotá\"}", true)]            // UTF-8 inválido (byte suelto de Latin-1)
    [InlineData("")]                                              // cuerpo vacío
    public async Task Un_cuerpo_malformado_responde_400_y_nunca_500(string cuerpo, bool latin1 = false)
    {
        var bytes = latin1 ? System.Text.Encoding.Latin1.GetBytes(cuerpo) : System.Text.Encoding.UTF8.GetBytes(cuerpo);
        using var contenido = new ByteArrayContent(bytes);
        contenido.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var respuesta = await fabrica.Como(e.AdminA).PostAsync(Ruta, contenido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("ConecsIA.", await respuesta.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Nombre_vacio_responde_400()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, NuevaSede(nombre: "  "));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(cuerpo.GetProperty("errors").TryGetProperty("nombre", out _));
    }

    [Fact]
    public async Task Nombre_duplicado_normalizado_en_la_misma_organizacion_responde_409()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, NuevaSede(nombre: "  sede   a1 "));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SEDE_NOMBRE_DUPLICADO", cuerpo.GetProperty("code").GetString());
    }

    [Fact]
    public async Task El_mismo_nombre_se_permite_en_otra_organizacion()
    {
        var respuesta = await fabrica.Como(e.AdminB).PostAsJsonAsync(Ruta, NuevaSede(nombre: "Sede A1"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Altas_concurrentes_con_el_mismo_nombre_crean_una_sola_sede()
    {
        var cliente = fabrica.Como(e.AdminA);
        var tareas = Enumerable.Range(0, 5).Select(_ => cliente.PostAsJsonAsync(Ruta, NuevaSede(nombre: "Sede Concurrente")));

        var respuestas = await Task.WhenAll(tareas);

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    // ---------- Permisos y alcance (CA-02) ----------

    [Fact]
    public async Task Un_rol_llamado_Administrador_sin_claves_de_permiso_recibe_403()
    {
        var cliente = fabrica.Como(e.SinPermisosA);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(Ruta, NuevaSede())).StatusCode);
    }

    [Fact]
    public async Task Operador_asignado_a_S1_solo_ve_S1()
    {
        var cuerpo = await fabrica.Como(e.OperadorA).GetFromJsonAsync<JsonElement>(Ruta);

        var ids = cuerpo.GetProperty("elementos").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).ToList();
        Assert.Equal([e.SedeA1], ids);
        Assert.Equal(1, cuerpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Operador_asignado_a_S1_no_puede_leer_ni_modificar_S2_aunque_la_UI_no_lo_impida()
    {
        var cliente = fabrica.Como(e.OperadorA);

        var leer = await cliente.GetAsync($"{Ruta}/{e.SedeA2}");
        var modificar = await cliente.PutAsJsonAsync($"{Ruta}/{e.SedeA2}", new
        {
            nombre = "Cambiada",
            ciudad = "Cali",
            pais = "Colombia",
            zonaHoraria = "America/Bogota",
            version = 1u,
        });

        Assert.Equal(HttpStatusCode.Forbidden, leer.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, modificar.StatusCode);
        Assert.Equal("FUERA_DE_ALCANCE", (await leer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Operador_sin_permiso_de_crear_ni_inactivar_recibe_403()
    {
        var cliente = fabrica.Como(e.OperadorA);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(Ruta, NuevaSede())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync($"{Ruta}/{e.SedeA1}/inactivar", null)).StatusCode);
    }

    // ---------- Aislamiento entre organizaciones (CA-01) ----------

    [Fact]
    public async Task Admin_de_A_no_ve_sedes_de_B_en_el_listado()
    {
        var cuerpo = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"{Ruta}?tamano=100");

        var ids = cuerpo.GetProperty("elementos").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(e.SedeA1, ids);
        Assert.Contains(e.SedeA2, ids);
        Assert.DoesNotContain(e.SedeB1, ids);
    }

    [Fact]
    public async Task Admin_de_A_recibe_404_al_leer_modificar_o_inactivar_una_sede_de_B()
    {
        var cliente = fabrica.Como(e.AdminA);

        var leer = await cliente.GetAsync($"{Ruta}/{e.SedeB1}");
        var modificar = await cliente.PutAsJsonAsync($"{Ruta}/{e.SedeB1}", new
        {
            nombre = "Robada",
            ciudad = "Lima",
            pais = "Perú",
            zonaHoraria = "America/Lima",
            version = 1u,
        });
        var inactivar = await cliente.PostAsync($"{Ruta}/{e.SedeB1}/inactivar", null);

        Assert.Equal(HttpStatusCode.NotFound, leer.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, modificar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inactivar.StatusCode);

        var sedeB = await fabrica.Como(e.AdminB).GetFromJsonAsync<JsonElement>($"{Ruta}/{e.SedeB1}");
        Assert.Equal("Sede B1", sedeB.GetProperty("nombre").GetString());
        Assert.Equal("Activa", sedeB.GetProperty("estado").GetString());
    }

    // ---------- Modificar y concurrencia ----------

    [Fact]
    public async Task Modificar_con_version_vigente_actualiza_y_audita_valores_anterior_y_nuevo()
    {
        var cliente = fabrica.Como(e.AdminA);
        var actual = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}/{e.SedeA2}");

        var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{e.SedeA2}", new
        {
            nombre = "Sede A2 Renovada",
            direccion = "Nueva dirección",
            ciudad = "Medellín",
            pais = "Colombia",
            zonaHoraria = "America/Bogota",
            version = actual.GetProperty("version").GetUInt32(),
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var evento = Assert.Single(await EventosAsync(e.AdminA), ev => ev.Accion == "sede.modificar");
        Assert.Contains("Sede A2", evento.ValoresAnteriores, StringComparison.Ordinal);
        Assert.Contains("Sede A2 Renovada", evento.ValoresNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Modificar_con_version_obsoleta_responde_409()
    {
        var cliente = fabrica.Como(e.AdminA);
        var actual = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}/{e.SedeA2}");
        var version = actual.GetProperty("version").GetUInt32();
        object Cambio(string nombre) => new { nombre, ciudad = "Medellín", pais = "Colombia", zonaHoraria = "America/Bogota", version };

        var primera = await cliente.PutAsJsonAsync($"{Ruta}/{e.SedeA2}", Cambio("Primera edición"));
        var segunda = await cliente.PutAsJsonAsync($"{Ruta}/{e.SedeA2}", Cambio("Edición obsoleta"));

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        var final = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}/{e.SedeA2}");
        Assert.Equal("Primera edición", final.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Operador_con_permiso_de_modificar_puede_editar_su_sede_asignada()
    {
        var cliente = fabrica.Como(e.OperadorA);
        var actual = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}/{e.SedeA1}");

        var respuesta = await cliente.PutAsJsonAsync($"{Ruta}/{e.SedeA1}", new
        {
            nombre = "Sede A1",
            direccion = "Portería principal",
            ciudad = "Bogotá",
            pais = "Colombia",
            zonaHoraria = "America/Bogota",
            version = actual.GetProperty("version").GetUInt32(),
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    // ---------- Inactivar (CA-03) ----------

    [Fact]
    public async Task Inactivar_conserva_la_sede_consultable_y_audita_el_cambio_de_estado()
    {
        var cliente = fabrica.Como(e.AdminA);

        var respuesta = await cliente.PostAsync($"{Ruta}/{e.SedeA2}/inactivar", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var sede = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}/{e.SedeA2}");
        Assert.Equal("Inactiva", sede.GetProperty("estado").GetString());
        Assert.NotEqual(JsonValueKind.Null, sede.GetProperty("inactivadaEn").ValueKind);

        var inactivas = await cliente.GetFromJsonAsync<JsonElement>($"{Ruta}?estado=Inactiva");
        Assert.Contains(e.SedeA2, inactivas.GetProperty("elementos").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()));

        var evento = Assert.Single(await EventosAsync(e.AdminA), ev => ev.Accion == "sede.inactivar");
        Assert.Contains("Activa", evento.ValoresAnteriores, StringComparison.Ordinal);
        Assert.Contains("Inactiva", evento.ValoresNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inactivar_una_sede_ya_inactiva_responde_422()
    {
        var cliente = fabrica.Como(e.AdminA);
        await cliente.PostAsync($"{Ruta}/{e.SedeA2}/inactivar", null);

        var respuesta = await cliente.PostAsync($"{Ruta}/{e.SedeA2}/inactivar", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("SEDE_YA_INACTIVA", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Paginacion_invalida_responde_400()
    {
        var respuesta = await fabrica.Como(e.AdminA).GetAsync($"{Ruta}?tamano=500");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    private async Task<List<EventoAuditoria>> EventosAsync(Guid actor)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        return await contexto.Set<EventoAuditoria>().AsNoTracking().Where(ev => ev.ActorUsuarioId == actor).ToListAsync();
    }
}
