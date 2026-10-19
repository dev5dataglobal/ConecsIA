using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests;

/// <summary>HU-009: organizaciones desde el plano SaaS y lectura/edición de la propia organización.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class OrganizacionesTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string RutaSaas = "/api/v1/saas/organizaciones";
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

    private static object NuevaOrganizacion(string nombre, string correo) => new
    {
        nombre,
        tipo = "conjunto-residencial",
        identificacionLegal = "900.123.456-7",
        zonaHoraria = "America/Bogota",
        contactoAdministrativo = "Administración del conjunto",
        administrador = new { correo, nombre = "Primera Administradora" },
    };

    [Fact]
    public async Task El_superadministrador_crea_una_organizacion_activa_auditada_y_con_su_primer_administrador()
    {
        var nombre = $"Conjunto {Guid.NewGuid():N}"[..20];
        var correo = $"primera.{Guid.NewGuid():N}@ejemplo.test";

        var respuesta = await fabrica.Como(superadmin).PostAsJsonAsync(RutaSaas, NuevaOrganizacion(nombre, correo));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var org = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var id = org.GetProperty("id").GetGuid();
        Assert.Equal("Activa", org.GetProperty("estado").GetString());

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var evento = await contexto.Set<EventoAuditoria>().SingleAsync(a => a.ObjetoId == id && a.Accion == "organizacion.crear");
        Assert.Equal(superadmin, evento.ActorUsuarioId);
        Assert.Equal(id, evento.OrganizacionId);

        // Primer administrador: usuario + membresía con el rol de administrador copiado de la plantilla.
        var admin = await contexto.Set<Usuario>().SingleAsync(u => u.Correo == correo);
        var membresia = await contexto.Set<Membresia>().SingleAsync(m => m.UsuarioId == admin.Id && m.OrganizacionId == id);
        var roles = await contexto.Set<Rol>().Where(r => r.OrganizacionId == id).ToListAsync();
        Assert.Equal(PlantillasRol.Todas.Count, roles.Count);
        Assert.Equal([roles.Single(r => r.Plantilla == "administrador-organizacion").Id], membresia.Roles);

        var me = await fabrica.Como(admin.Id).GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.Contains(Permisos.RolesAdministrar, me.GetProperty("permisos").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    public async Task Un_nombre_repetido_responde_409_aunque_cambien_mayusculas_y_espacios()
    {
        var nombre = $"Club {Guid.NewGuid():N}"[..16];
        Assert.Equal(HttpStatusCode.Created,
            (await fabrica.Como(superadmin).PostAsJsonAsync(RutaSaas, NuevaOrganizacion(nombre, $"a.{Guid.NewGuid():N}@ejemplo.test"))).StatusCode);

        var repetido = await fabrica.Como(superadmin).PostAsJsonAsync(RutaSaas,
            NuevaOrganizacion($"  {nombre.ToUpperInvariant()} ", $"b.{Guid.NewGuid():N}@ejemplo.test"));

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task Un_tipo_de_organizacion_inexistente_responde_400()
    {
        var cuerpo = new { nombre = "Raro", tipo = "nave-espacial", administrador = new { correo = "x@ejemplo.test", nombre = "X" } };

        Assert.Equal(HttpStatusCode.BadRequest, (await fabrica.Como(superadmin).PostAsJsonAsync(RutaSaas, cuerpo)).StatusCode);
    }

    [Fact]
    public async Task Un_administrador_de_organizacion_no_usa_el_plano_SaaS()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.AdminA).GetAsync(RutaSaas)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await fabrica.Como(e.AdminA).PostAsJsonAsync(RutaSaas, NuevaOrganizacion("Intrusa", "i@ejemplo.test"))).StatusCode);
    }

    [Fact]
    public async Task El_superadministrador_no_obtiene_acceso_ordinario_a_datos_de_clientes()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(superadmin).GetAsync("/api/v1/sedes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(superadmin, e.OrgA).GetAsync("/api/v1/sedes")).StatusCode);
    }

    [Fact]
    public async Task El_administrador_lee_y_edita_su_organizacion_y_recibe_404_con_otra()
    {
        var propia = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"/api/v1/organizaciones/{e.OrgA}");
        Assert.Equal(e.OrgA, propia.GetProperty("id").GetGuid());

        var editar = await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/organizaciones/{e.OrgA}", new
        {
            nombre = propia.GetProperty("nombre").GetString(),
            zonaHoraria = "America/Bogota",
            contactoAdministrativo = "Gerencia",
            version = propia.GetProperty("version").GetUInt32(),
        });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).GetAsync($"/api/v1/organizaciones/{e.OrgB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).PutAsJsonAsync($"/api/v1/organizaciones/{e.OrgB}", new
        {
            nombre = "Robada",
            version = 1u,
        })).StatusCode);
    }

    [Fact]
    public async Task Una_organizacion_inactivada_bloquea_operaciones_nuevas_y_conserva_sus_datos()
    {
        var antes = await fabrica.Como(e.AdminB).GetFromJsonAsync<JsonElement>($"/api/v1/sedes/{e.SedeB1}");

        var inactivar = await fabrica.Como(superadmin).PostAsync($"{RutaSaas}/{e.OrgB}/inactivar", null);

        Assert.Equal(HttpStatusCode.OK, inactivar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.AdminB).GetAsync("/api/v1/sedes")).StatusCode);
        var consultada = await fabrica.Como(superadmin).GetFromJsonAsync<JsonElement>($"{RutaSaas}/{e.OrgB}");
        Assert.Equal("Inactiva", consultada.GetProperty("estado").GetString());

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        Assert.True(await contexto.Set<ConecsIA.Modules.Estructura.Domain.Sede>().AnyAsync(s => s.Id == e.SedeB1));
        var evento = await contexto.Set<EventoAuditoria>().SingleAsync(a => a.ObjetoId == e.OrgB && a.Accion == "organizacion.inactivar");
        Assert.Equal(ResultadoAuditoria.Exitoso, evento.Resultado);
        Assert.Equal("Sede B1", antes.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task Reactivar_devuelve_el_acceso_y_queda_auditado()
    {
        await fabrica.Como(superadmin).PostAsync($"{RutaSaas}/{e.OrgB}/inactivar", null);

        var reactivar = await fabrica.Como(superadmin).PostAsync($"{RutaSaas}/{e.OrgB}/reactivar", null);

        Assert.Equal(HttpStatusCode.OK, reactivar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminB).GetAsync("/api/v1/sedes")).StatusCode);
    }

    [Fact]
    public async Task Modificar_con_version_obsoleta_responde_409()
    {
        var actual = await fabrica.Como(superadmin).GetFromJsonAsync<JsonElement>($"{RutaSaas}/{e.OrgA}");
        var cuerpo = new
        {
            nombre = actual.GetProperty("nombre").GetString(),
            tipo = "empresa",
            version = actual.GetProperty("version").GetUInt32(),
        };
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(superadmin).PutAsJsonAsync($"{RutaSaas}/{e.OrgA}", cuerpo)).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await fabrica.Como(superadmin).PutAsJsonAsync($"{RutaSaas}/{e.OrgA}", cuerpo)).StatusCode);
    }

    [Fact]
    public async Task La_plataforma_informa_los_permisos_SaaS_sin_organizacion_resuelta()
    {
        var plataforma = await fabrica.Como(superadmin).GetFromJsonAsync<JsonElement>("/api/v1/me/plataforma");

        Assert.Equal(PermisosSaas.Todos.Order(),
            plataforma.GetProperty("permisosSaas").EnumerateArray().Select(p => p.GetString()!).Order());
    }
}
