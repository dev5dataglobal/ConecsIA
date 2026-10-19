using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ConecsIA.IntegrationTests;

/// <summary>HU-007 (ADR-005, D-03b): contexto derivado de la membresía, multi-membresía y denegaciones.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ContextoOrganizacionTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;
    private Escenario e = null!;
    private Guid multi;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var usuario = Usuario.Crear($"multi.{Guid.NewGuid():N}@ejemplo.test", "Usuaria multi");
        contexto.Add(usuario);
        contexto.AddRange(
            Membresia.Crear(usuario.Id, e.OrgA, [Permisos.SedesConsultar], alcanceTotalSedes: true, sedesAsignadas: []),
            Membresia.Crear(usuario.Id, e.OrgB, [Permisos.SedesConsultar], alcanceTotalSedes: true, sedesAsignadas: []));
        await contexto.SaveChangesAsync();
        multi = usuario.Id;
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    [Fact]
    public async Task Con_varias_membresias_y_sin_seleccion_responde_403_con_codigo_estable()
    {
        var respuesta = await fabrica.Como(multi).GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ORGANIZACION_NO_SELECCIONADA", cuerpo.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Las_membresias_se_listan_sin_haber_seleccionado_organizacion()
    {
        var membresias = await fabrica.Como(multi).GetFromJsonAsync<JsonElement>("/api/v1/me/membresias");

        var ids = membresias.EnumerateArray().Select(m => m.GetProperty("organizacionId").GetGuid()).Order().ToList();
        Assert.Equal(new[] { e.OrgA, e.OrgB }.Order(), ids);
        Assert.All(membresias.EnumerateArray(), m => Assert.False(string.IsNullOrEmpty(m.GetProperty("nombre").GetString())));
    }

    [Fact]
    public async Task Las_membresias_solo_incluyen_las_del_usuario_autenticado()
    {
        var membresias = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>("/api/v1/me/membresias");

        Assert.Equal(e.OrgA, Assert.Single(membresias.EnumerateArray()).GetProperty("organizacionId").GetGuid());
    }

    [Fact]
    public async Task Listar_membresias_exige_un_usuario_activo()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await fabrica.Como(e.UsuarioInactivo).GetAsync("/api/v1/me/membresias")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fabrica.CreateClient().GetAsync("/api/v1/me/membresias")).StatusCode);
    }

    [Fact]
    public async Task Al_seleccionar_cada_organizacion_solo_se_ven_sus_sedes()
    {
        var enA = await fabrica.Como(multi, e.OrgA).GetFromJsonAsync<JsonElement>("/api/v1/sedes?tamano=100");
        var enB = await fabrica.Como(multi, e.OrgB).GetFromJsonAsync<JsonElement>("/api/v1/sedes?tamano=100");

        Assert.Contains(e.SedeA1, Ids(enA));
        Assert.DoesNotContain(e.SedeB1, Ids(enA));
        Assert.Equal([e.SedeB1], Ids(enB));
    }

    [Fact]
    public async Task Un_cambio_de_permisos_de_la_membresia_se_aplica_en_la_siguiente_peticion()
    {
        var cliente = fabrica.Como(e.OperadorA);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/v1/sedes/{e.SedeA1}")).StatusCode);

        await using (var conexion = new NpgsqlConnection(baseDatos.CadenaApp))
        {
            await conexion.OpenAsync();
            await using var quitar = new NpgsqlCommand(
                "UPDATE membresia SET permisos = '{}' WHERE usuario_id = @u", conexion);
            quitar.Parameters.AddWithValue("u", e.OperadorA);
            await quitar.ExecuteNonQueryAsync();
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync($"/api/v1/sedes/{e.SedeA1}")).StatusCode);
    }

    [Fact]
    public async Task Una_denegacion_de_permiso_queda_auditada_sin_identificadores_en_la_ruta()
    {
        var respuesta = await fabrica.Como(e.OperadorA).PostAsync($"/api/v1/sedes/{e.SedeA1}/inactivar", null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var evento = await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .SingleAsync(a => a.ActorUsuarioId == e.OperadorA && a.Accion == "seguridad.acceso_denegado");
        Assert.Equal(e.OrgA, evento.OrganizacionId);
        Assert.Equal(ResultadoAuditoria.Denegado, evento.Resultado);
        Assert.Contains(AccesoDenegadoException.CodigoPermisoRequerido, evento.ValoresNuevos, StringComparison.Ordinal);
        Assert.DoesNotContain(e.SedeA1.ToString(), evento.ValoresNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_proceso_de_fondo_opera_solo_en_la_organizacion_explicita()
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoProceso(e.OrgB, [Permisos.SedesConsultar]);
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();

        var organizaciones = await contexto.Set<Sede>().Select(s => s.OrganizacionId).Distinct().ToListAsync();
        Assert.Equal([e.OrgB], organizaciones);

        contexto.Add(Sede.Crear(e.OrgA, "Desde proceso", null, null, null, "UTC", DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<EscrituraEntreOrganizacionesException>(() => contexto.SaveChangesAsync());
    }

    private static List<Guid> Ids(JsonElement pagina) =>
        pagina.GetProperty("elementos").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).ToList();
}
