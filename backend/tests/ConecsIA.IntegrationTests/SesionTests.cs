using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.IntegrationTests.Infraestructura;
using ConecsIA.Modules.Auditoria.Domain;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.IntegrationTests;

/// <summary>
/// HU-006 contra PostgreSQL real con la cookie de sesión (ADR-006, opción A): inicio y cierre,
/// respuesta genérica, bloqueo 5 / 15 min, revocación por sello de seguridad y auditoría sin secretos.
/// </summary>
[Collection(ColeccionBaseDatos.Nombre)]
public sealed class SesionTests(BaseDatosFixture baseDatos) : IAsyncLifetime
{
    private const string Contrasena = "una-frase-larga-de-prueba";
    private FabricaApi fabrica = null!;
    private Escenario e = null!;
    private string correoAdminA = null!;

    public async Task InitializeAsync()
    {
        fabrica = new FabricaApi(baseDatos);
        e = await Escenario.CrearAsync(fabrica);
        correoAdminA = await ModificarUsuarioAsync(e.AdminA, (u, h) => u.EstablecerContrasena(h.Hashear(Contrasena)));
    }

    public async Task DisposeAsync() => await fabrica.DisposeAsync();

    [Fact]
    public async Task Credenciales_validas_emiten_cookie_protegida_y_me_devuelve_el_contexto()
    {
        var cliente = Navegador();

        var respuesta = await IngresarAsync(cliente, correoAdminA, Contrasena);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var cookie = Assert.Single(respuesta.Headers.GetValues("Set-Cookie"), c => c.StartsWith("conecsia.sesion=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var me = await cliente.GetFromJsonAsync<JsonElement>("/api/v1/me");
        Assert.Equal(e.AdminA, me.GetProperty("usuario").GetProperty("id").GetGuid());
        Assert.Equal(e.OrgA, me.GetProperty("organizacion").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Contrasena_incorrecta_y_usuario_inexistente_responden_igual()
    {
        var incorrecta = await IngresarAsync(Navegador(), correoAdminA, "otra-contrasena-cualquiera");
        var inexistente = await IngresarAsync(Navegador(), $"nadie.{Guid.NewGuid():N}@ejemplo.test", Contrasena);

        Assert.Equal(HttpStatusCode.Unauthorized, incorrecta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(await incorrecta.Content.ReadAsStringAsync().ContinueWith(t => SinTraza(t.Result)),
            await inexistente.Content.ReadAsStringAsync().ContinueWith(t => SinTraza(t.Result)));
        Assert.False(incorrecta.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Cinco_fallos_bloquean_la_cuenta_aunque_luego_la_contrasena_sea_correcta()
    {
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await IngresarAsync(Navegador(), correoAdminA, "incorrecta-" + i)).StatusCode);
        }

        var respuesta = await IngresarAsync(Navegador(), correoAdminA, Contrasena);

        Assert.Equal(HttpStatusCode.Locked, respuesta.StatusCode);
        var eventos = await EventosAsync(e.AdminA);
        Assert.Contains(eventos, ev => ev.Accion == ServicioIngreso.AccionBloqueo);
    }

    [Fact]
    public async Task Inactivar_al_usuario_invalida_la_sesion_en_la_siguiente_peticion()
    {
        var cliente = Navegador();
        await IngresarAsync(cliente, correoAdminA, Contrasena);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/me")).StatusCode);

        await ModificarUsuarioAsync(e.AdminA, (u, _) => u.Inactivar());

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/v1/me")).StatusCode);
    }

    [Fact]
    public async Task Cambiar_la_contrasena_revoca_las_sesiones_previas()
    {
        var cliente = Navegador();
        await IngresarAsync(cliente, correoAdminA, Contrasena);

        await ModificarUsuarioAsync(e.AdminA, (u, h) => u.EstablecerContrasena(h.Hashear("otra-frase-larga-nueva")));

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/v1/me")).StatusCode);
    }

    [Fact]
    public async Task Usuario_sin_membresia_activa_inicia_sesion_pero_no_obtiene_contexto()
    {
        var correo = await ModificarUsuarioAsync(e.UsuarioInactivo, (u, h) => u.EstablecerContrasena(h.Hashear(Contrasena)));
        var sinMembresia = Usuario.Crear($"solo.{Guid.NewGuid():N}@ejemplo.test", "Sin membresía");
        await GuardarAsync(c => c.Add(sinMembresia));
        await ModificarUsuarioAsync(sinMembresia.Id, (u, h) => u.EstablecerContrasena(h.Hashear(Contrasena)));
        var cliente = Navegador();

        Assert.Equal(HttpStatusCode.Unauthorized, (await IngresarAsync(Navegador(), correo, Contrasena)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await IngresarAsync(cliente, sinMembresia.Correo, Contrasena)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/v1/me")).StatusCode);
    }

    [Fact]
    public async Task Cerrar_sesion_retira_la_cookie_y_queda_auditado()
    {
        var cliente = Navegador();
        await IngresarAsync(cliente, correoAdminA, Contrasena);

        var respuesta = await PostConCsrfAsync(cliente, "/api/v1/auth/logout");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/v1/me")).StatusCode);
        Assert.Contains(await EventosAsync(e.AdminA), ev => ev.Accion == ServicioIngreso.AccionCierre);
    }

    [Fact]
    public async Task La_auditoria_de_sesion_no_contiene_contrasenas_ni_cookies()
    {
        var cliente = Navegador();
        await IngresarAsync(cliente, correoAdminA, "intento-fallido-secreto");
        var respuesta = await IngresarAsync(cliente, correoAdminA, Contrasena);
        var cookie = respuesta.Headers.GetValues("Set-Cookie").First().Split(';')[0].Split('=', 2)[1];

        var serializados = (await EventosDeObjetoAsync(e.AdminA))
            .Select(ev => $"{ev.ValoresAnteriores}|{ev.ValoresNuevos}")
            .ToList();

        Assert.NotEmpty(serializados);
        Assert.All(serializados, s =>
        {
            Assert.DoesNotContain(Contrasena, s, StringComparison.Ordinal);
            Assert.DoesNotContain("intento-fallido-secreto", s, StringComparison.Ordinal);
            Assert.DoesNotContain(cookie, s, StringComparison.Ordinal);
            Assert.DoesNotContain(correoAdminA, s, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Con_la_cookie_de_sesion_las_escrituras_sin_token_csrf_se_rechazan()
    {
        var cliente = Navegador();
        await IngresarAsync(cliente, correoAdminA, Contrasena);

        var logout = await cliente.PostAsync("/api/v1/auth/logout", content: null);
        var sede = await cliente.PostAsJsonAsync("/api/v1/sedes", new { nombre = "Sede CSRF", zonaHoraria = "America/Bogota" });

        Assert.Equal(HttpStatusCode.Forbidden, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, sede.StatusCode);
        Assert.Equal("CSRF_INVALIDO", (await sede.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/v1/me")).StatusCode);
    }

    [Fact]
    public async Task Con_token_csrf_ligado_a_la_sesion_la_escritura_se_acepta()
    {
        var cliente = Navegador();
        await IngresarAsync(cliente, correoAdminA, Contrasena);

        var respuesta = await PostConCsrfAsync(cliente, "/api/v1/sedes",
            JsonContent.Create(new { nombre = $"Sede {Guid.NewGuid():N}"[..20], zonaHoraria = "America/Bogota" }));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_token_csrf_de_otra_sesion_no_sirve()
    {
        var anonimo = Navegador();
        var ajeno = (await anonimo.GetFromJsonAsync<JsonElement>("/api/v1/auth/csrf")).GetProperty("token").GetString();
        var cliente = Navegador();
        await IngresarAsync(cliente, correoAdminA, Contrasena);

        var solicitud = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        solicitud.Headers.Add("X-CSRF-TOKEN", ajeno);

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.SendAsync(solicitud)).StatusCode);
    }

    [Fact]
    public async Task Sesiones_simultaneas_se_listan_y_revocar_una_no_afecta_a_las_demas()
    {
        var portatil = Navegador();
        var celular = Navegador();
        await IngresarAsync(portatil, correoAdminA, Contrasena);
        await IngresarAsync(celular, correoAdminA, Contrasena);

        var sesiones = await portatil.GetFromJsonAsync<JsonElement[]>("/api/v1/auth/sesiones");
        Assert.Equal(2, sesiones!.Length);
        var otra = Assert.Single(sesiones, s => !s.GetProperty("actual").GetBoolean()).GetProperty("id").GetGuid();

        var revocar = await EnviarConCsrfAsync(portatil, HttpMethod.Delete, $"/api/v1/auth/sesiones/{otra}");

        Assert.Equal(HttpStatusCode.NoContent, revocar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await celular.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await portatil.GetAsync("/api/v1/me")).StatusCode);
        Assert.Contains(await EventosAsync(e.AdminA), ev => ev.Accion == ServicioIngreso.AccionRevocar && ev.ObjetoId == otra);
    }

    [Fact]
    public async Task Tras_cerrar_sesion_una_copia_de_la_cookie_ya_no_sirve()
    {
        var cliente = Navegador();
        var ingreso = await IngresarAsync(cliente, correoAdminA, Contrasena);
        var cookie = ingreso.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("conecsia.sesion=", StringComparison.Ordinal)).Split(';')[0];

        await PostConCsrfAsync(cliente, "/api/v1/auth/logout");

        var copia = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var solicitud = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        solicitud.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await copia.SendAsync(solicitud)).StatusCode);
    }

    [Fact]
    public async Task No_se_puede_revocar_la_sesion_de_otro_usuario()
    {
        var correoB = await ModificarUsuarioAsync(e.AdminB, (u, h) => u.EstablecerContrasena(h.Hashear(Contrasena)));
        var clienteB = Navegador();
        await IngresarAsync(clienteB, correoB, Contrasena);
        var sesionB = (await clienteB.GetFromJsonAsync<JsonElement[]>("/api/v1/auth/sesiones"))!.Single().GetProperty("id").GetGuid();
        var clienteA = Navegador();
        await IngresarAsync(clienteA, correoAdminA, Contrasena);

        var respuesta = await EnviarConCsrfAsync(clienteA, HttpMethod.Delete, $"/api/v1/auth/sesiones/{sesionB}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clienteB.GetAsync("/api/v1/me")).StatusCode);
    }

    private static Task<HttpResponseMessage> PostConCsrfAsync(HttpClient cliente, string ruta, HttpContent? contenido = null) =>
        EnviarConCsrfAsync(cliente, HttpMethod.Post, ruta, contenido);

    private static async Task<HttpResponseMessage> EnviarConCsrfAsync(HttpClient cliente, HttpMethod metodo, string ruta, HttpContent? contenido = null)
    {
        var token = (await cliente.GetFromJsonAsync<JsonElement>("/api/v1/auth/csrf")).GetProperty("token").GetString();
        var solicitud = new HttpRequestMessage(metodo, ruta) { Content = contenido };
        solicitud.Headers.Add("X-CSRF-TOKEN", token);
        return await cliente.SendAsync(solicitud);
    }

    private HttpClient Navegador() =>
        fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    private static Task<HttpResponseMessage> IngresarAsync(HttpClient cliente, string correo, string contrasena) =>
        cliente.PostAsJsonAsync("/api/v1/auth/login", new { correo, contrasena });

    private static string SinTraza(string cuerpo)
    {
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(cuerpo)!;
        json.Remove("traceId");
        return JsonSerializer.Serialize(json);
    }

    private async Task<string> ModificarUsuarioAsync(Guid id, Action<Usuario, IHasherContrasenas> cambio)
    {
        string correo = null!;
        await GuardarAsync(c => { }, async (c, servicios) =>
        {
            var usuario = await c.Set<Usuario>().SingleAsync(u => u.Id == id);
            cambio(usuario, servicios.GetRequiredService<IHasherContrasenas>());
            correo = usuario.Correo;
        });
        return correo;
    }

    private async Task GuardarAsync(Action<ConecsIADbContext> cambio, Func<ConecsIADbContext, IServiceProvider, Task>? cambioAsync = null)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        cambio(contexto);
        if (cambioAsync is not null) await cambioAsync(contexto, ambito.ServiceProvider);
        await contexto.SaveChangesAsync();
    }

    private Task<List<EventoAuditoria>> EventosAsync(Guid usuario) =>
        ConsultarEventosAsync(ev => ev.ObjetoId == usuario || ev.ActorUsuarioId == usuario);

    private Task<List<EventoAuditoria>> EventosDeObjetoAsync(Guid usuario) =>
        ConsultarEventosAsync(ev => ev.ObjetoId == usuario);

    private async Task<List<EventoAuditoria>> ConsultarEventosAsync(System.Linq.Expressions.Expression<Func<EventoAuditoria, bool>> filtro)
    {
        await using var ambito = fabrica.Services.CreateAsyncScope();
        ambito.ServiceProvider.GetRequiredService<ContextoSolicitud>().ComoSistema();
        var contexto = ambito.ServiceProvider.GetRequiredService<ConecsIADbContext>();
        return await contexto.Set<EventoAuditoria>().AsNoTracking().Where(filtro).ToListAsync();
    }
}
