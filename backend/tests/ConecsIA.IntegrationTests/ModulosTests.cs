using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Modulos;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests;

/// <summary>HU-011: habilitación de módulos opcionales por organización (CA-23).</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class ModulosTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private FabricaApi fabrica = null!;
    private Escenario e = null!;
    private Guid superadmin;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var usuario = Usuario.Crear($"super.{Guid.NewGuid():N}@ejemplo.test", "Superadministración SaaS");
        usuario.AsignarPermisosSaas(PermisosSaas.Todos);
        contexto.Add(usuario);
        await contexto.SaveChangesAsync();
        superadmin = usuario.Id;
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    private string RutaModulos(Guid organizacion) => $"/api/v1/saas/organizaciones/{organizacion}/modulos";

    private Task<HttpResponseMessage> HabilitarAsync(Guid organizacion, params string[] modulos) =>
        fabrica.Como(superadmin).PutAsJsonAsync(RutaModulos(organizacion), new { habilitados = modulos });

    [Fact]
    public async Task El_catalogo_lista_los_modulos_opcionales()
    {
        var catalogo = await fabrica.Como(superadmin).GetFromJsonAsync<JsonElement>("/api/v1/saas/modulos");

        Assert.Equal(ModulosOpcionales.Todos.Select(m => m.Clave).Order(),
            catalogo.EnumerateArray().Select(m => m.GetProperty("clave").GetString()!).Order());
    }

    [Fact]
    public async Task Una_operacion_nueva_de_un_modulo_deshabilitado_se_rechaza_con_codigo_estable()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsync("/api/v1/soporte-pruebas/operacion-de-modulo/vehiculos", null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MODULO_NO_HABILITADO", cuerpo.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Habilitar_tiene_efecto_inmediato_solo_en_esa_organizacion()
    {
        Assert.Equal(HttpStatusCode.OK, (await HabilitarAsync(e.OrgA, "vehiculos")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await fabrica.Como(e.AdminA).PostAsync("/api/v1/soporte-pruebas/operacion-de-modulo/vehiculos", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await fabrica.Como(e.AdminB).PostAsync("/api/v1/soporte-pruebas/operacion-de-modulo/vehiculos", null)).StatusCode);

        var deA = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>("/api/v1/me/modulos");
        Assert.Equal(["vehiculos"], deA.EnumerateArray().Select(m => m.GetString()));
        var deB = await fabrica.Como(e.AdminB).GetFromJsonAsync<JsonElement>("/api/v1/me/modulos");
        Assert.Empty(deB.EnumerateArray());
    }

    [Fact]
    public async Task Deshabilitar_impide_operaciones_nuevas_sin_borrar_el_registro_de_habilitacion()
    {
        await HabilitarAsync(e.OrgA, "vehiculos", "equipos");

        Assert.Equal(HttpStatusCode.OK, (await HabilitarAsync(e.OrgA, "equipos")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await fabrica.Como(e.AdminA).PostAsync("/api/v1/soporte-pruebas/operacion-de-modulo/vehiculos", null)).StatusCode);
        var estado = await fabrica.Como(superadmin).GetFromJsonAsync<JsonElement>(RutaModulos(e.OrgA));
        var vehiculos = estado.EnumerateArray().Single(m => m.GetProperty("modulo").GetString() == "vehiculos");
        Assert.False(vehiculos.GetProperty("habilitado").GetBoolean());
        Assert.Equal(superadmin, vehiculos.GetProperty("cambiadoPor").GetGuid());
    }

    [Fact]
    public async Task El_cambio_de_habilitacion_queda_auditado_con_valores_anterior_y_nuevo()
    {
        await HabilitarAsync(e.OrgA, "correo");

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var evento = await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .SingleAsync(a => a.ObjetoId == e.OrgA && a.Accion == "organizacion.modulos");
        Assert.Equal(superadmin, evento.ActorUsuarioId);
        Assert.Contains("correo", evento.ValoresNuevos, StringComparison.Ordinal);
        Assert.NotNull(evento.ValoresAnteriores);
    }

    [Fact]
    public async Task Un_administrador_de_organizacion_no_puede_habilitarse_modulos()
    {
        var respuesta = await fabrica.Como(e.AdminA).PutAsJsonAsync(RutaModulos(e.OrgA), new { habilitados = new[] { "vehiculos" } });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_modulo_fuera_del_catalogo_responde_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await HabilitarAsync(e.OrgA, "teletransporte")).StatusCode);
    }
}
