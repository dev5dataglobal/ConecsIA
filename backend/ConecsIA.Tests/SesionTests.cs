using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ConecsIA.Application.Interfaces;
using ConecsIA.Tests.Infraestructura;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Tests;

/// <summary>
/// Sesión renovable («Recordarme»), aplicación de permisos de perfil al iniciar y al renovar sesión,
/// y recuperación de contraseña por correo.
/// </summary>
public sealed class SesionTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Cabecera = "X-Conecsia-Sesion";
    private int _org, _rolAdmin, _rolOperador;
    private string _sufijo = "";

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _org = await api.CrearOrganizacionAsync($"Org sesion {_sufijo}");
        _rolAdmin = await api.CrearRolAsync(_org, "Administrador");
        _rolOperador = await api.CrearRolAsync(_org, "Operador");
        foreach (var modulo in new[] { "usuarios", "perfiles", "dashboard" }) await api.AsignarModuloAsync(_org, _rolAdmin, modulo);
        await api.CrearUsuarioAsync(Correo("admin"), _org, _rolAdmin);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient cliente, string correo, bool recordarme, string? contrasena = null) =>
        await cliente.PostAsJsonAsync("/api/auth/login", new { correo, password = contrasena ?? ApiFactory.Contrasena, recordarme });

    private static async Task<string> TokenAsync(HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;

    private static HttpRequestMessage Renovar() => new(HttpMethod.Post, "/api/auth/renovar") { Headers = { { Cabecera, "1" } } };

    private static string? CookieSesion(HttpResponseMessage respuesta) =>
        respuesta.Headers.TryGetValues("Set-Cookie", out var valores) ? valores.FirstOrDefault(x => x.StartsWith("conecsia_sesion=", StringComparison.Ordinal)) : null;

    private static async Task<string[]> MisModulosAsync(HttpClient cliente, string token)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/app-modulos/mios");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await cliente.SendAsync(peticion);
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement[]>())!.Select(x => x.GetProperty("codigo").GetString()!).ToArray();
    }

    // ---------- Recordarme ----------

    [Fact]
    public async Task Login_sin_recordarme_emite_cookie_de_sesion_httponly_sin_vencimiento()
    {
        var respuesta = await LoginAsync(api.CreateClient(), Correo("admin"), recordarme: false);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cookie = CookieSesion(respuesta)!.ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("path=/api/auth", cookie);
        Assert.DoesNotContain("expires=", cookie);
    }

    [Fact]
    public async Task Login_con_recordarme_emite_cookie_persistente()
    {
        var respuesta = await LoginAsync(api.CreateClient(), Correo("admin"), recordarme: true);

        var cookie = CookieSesion(respuesta)!.ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("expires=", cookie);
    }

    [Fact]
    public async Task Renovar_rota_la_cookie_y_cerrar_sesion_la_invalida()
    {
        var cliente = api.CreateClient();
        await LoginAsync(cliente, Correo("admin"), recordarme: true);

        var renovada = await cliente.SendAsync(Renovar());
        Assert.Equal(HttpStatusCode.OK, renovada.StatusCode);
        Assert.NotNull(CookieSesion(renovada));
        Assert.NotEmpty(await TokenAsync(renovada));

        var cierre = await cliente.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/auth/cerrar-sesion") { Headers = { { Cabecera, "1" } } });
        Assert.Equal(HttpStatusCode.NoContent, cierre.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.SendAsync(Renovar())).StatusCode);
    }

    [Fact]
    public async Task Renovar_exige_la_cabecera_de_sesion_y_una_cookie()
    {
        var cliente = api.CreateClient();
        await LoginAsync(cliente, Correo("admin"), recordarme: false);

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsync("/api/auth/renovar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().SendAsync(Renovar())).StatusCode);
    }

    [Fact]
    public async Task Cookie_rotada_reutilizada_fuera_de_la_rotacion_no_crea_otra_sesion()
    {
        // Simula dos pestañas: la segunda llega con la cookie anterior justo después de la rotación.
        var cliente = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var login = await LoginAsync(cliente, Correo("admin"), recordarme: true);
        var original = CookieSesion(login)!.Split(';')[0];

        using var primera = Renovar();
        primera.Headers.Add("Cookie", original);
        var rotada = await cliente.SendAsync(primera);
        Assert.Equal(HttpStatusCode.OK, rotada.StatusCode);
        Assert.NotNull(CookieSesion(rotada));

        using var segunda = Renovar();
        segunda.Headers.Add("Cookie", original);
        var tardia = await cliente.SendAsync(segunda);
        Assert.Equal(HttpStatusCode.OK, tardia.StatusCode);
        Assert.Null(CookieSesion(tardia));
    }

    // ---------- Permisos del perfil ----------

    [Fact]
    public async Task Usuario_con_perfil_recibe_sus_modulos_al_iniciar_sesion()
    {
        await api.AsignarModuloAsync(_org, _rolOperador, "usuarios", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("operador"), _org, _rolOperador);

        var cliente = api.CreateClient();
        var modulos = await MisModulosAsync(cliente, await TokenAsync(await LoginAsync(cliente, Correo("operador"), false)));

        Assert.Equal(["usuarios"], modulos);
    }

    [Fact]
    public async Task Permisos_modificados_en_el_perfil_se_aplican_a_la_sesion_activa_y_al_renovar()
    {
        var rol = await api.CrearRolAsync(_org, $"Recepción {_sufijo}");
        await api.CrearUsuarioAsync(Correo("recepcion"), _org, rol);
        var cliente = api.CreateClient();
        var token = await TokenAsync(await LoginAsync(cliente, Correo("recepcion"), false));
        Assert.Empty(await MisModulosAsync(cliente, token));

        // El administrador otorga el módulo desde Perfiles (API real: invalida la caché de accesos).
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var idModulo = await api.ScalarAsync<int>("SELECT id_app_modulo FROM app_modulos WHERE codigo = 'dashboard'");
        var guardado = await admin.PutAsJsonAsync($"/api/roles/{rol}/modulos",
            new { asignaciones = new[] { new { idAppModulo = idModulo, consultar = true, anadir = false, editar = false } } });
        Assert.Equal(HttpStatusCode.OK, guardado.StatusCode);

        // El mismo token ya lo recibe (el acceso por módulo se valida en cada petición)…
        Assert.Equal(["dashboard"], await MisModulosAsync(cliente, token));
        // …y el token renovado también.
        var renovado = await TokenAsync(await cliente.SendAsync(Renovar()));
        Assert.Equal(["dashboard"], await MisModulosAsync(cliente, renovado));
    }

    // ---------- Recuperación de contraseña ----------

    private sealed class CorreoFalso : IEnviadorCorreo
    {
        public List<(string Destinatario, string Texto)> Enviados { get; } = [];
        public bool Configurado => true;

        public Task EnviarAsync(string destinatario, string asunto, string cuerpoTexto, string cuerpoHtml, CancellationToken cancellationToken = default)
        {
            Enviados.Add((destinatario, cuerpoTexto));
            return Task.CompletedTask;
        }
    }

    private (HttpClient Cliente, CorreoFalso Correo) ClienteConCorreo()
    {
        var correo = new CorreoFalso();
        var cliente = api.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<IEnviadorCorreo>(correo))).CreateClient();
        return (cliente, correo);
    }

    private static string TokenDelEnlace(string texto) => Uri.UnescapeDataString(Regex.Match(texto, "#token=([A-Za-z0-9_%-]+)").Groups[1].Value);

    [Fact]
    public async Task Recuperar_sin_correo_configurado_responde_503_sin_simular_el_envio()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/auth/recuperar-contrasena", new { correo = Correo("admin") });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal(0, await api.ScalarAsync<int>(
            "SELECT count(*)::int FROM recuperacion_contrasena r JOIN usuario u ON u.id_usuario = r.id_usuario WHERE u.correo = @p0", Correo("admin")));
    }

    [Fact]
    public async Task Recuperar_responde_igual_exista_o_no_la_cuenta()
    {
        var (cliente, correo) = ClienteConCorreo();

        var existente = await cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new { correo = Correo("admin") });
        var inexistente = await cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new { correo = Correo("nadie") });

        Assert.Equal(HttpStatusCode.Accepted, existente.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, inexistente.StatusCode);
        Assert.Equal(await existente.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync());
        Assert.Single(correo.Enviados);
    }

    [Fact]
    public async Task Restablecer_cambia_la_contrasena_una_sola_vez_y_cierra_las_sesiones()
    {
        await api.CrearUsuarioAsync(Correo("olvido"), _org, _rolOperador);
        var (cliente, correo) = ClienteConCorreo();
        await LoginAsync(cliente, Correo("olvido"), recordarme: true);

        await cliente.PostAsJsonAsync("/api/auth/recuperar-contrasena", new { correo = Correo("olvido") });
        var token = TokenDelEnlace(correo.Enviados.Single().Texto);
        Assert.Equal(0, await api.ScalarAsync<int>("SELECT count(*)::int FROM recuperacion_contrasena WHERE token_hash = @p0", token));

        var debil = await cliente.PostAsJsonAsync("/api/auth/restablecer-contrasena", new { token, nuevaContrasena = "solominusculas", confirmacionContrasena = "solominusculas" });
        Assert.Equal(HttpStatusCode.BadRequest, debil.StatusCode);
        var distinta = await cliente.PostAsJsonAsync("/api/auth/restablecer-contrasena", new { token, nuevaContrasena = "Nueva-Clave-123", confirmacionContrasena = "Otra-Clave-123" });
        Assert.Equal(HttpStatusCode.BadRequest, distinta.StatusCode);

        var ok = await cliente.PostAsJsonAsync("/api/auth/restablecer-contrasena", new { token, nuevaContrasena = "Nueva-Clave-123", confirmacionContrasena = "Nueva-Clave-123" });
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);

        var reutilizado = await cliente.PostAsJsonAsync("/api/auth/restablecer-contrasena", new { token, nuevaContrasena = "Otra-Clave-456", confirmacionContrasena = "Otra-Clave-456" });
        Assert.Equal(HttpStatusCode.BadRequest, reutilizado.StatusCode);

        // La sesión «Recordarme» abierta antes del cambio ya no se renueva.
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.SendAsync(Renovar())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(api.CreateClient(), Correo("olvido"), false)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(api.CreateClient(), Correo("olvido"), false, "Nueva-Clave-123")).StatusCode);
    }

    [Fact]
    public async Task Restablecer_con_token_desconocido_falla()
    {
        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/auth/restablecer-contrasena",
            new { token = "token-inexistente", nuevaContrasena = "Nueva-Clave-123", confirmacionContrasena = "Nueva-Clave-123" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
