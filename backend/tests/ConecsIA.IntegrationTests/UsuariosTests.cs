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

/// <summary>HU-016 (usuarios por invitación) y HU-018 (alcance por sede) contra PostgreSQL real.</summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class UsuariosTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string Ruta = "/api/v1/usuarios";
    private const string Contrasena = "una-contraseña-larga-123";
    private FabricaApi fabrica = null!;
    private Escenario e = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    private async Task<(Guid Usuario, string Token)> InvitarAsync(string? correo = null, Guid[]? roles = null, Guid? como = null)
    {
        var respuesta = await fabrica.Como(como ?? e.AdminA).PostAsJsonAsync($"{Ruta}/invitaciones", new
        {
            correo = correo ?? $"invitada.{Guid.NewGuid():N}@ejemplo.test",
            nombre = "Persona Invitada",
            roles = roles ?? [e.RolA1],
            alcanceTotalSedes = false,
            sedes = new[] { e.SedeA1 },
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var enlace = cuerpo.GetProperty("enlaceAceptacion").GetString()!;
        return (cuerpo.GetProperty("usuario").GetProperty("usuarioId").GetGuid(), enlace[(enlace.LastIndexOf('/') + 1)..]);
    }

    private Task<HttpResponseMessage> AceptarAsync(string token) =>
        fabrica.CreateClient().PostAsJsonAsync("/api/v1/invitaciones/aceptar", new { token, contrasena = Contrasena });

    private async Task<JsonElement> UsuarioAsync(Guid usuario) =>
        await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>($"{Ruta}/{usuario}");

    // ---------- HU-016 ----------

    [Fact]
    public async Task Invitar_crea_una_membresia_pendiente_auditada_sin_exponer_el_token_en_la_auditoria()
    {
        var (usuario, token) = await InvitarAsync();

        var detalle = await UsuarioAsync(usuario);
        Assert.Equal("Pendiente", detalle.GetProperty("estado").GetString());

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var evento = await contexto.Set<EventoAuditoria>().SingleAsync(a => a.ObjetoId == usuario && a.Accion == "usuario.invitar");
        Assert.DoesNotContain(token, evento.ValoresNuevos ?? "", StringComparison.Ordinal);
        Assert.False(await contexto.Set<Invitacion>().AnyAsync(i => i.HashToken == token), "El token no se guarda en claro.");
    }

    [Fact]
    public async Task Aceptar_define_la_contrasena_y_activa_la_membresia()
    {
        var (usuario, token) = await InvitarAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await AceptarAsync(token)).StatusCode);

        Assert.Equal("Activo", (await UsuarioAsync(usuario)).GetProperty("estado").GetString());
        var me = await fabrica.Como(usuario).GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.Equal(e.OrgA, me.GetProperty("organizacion").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Un_token_ya_usado_responde_410()
    {
        var (_, token) = await InvitarAsync();
        await AceptarAsync(token);

        Assert.Equal(HttpStatusCode.Gone, (await AceptarAsync(token)).StatusCode);
    }

    [Fact]
    public async Task Un_token_vencido_o_inexistente_responde_410()
    {
        var (usuario, token) = await InvitarAsync();
        await using (var conexion = new NpgsqlConnection(baseDatos.CadenaApp))
        {
            await conexion.OpenAsync();
            await using var vencer = new NpgsqlCommand("UPDATE invitacion SET expira_en = now() - interval '1 minute' WHERE usuario_id = @u", conexion);
            vencer.Parameters.AddWithValue("u", usuario);
            await vencer.ExecuteNonQueryAsync();
        }

        Assert.Equal(HttpStatusCode.Gone, (await AceptarAsync(token)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await AceptarAsync("token-que-no-existe")).StatusCode);
    }

    [Fact]
    public async Task Aceptaciones_concurrentes_del_mismo_token_dejan_una_sola_aceptacion()
    {
        var (usuario, token) = await InvitarAsync();

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => AceptarAsync(token)));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.NoContent),
            r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Gone, HttpStatusCode.Conflict }));
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        Assert.Equal(1, await contexto.Set<Membresia>().CountAsync(m => m.UsuarioId == usuario && m.OrganizacionId == e.OrgA));
    }

    [Fact]
    public async Task Un_correo_que_ya_es_miembro_de_la_organizacion_responde_409()
    {
        var correo = $"repetida.{Guid.NewGuid():N}@ejemplo.test";
        await InvitarAsync(correo);

        var repetida = await fabrica.Como(e.AdminA).PostAsJsonAsync($"{Ruta}/invitaciones", new
        {
            correo, nombre = "Otra vez", roles = Array.Empty<Guid>(), alcanceTotalSedes = false, sedes = Array.Empty<Guid>(),
        });

        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
    }

    [Fact]
    public async Task Un_usuario_de_otra_organizacion_recibe_una_membresia_nueva_D03b()
    {
        string correoB;
        await using (var ambito = fabrica.Services.CreateAsyncScope())
        {
            ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
            correoB = (await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<Usuario>().SingleAsync(u => u.Id == e.AdminB)).Correo;
        }

        var (usuario, _) = await InvitarAsync(correoB);

        Assert.Equal(e.AdminB, usuario);
    }

    [Fact]
    public async Task Nadie_asigna_roles_con_permisos_que_no_tiene()
    {
        var limitado = await UsuarioConPermisosAsync(Permisos.UsuariosConsultar, Permisos.UsuariosInvitar, Permisos.UsuariosAdministrar);
        Guid rolPoderoso;
        await using (var ambito = fabrica.Services.CreateAsyncScope())
        {
            ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
            var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
            var rol = Rol.Crear(e.OrgA, $"Poderoso {Guid.NewGuid():N}"[..20], null, [Permisos.SedesCrear], null, DateTimeOffset.UtcNow);
            contexto.Add(rol);
            await contexto.SaveChangesAsync();
            rolPoderoso = rol.Id;
        }

        var invitar = await fabrica.Como(limitado).PostAsJsonAsync($"{Ruta}/invitaciones", new
        {
            correo = $"x.{Guid.NewGuid():N}@ejemplo.test", nombre = "X", roles = new[] { rolPoderoso },
            alcanceTotalSedes = false, sedes = Array.Empty<Guid>(),
        });
        var asignar = await fabrica.Como(limitado).PutAsJsonAsync($"{Ruta}/{e.OperadorA}/roles", new { roles = new[] { rolPoderoso } });

        Assert.Equal(HttpStatusCode.Forbidden, invitar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, asignar.StatusCode);
    }

    [Fact]
    public async Task Cambiar_roles_rige_en_la_siguiente_peticion_y_se_audita_con_anterior_y_nuevo()
    {
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.SinPermisosA).GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.SinPermisosA).GetAsync("/api/v1/sedes")).StatusCode);

        var respuesta = await fabrica.Como(e.AdminA).PutAsJsonAsync($"{Ruta}/{e.SinPermisosA}/roles", new { roles = new[] { e.RolA1 } });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.SinPermisosA).GetAsync("/api/v1/sedes")).StatusCode);
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var evento = await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .SingleAsync(a => a.ObjetoId == e.SinPermisosA && a.Accion == "usuario.roles");
        Assert.NotNull(evento.ValoresAnteriores);
        Assert.Contains(e.RolA1.ToString(), evento.ValoresNuevos, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Un_rol_de_otra_organizacion_no_se_puede_asignar()
    {
        var respuesta = await fabrica.Como(e.AdminA).PutAsJsonAsync($"{Ruta}/{e.OperadorA}/roles", new { roles = new[] { e.RolB1 } });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Inactivar_corta_el_acceso_revoca_sesiones_y_conserva_el_usuario_y_su_autoria()
    {
        string selloAntes;
        await using (var ambito = fabrica.Services.CreateAsyncScope())
        {
            ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
            selloAntes = (await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<Usuario>().SingleAsync(u => u.Id == e.OperadorA)).SelloSeguridad;
        }

        var respuesta = await fabrica.Como(e.AdminA).PostAsync($"{Ruta}/{e.OperadorA}/inactivar", null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.OperadorA).GetAsync("/api/v1/sedes")).StatusCode);
        Assert.Equal("Inactivo", (await UsuarioAsync(e.OperadorA)).GetProperty("estado").GetString());
        await using var verificacion = fabrica.Services.CreateAsyncScope();
        verificacion.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var usuario = await verificacion.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<Usuario>().SingleAsync(u => u.Id == e.OperadorA);
        Assert.NotEqual(selloAntes, usuario.SelloSeguridad);

        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.AdminA).PostAsync($"{Ruta}/{e.OperadorA}/reactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.Como(e.OperadorA).GetAsync("/api/v1/sedes")).StatusCode);
    }

    [Fact]
    public async Task El_administrador_de_A_no_ve_ni_administra_usuarios_de_B()
    {
        var lista = await fabrica.Como(e.AdminA).GetFromJsonAsync<JsonElement>(Ruta);
        var ids = lista.EnumerateArray().Select(u => u.GetProperty("usuarioId").GetGuid()).ToList();

        Assert.Contains(e.OperadorA, ids);
        Assert.DoesNotContain(e.AdminB, ids);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).GetAsync($"{Ruta}/{e.AdminB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.Como(e.AdminA).PostAsync($"{Ruta}/{e.AdminB}/inactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await fabrica.Como(e.AdminA).PutAsJsonAsync($"{Ruta}/{e.AdminB}/roles", new { roles = Array.Empty<Guid>() })).StatusCode);
    }

    // ---------- HU-018 ----------

    private Task<HttpResponseMessage> AlcanceAsync(Guid como, Guid usuario, bool total, params Guid[] sedes) =>
        fabrica.Como(como).PutAsJsonAsync($"{Ruta}/{usuario}/alcance", new { alcanceTotalSedes = total, sedes });

    [Fact]
    public async Task El_alcance_asignado_filtra_las_sedes_visibles()
    {
        Assert.Equal(HttpStatusCode.OK, (await AlcanceAsync(e.AdminA, e.OperadorA, false, e.SedeA2)).StatusCode);

        var sedes = await fabrica.Como(e.OperadorA).GetFromJsonAsync<JsonElement>("/api/v1/sedes?tamano=100");
        Assert.Equal([e.SedeA2], sedes.GetProperty("elementos").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, (await fabrica.Como(e.OperadorA).GetAsync($"/api/v1/sedes/{e.SedeA1}")).StatusCode);
    }

    [Fact]
    public async Task Un_alcance_vacio_no_da_acceso_territorial()
    {
        await AlcanceAsync(e.AdminA, e.OperadorA, false);

        var sedes = await fabrica.Como(e.OperadorA).GetFromJsonAsync<JsonElement>("/api/v1/sedes?tamano=100");
        Assert.Empty(sedes.GetProperty("elementos").EnumerateArray());
    }

    [Fact]
    public async Task Un_administrador_de_sede_solo_asigna_dentro_de_su_propio_alcance()
    {
        var adminSede = await UsuarioConPermisosAsync([e.SedeA1], Permisos.UsuariosConsultar, Permisos.UsuariosAdministrar);

        Assert.Equal(HttpStatusCode.Forbidden, (await AlcanceAsync(adminSede, e.OperadorA, false, e.SedeA2)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AlcanceAsync(adminSede, e.OperadorA, true)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AlcanceAsync(adminSede, e.OperadorA, false, e.SedeA1)).StatusCode);
    }

    [Fact]
    public async Task Una_sede_de_otra_organizacion_en_el_alcance_responde_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await AlcanceAsync(e.AdminA, e.OperadorA, false, e.SedeB1)).StatusCode);
    }

    [Fact]
    public async Task El_cambio_de_alcance_se_audita()
    {
        await AlcanceAsync(e.AdminA, e.OperadorA, false, e.SedeA2);

        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var evento = await ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>().Set<EventoAuditoria>()
            .SingleAsync(a => a.ObjetoId == e.OperadorA && a.Accion == "usuario.alcance");
        Assert.Contains(e.SedeA1.ToString(), evento.ValoresAnteriores, StringComparison.Ordinal);
        Assert.Contains(e.SedeA2.ToString(), evento.ValoresNuevos, StringComparison.Ordinal);
    }

    private Task<Guid> UsuarioConPermisosAsync(params string[] permisos) => UsuarioConPermisosAsync(null, permisos);

    private async Task<Guid> UsuarioConPermisosAsync(Guid[]? sedes, params string[] permisos)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        var usuario = Usuario.Crear($"limitado.{Guid.NewGuid():N}@ejemplo.test", "Usuario limitado");
        contexto.Add(usuario);
        contexto.Add(Membresia.Crear(usuario.Id, e.OrgA, permisos, alcanceTotalSedes: sedes is null, sedesAsignadas: sedes ?? []));
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }
}
