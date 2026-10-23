using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ConecsIA.Tests.Infraestructura;

/// <summary>
/// Levanta la API contra una base PostgreSQL temporal (se crea con las migraciones y se elimina al final).
/// Conexión base: variable CONECSIA_TEST_CONNECTION o la DefaultConnection de appsettings.Local.json.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Contrasena = "Prueba-Segura-123";
    private readonly string _baseDatos = $"conecsia_test_{Guid.NewGuid():N}";
    private readonly string _conexion;

    public ApiFactory()
    {
        var baseConexion = Environment.GetEnvironmentVariable("CONECSIA_TEST_CONNECTION") ?? LeerConexionLocal();
        _conexion = new NpgsqlConnectionStringBuilder(baseConexion) { Database = _baseDatos, Pooling = false }.ConnectionString;
    }

    public string Conexion => _conexion;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // UseSetting se aplica al crear el builder: Program.cs lee la conexión y JWT antes de Build().
        builder.UseSetting("ConnectionStrings:Conecsia", _conexion);
        builder.UseSetting("JWT:Key", "clave-de-pruebas-de-integracion-0123456789");
        builder.UseSetting("JWT:Issuer", "ConecsIA.Tests");
        builder.UseSetting("JWT:Audience", "ConecsIA.Tests");
        builder.UseSetting("JWT:DurationInMinutes", "30");
        builder.ConfigureServices(services =>
            services.Configure<RateLimiterOptions>(options => options.GlobalLimiter = null));
    }

    public async Task InitializeAsync()
    {
        var opciones = new DbContextOptionsBuilder<ConecsiaDbContext>().UseNpgsql(_conexion).Options;
        await using var db = new ConecsiaDbContext(opciones);
        await db.Database.MigrateAsync();
        // El usuario 1 es el administrador general (regla vigente): los usuarios de prueba empiezan después.
        await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('public.usuario', 'id_usuario'), 1000)");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        var admin = new NpgsqlConnectionStringBuilder(_conexion) { Database = "postgres" }.ConnectionString;
        await using var conexion = new NpgsqlConnection(admin);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_baseDatos}\" WITH (FORCE)", conexion);
        await comando.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string sql, params object[] parametros)
    {
        await using var conexion = new NpgsqlConnection(_conexion);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        for (var i = 0; i < parametros.Length; i++) comando.Parameters.AddWithValue($"p{i}", parametros[i]);
        var valor = await comando.ExecuteScalarAsync();
        return valor is null or DBNull ? default : (T)Convert.ChangeType(valor, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    public Task<int?> EjecutarAsync(string sql, params object[] parametros) => ScalarAsync<int?>(sql, parametros);

    /// <summary>Crea una organización con tipo y plan mínimos.</summary>
    public async Task<int> CrearOrganizacionAsync(string nombre)
    {
        var tipo = await ScalarAsync<int>("INSERT INTO tipo_organizacion (nombre, activo) VALUES (@p0, true) RETURNING id_tipo_organizacion", $"Tipo {nombre}");
        var plan = await ScalarAsync<int>("INSERT INTO plan (nombre, activo) VALUES (@p0, true) RETURNING id_plan", $"Plan {nombre}");
        return await ScalarAsync<int>(
            "INSERT INTO organizacion (id_tipo_organizacion, id_plan, nombre, estado, creado_en) VALUES (@p0, @p1, @p2, 'Activa', now()) RETURNING id_organizacion",
            tipo, plan, nombre);
    }

    public Task<int> CrearRolAsync(int idOrganizacion, string nombre) =>
        ScalarAsync<int>("INSERT INTO rol (id_organizacion, nombre, activo) VALUES (@p0, @p1, true) RETURNING id_rol", idOrganizacion, nombre);

    public async Task AsignarPermisoAsync(int idOrganizacion, int idRol, string codigo)
    {
        var idPermiso = await ScalarAsync<int?>("SELECT id_permiso FROM permiso WHERE codigo = @p0", codigo)
            ?? await ScalarAsync<int>("INSERT INTO permiso (codigo, activo) VALUES (@p0, true) RETURNING id_permiso", codigo);
        await EjecutarAsync("INSERT INTO rol_permiso (id_organizacion, id_rol, id_permiso) VALUES (@p0, @p1, @p2)", idOrganizacion, idRol, idPermiso);
    }

    /// <summary>Asignación directa en BD (preparación de escenarios, no pasa por la API).</summary>
    public async Task AsignarModuloAsync(int idOrganizacion, int idRol, string codigoModulo)
    {
        var idModulo = await ScalarAsync<int>("SELECT id_app_modulo FROM app_modulos WHERE codigo = @p0", codigoModulo);
        await EjecutarAsync(
            "INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en) VALUES (@p0, @p1, @p2, now())",
            idOrganizacion, idRol, idModulo);
    }

    public async Task<int> CrearUsuarioAsync(string correo, int idOrganizacion, params int[] roles) =>
        await CrearUsuarioConIdAsync(null, correo, idOrganizacion, roles);

    /// <summary>Con <paramref name="idUsuario"/> = 1 crea el administrador general.</summary>
    public async Task<int> CrearUsuarioConIdAsync(int? idUsuario, string correo, int idOrganizacion, params int[] roles)
    {
        var hash = new PasswordHasher<Usuario>().HashPassword(new Usuario(), Contrasena);
        idUsuario = idUsuario is null
            ? await ScalarAsync<int>(
                "INSERT INTO usuario (correo, correo_normalizado, password_hash, sello_seguridad, intentos_fallidos, activo, creado_en) VALUES (@p0, @p1, @p2, @p3, 0, true, now()) RETURNING id_usuario",
                correo, correo.Trim().ToUpperInvariant(), hash, Guid.NewGuid().ToString())
            : await ScalarAsync<int>(
                "INSERT INTO usuario (id_usuario, correo, correo_normalizado, password_hash, sello_seguridad, intentos_fallidos, activo, creado_en) VALUES (@p4, @p0, @p1, @p2, @p3, 0, true, now()) RETURNING id_usuario",
                correo, correo.Trim().ToUpperInvariant(), hash, Guid.NewGuid().ToString(), idUsuario.Value);
        var idMembresia = await ScalarAsync<int>(
            "INSERT INTO membresia_organizacion (id_organizacion, id_usuario, estado, creado_en) VALUES (@p0, @p1, 'Activa', now()) RETURNING id_membresia",
            idOrganizacion, idUsuario.Value);
        foreach (var rol in roles)
            await EjecutarAsync("INSERT INTO membresia_rol (id_organizacion, id_membresia, id_rol) VALUES (@p0, @p1, @p2)", idOrganizacion, idMembresia, rol);
        return idUsuario.Value;
    }

    public async Task<HttpClient> ClienteAutenticadoAsync(string correo)
    {
        var cliente = CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { correo, password = Contrasena });
        if (!respuesta.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login fallido ({(int)respuesta.StatusCode}): {await respuesta.Content.ReadAsStringAsync()}");
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo.GetProperty("accessToken").GetString());
        return cliente;
    }

    private static string LeerConexionLocal([System.Runtime.CompilerServices.CallerFilePath] string archivo = "")
    {
        // Se parte de la ubicación del código fuente porque los binarios pueden compilarse fuera del repositorio.
        var raiz = Path.GetDirectoryName(archivo);
        while (raiz is not null && !Directory.Exists(Path.Combine(raiz, "ApiConecsia"))) raiz = Path.GetDirectoryName(raiz);
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(raiz ?? ".", "ApiConecsia", "appsettings.Local.json"), optional: true)
            .Build();
        return config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Defina CONECSIA_TEST_CONNECTION o appsettings.Local.json para las pruebas de integración.");
    }
}
