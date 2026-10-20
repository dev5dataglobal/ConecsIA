using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests;

/// <summary>HU-010: plantilla básica por tipo, copiada a la organización sin pisar personalizaciones (CA-26).</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class PlantillasTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string Catalogo = "/api/v1/catalogos/tipos-area";
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

    private Task<HttpResponseMessage> AplicarAsync(Guid organizacion) =>
        fabrica.Como(superadmin).PostAsync($"/api/v1/saas/organizaciones/{organizacion}/aplicar-plantilla", null);

    private async Task<List<JsonElement>> ElementosAsync(Guid como) =>
        (await fabrica.Como(como).GetFromJsonAsync<JsonElement>(Catalogo)).EnumerateArray().ToList();

    [Fact]
    public async Task Aplicar_la_plantilla_copia_los_elementos_del_tipo_con_su_origen()
    {
        var respuesta = await AplicarAsync(e.OrgA);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var elementos = await ElementosAsync(e.AdminA);
        Assert.NotEmpty(elementos);
        Assert.All(elementos, x => Assert.StartsWith("plantilla:", x.GetProperty("origen").GetString()));
        Assert.Empty(await ElementosAsync(e.AdminB));
    }

    [Fact]
    public async Task Reaplicar_no_duplica_ni_modifica_personalizaciones_inactivaciones_ni_roles()
    {
        await CambiarTipoAsync(e.OrgA, "empresa");
        await AplicarAsync(e.OrgA);
        var original = await ElementosAsync(e.AdminA);
        var primero = original[0];
        var id = primero.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PutAsJsonAsync($"{Catalogo}/{id}", new { nombre = "Nombre propio" })).StatusCode);
        var segundo = original[1].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PostAsync($"{Catalogo}/{segundo}/inactivar", null)).StatusCode);
        var rolesAntes = await RolesAsync(e.OrgA);

        // Cambiar de tipo y reaplicar.
        await CambiarTipoAsync(e.OrgA, "universidad");
        Assert.Equal(HttpStatusCode.OK, (await AplicarAsync(e.OrgA)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AplicarAsync(e.OrgA)).StatusCode);

        var despues = await ElementosAsync(e.AdminA);
        Assert.Equal("Nombre propio", despues.Single(x => x.GetProperty("id").GetGuid() == id).GetProperty("nombre").GetString());
        Assert.Equal("Inactivo", despues.Single(x => x.GetProperty("id").GetGuid() == segundo).GetProperty("estado").GetString());
        Assert.Equal(despues.Count, despues.Select(x => x.GetProperty("clave").GetString()).Distinct().Count());
        Assert.True(despues.Count > original.Count, "Agrega los elementos de la plantilla nueva que faltaban.");
        Assert.Equal(rolesAntes, await RolesAsync(e.OrgA));
    }

    [Fact]
    public async Task Aplicaciones_simultaneas_no_duplican_elementos()
    {
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => AplicarAsync(e.OrgB)));

        Assert.All(respuestas, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        var elementos = await ElementosAsync(e.AdminB);
        Assert.Equal(elementos.Count, elementos.Select(x => x.GetProperty("clave").GetString()).Distinct().Count());
    }

    [Fact]
    public async Task Una_organizacion_nueva_recibe_la_plantilla_de_su_tipo()
    {
        var correo = $"admin.{Guid.NewGuid():N}@ejemplo.test";
        var creada = await fabrica.Como(superadmin).PostAsJsonAsync("/api/v1/saas/organizaciones", new
        {
            nombre = $"Club {Guid.NewGuid():N}"[..16], tipo = "club", administrador = new { correo, nombre = "Admin" },
        });
        var id = (await creada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        Assert.True(await contexto.Set<ElementoCatalogo>().AnyAsync(x => x.OrganizacionId == id && x.Origen == "plantilla:club:v1"));
        Assert.True(await contexto.Set<EventoAuditoria>().AnyAsync(a => a.ObjetoId == id && a.Accion == "organizacion.aplicar_plantilla"));
    }

    [Fact]
    public async Task El_administrador_agrega_elementos_personalizados_y_no_aplica_plantillas()
    {
        var alta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Catalogo, new { clave = "mezzanine", nombre = "Mezzanine" });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        Assert.Equal("personalizado", (await alta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("origen").GetString());

        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.AdminA).PostAsync($"/api/v1/saas/organizaciones/{e.OrgA}/aplicar-plantilla", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.OperadorA).PostAsJsonAsync(Catalogo, new { clave = "x", nombre = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).GetAsync("/api/v1/catalogos/inexistente")).StatusCode);
    }

    private async Task CambiarTipoAsync(Guid organizacion, string tipo)
    {
        var org = await fabrica.Como(superadmin).GetFromJsonAsync<JsonElement>($"/api/v1/saas/organizaciones/{organizacion}");
        var respuesta = await fabrica.Como(superadmin).PutAsJsonAsync($"/api/v1/saas/organizaciones/{organizacion}", new
        {
            nombre = org.GetProperty("nombre").GetString(), tipo, version = org.GetProperty("version").GetUInt32(),
        });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    private async Task<List<string>> RolesAsync(Guid organizacion)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        return await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<Rol>()
            .Where(r => r.OrganizacionId == organizacion)
            .OrderBy(r => r.Id).Select(r => r.Id + string.Join(",", r.Permisos)).ToListAsync();
    }
}
