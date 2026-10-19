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

namespace ConecsIA.IntegrationTests;

/// <summary>HU-017 de punta a punta: catálogo, roles por organización, anti-escalamiento y matriz de permisos.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class RolesTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string Ruta = "/api/v1/roles";
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    [Fact]
    public async Task El_catalogo_de_permisos_lista_recurso_y_accion()
    {
        var catalogo = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>("/api/v1/permisos");

        var claves = catalogo.EnumerateArray().Select(p => p.GetProperty("clave").GetString()).ToList();
        Assert.Equal(Permisos.Todos.Order(), claves.Order());
        Assert.All(catalogo.EnumerateArray(), p => Assert.False(string.IsNullOrEmpty(p.GetProperty("recurso").GetString())));
    }

    [Fact]
    public async Task Crear_un_rol_responde_201_y_audita_sus_permisos()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, new
        {
            nombre = "Supervisor de portería",
            permisos = new[] { Permisos.SedesConsultar, Permisos.SedesModificar },
            plantilla = "administrador-sede",
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var rol = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var evento = await EventoAsync(rol.GetProperty("id").GetGuid(), "rol.crear");
        Assert.Equal(e.OrgA, evento.OrganizacionId);
        Assert.Contains(Permisos.SedesModificar, evento.ValoresNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_permiso_fuera_del_catalogo_responde_400()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, new { nombre = "Raro", permisos = new[] { "todo.poder" } });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_nombre_repetido_en_la_organizacion_responde_409()
    {
        var respuesta = await fabrica.Como(e.AdminA).PostAsJsonAsync(Ruta, new { nombre = "PORTERÍA A", permisos = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Nadie_concede_permisos_que_no_tiene()
    {
        var limitado = await UsuarioConPermisosAsync(e.OrgA, Permisos.RolesConsultar, Permisos.RolesAdministrar);

        var respuesta = await fabrica.Como(limitado).PostAsJsonAsync(Ruta, new { nombre = "Escalado", permisos = new[] { Permisos.SedesCrear } });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ESCALAMIENTO_DE_PERMISOS", cuerpo.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Un_rol_llamado_Administrador_sin_permisos_no_concede_nada()
    {
        var rol = await CrearRolAsync("Administrador", []);
        var usuario = await UsuarioConRolesAsync(e.OrgA, rol);

        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(usuario).GetAsync(Ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(usuario).GetAsync("/api/v1/sedes")).StatusCode);
    }

    [Fact]
    public async Task Un_cambio_de_permisos_del_rol_rige_en_la_siguiente_peticion()
    {
        var rol = await CrearRolAsync("Consulta de sedes", [Permisos.SedesConsultar]);
        var usuario = await UsuarioConRolesAsync(e.OrgA, rol);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(usuario).GetAsync("/api/v1/sedes")).StatusCode);

        var actual = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"{Ruta}/{rol}");
        var modificar = await fabrica.Como(e.AdminA).PutAsJsonAsync($"{Ruta}/{rol}", new
        {
            nombre = "Consulta de sedes",
            permisos = Array.Empty<string>(),
            version = actual.GetProperty("version").GetUInt32(),
        });
        Assert.Equal(HttpStatusCode.OK, modificar.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(usuario).GetAsync("/api/v1/sedes")).StatusCode);
    }

    [Fact]
    public async Task Inactivar_un_rol_retira_sus_permisos_y_lo_conserva_consultable()
    {
        var rol = await CrearRolAsync("Temporal", [Permisos.SedesConsultar]);
        var usuario = await UsuarioConRolesAsync(e.OrgA, rol);

        var inactivar = await fabrica.Como(e.AdminA).PostAsync($"{Ruta}/{rol}/inactivar", null);

        Assert.Equal(HttpStatusCode.OK, inactivar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(usuario).GetAsync("/api/v1/sedes")).StatusCode);
        var consultado = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"{Ruta}/{rol}");
        Assert.Equal("Inactivo", consultado.GetProperty("estado").GetString());
        await EventoAsync(rol, "rol.inactivar");
    }

    [Fact]
    public async Task No_se_inactiva_el_unico_rol_activo_que_administra_roles()
    {
        var unico = await CrearRolAsync("Administración", [Permisos.RolesAdministrar]);

        var rechazo = await fabrica.Como(e.AdminA).PostAsync($"{Ruta}/{unico}/inactivar", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rechazo.StatusCode);
        var cuerpo = await rechazo.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ULTIMO_ROL_ADMINISTRADOR", cuerpo.GetProperty("code").GetString());

        await CrearRolAsync("Administración suplente", [Permisos.RolesAdministrar]);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PostAsync($"{Ruta}/{unico}/inactivar", null)).StatusCode);
    }

    /// <summary>
    /// Matriz plantilla × endpoint (DoD de HU-017): cada plantilla accede exactamente a lo que sus
    /// permisos conceden. Un 403 por falta de permiso nunca se confunde con otro error.
    /// </summary>
    public static TheoryData<string, string, string, string> Matriz()
    {
        (string Metodo, string Ruta, string Permiso)[] endpoints =
        [
            ("GET", "/api/v1/sedes", Permisos.SedesConsultar),
            ("POST", "/api/v1/sedes", Permisos.SedesCrear),
            ("GET", "/api/v1/roles", Permisos.RolesConsultar),
            ("GET", "/api/v1/permisos", Permisos.RolesConsultar),
            ("POST", "/api/v1/roles", Permisos.RolesAdministrar),
        ];
        var datos = new TheoryData<string, string, string, string>();
        foreach (var plantilla in PlantillasRol.Todas)
        {
            foreach (var (metodo, ruta, permiso) in endpoints)
            {
                datos.Add(plantilla.Clave, metodo, ruta, permiso);
            }
        }

        return datos;
    }

    [Theory]
    [MemberData(nameof(Matriz))]
    public async Task Cada_plantilla_accede_solo_a_lo_que_conceden_sus_permisos(string plantilla, string metodo, string ruta, string permiso)
    {
        var definicion = PlantillasRol.Todas.Single(p => p.Clave == plantilla);
        var rol = await CrearRolAsync($"Plantilla {plantilla}", definicion.Permisos, plantilla);
        var usuario = await UsuarioConRolesAsync(e.OrgA, rol);
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        var unico = Guid.NewGuid().ToString("N")[..8];
        if (ruta == "/api/v1/sedes" && metodo == "POST")
            solicitud.Content = JsonContent.Create(new { nombre = $"Sede {unico}", zonaHoraria = "UTC" });
        else if (metodo == "POST")
            solicitud.Content = JsonContent.Create(new { nombre = $"Rol {unico}", permisos = Array.Empty<string>() });

        var respuesta = await fabrica.Como(usuario).SendAsync(solicitud);

        if (definicion.Permisos.Contains(permiso))
            Assert.NotEqual(HttpStatusCode.Forbidden, respuesta.StatusCode);
        else
            Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    private async Task<Guid> CrearRolAsync(string nombre, IReadOnlyList<string> permisos, string? plantilla = null)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var rol = Rol.Crear(e.OrgA, nombre, null, permisos, plantilla, DateTimeOffset.UtcNow);
        contexto.Add(rol);
        await contexto.SaveChangesAsync();
        return rol.Id;
    }

    private async Task<Guid> UsuarioConRolesAsync(Guid organizacion, params Guid[] roles) =>
        await UsuarioAsync(organizacion, [], roles);

    private async Task<Guid> UsuarioConPermisosAsync(Guid organizacion, params string[] permisos) =>
        await UsuarioAsync(organizacion, permisos, []);

    private async Task<Guid> UsuarioAsync(Guid organizacion, string[] permisos, Guid[] roles)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var usuario = Usuario.Crear($"rol.{Guid.NewGuid():N}@ejemplo.test", "Usuario de rol");
        contexto.Add(usuario);
        contexto.Add(Membresia.Crear(usuario.Id, organizacion, permisos, alcanceTotalSedes: true, sedesAsignadas: [], roles: roles));
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    private async Task<EventoAuditoria> EventoAsync(Guid objeto, string accion)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        return await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .SingleAsync(a => a.ObjetoId == objeto && a.Accion == accion);
    }
}
