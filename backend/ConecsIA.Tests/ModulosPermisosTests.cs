using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Gestión dinámica de acceso a módulos (app_modulos / modulos_permisos).
/// Escenario: dos organizaciones (A y B); en A un administrador de organización y un operador.
/// </summary>
public sealed class ModulosPermisosTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _rolAdminA, _rolOperadorA, _rolOperadorB;
    private string _sufijo = "";

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");
        _rolAdminA = await api.CrearRolAsync(_orgA, "Administrador de organización");
        _rolOperadorA = await api.CrearRolAsync(_orgA, "Operador");
        _rolOperadorB = await api.CrearRolAsync(_orgB, "Operador B");
        await api.AsignarModuloAsync(_orgA, _rolAdminA, "perfiles");
        await api.AsignarModuloAsync(_orgA, _rolAdminA, "personas");
        await api.CrearUsuarioAsync(Correo("admin"), _orgA, _rolAdminA);
        await api.CrearUsuarioAsync(Correo("operador"), _orgA, _rolOperadorA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";

    private Task<int> IdModulo(string codigo) => api.ScalarAsync<int>("SELECT id_app_modulo FROM app_modulos WHERE codigo = @p0", codigo);

    private Task<int> Asignaciones(int idRol) => api.ScalarAsync<int>("SELECT count(*)::int FROM modulos_permisos WHERE id_rol = @p0", idRol);

    [Fact]
    public async Task Catalogo_inicial_contiene_los_modulos_reales_sin_duplicados()
    {
        var codigos = await api.ScalarAsync<string>("SELECT string_agg(codigo, ',' ORDER BY codigo) FROM app_modulos WHERE activo");
        Assert.Equal("alertas,auditoria,autorizaciones,configuracion,control_accesos,dashboard,dispositivos,organizaciones,perfiles,personas,sedes,usuarios,vehiculos,visitas,zonas_empresariales", codigos);
        // «administracion» se conserva inactivo (se dividió en cuatro módulos).
        Assert.False(await api.ScalarAsync<bool>("SELECT activo FROM app_modulos WHERE codigo = 'administracion'"));
    }

    [Fact]
    public async Task La_base_impide_asignaciones_duplicadas_y_referencias_inexistentes()
    {
        var idModulo = await IdModulo("visitas");
        await api.EjecutarAsync("INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en) VALUES (@p0, @p1, @p2, now())", _orgA, _rolOperadorA, idModulo);

        await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            api.EjecutarAsync("INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en) VALUES (@p0, @p1, @p2, now())", _orgA, _rolOperadorA, idModulo));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            api.EjecutarAsync("INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en) VALUES (@p0, @p1, 999999, now())", _orgA, _rolOperadorA));
        // El rol debe pertenecer a la organización indicada en la asignación.
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
            api.EjecutarAsync("INSERT INTO modulos_permisos (id_organizacion, id_rol, id_app_modulo, asignado_en) VALUES (@p0, @p1, @p2, now())", _orgA, _rolOperadorB, idModulo));
    }

    [Fact]
    public async Task Sin_asignacion_el_acceso_al_modulo_se_rechaza_aunque_se_llame_la_api_directamente()
    {
        var operador = await api.ClienteAutenticadoAsync(Correo("operador"));

        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/Persona")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/dashboard")).StatusCode);
        var mios = await operador.GetFromJsonAsync<JsonElement>("/api/app-modulos/mios");
        Assert.Equal(0, mios.GetArrayLength());
    }

    [Fact]
    public async Task Asignar_un_modulo_habilita_el_acceso_de_inmediato_y_retirarlo_lo_revoca()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var operador = await api.ClienteAutenticadoAsync(Correo("operador"));
        var idPersonas = await IdModulo("personas");
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/Persona")).StatusCode);

        var asignar = await admin.PostAsync($"/api/roles/{_rolOperadorA}/modulos/{idPersonas}", null);
        Assert.Equal(HttpStatusCode.NoContent, asignar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await operador.GetAsync("/api/Persona")).StatusCode);
        var mios = await operador.GetFromJsonAsync<JsonElement>("/api/app-modulos/mios");
        Assert.Contains(mios.EnumerateArray(), m => m.GetProperty("codigo").GetString() == "personas");

        var duplicado = await admin.PostAsync($"/api/roles/{_rolOperadorA}/modulos/{idPersonas}", null);
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var retirar = await admin.DeleteAsync($"/api/roles/{_rolOperadorA}/modulos/{idPersonas}");
        Assert.Equal(HttpStatusCode.NoContent, retirar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/Persona")).StatusCode);

        var auditadas = await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'modulos_permisos' AND id_organizacion = @p0", _orgA);
        Assert.True(auditadas >= 2);
    }

    [Fact]
    public async Task Reemplazar_asignaciones_es_atomico_y_valida_todos_los_modulos()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var idPersonas = await IdModulo("personas");

        var invalido = await admin.PutAsJsonAsync($"/api/roles/{_rolOperadorA}/modulos", new { idsModulos = new[] { idPersonas, 999999 } });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.Equal(0, await Asignaciones(_rolOperadorA));

        var valido = await admin.PutAsJsonAsync($"/api/roles/{_rolOperadorA}/modulos", new { idsModulos = new[] { idPersonas, idPersonas } });
        Assert.Equal(HttpStatusCode.OK, valido.StatusCode);
        var asignados = await admin.GetFromJsonAsync<JsonElement>($"/api/roles/{_rolOperadorA}/modulos");
        Assert.Equal(["personas"], asignados.EnumerateArray().Select(m => m.GetProperty("codigo").GetString()));
    }

    [Fact]
    public async Task Un_administrador_de_organizacion_no_gestiona_roles_de_otra_organizacion()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var idPersonas = await IdModulo("personas");

        var respuesta = await admin.PostAsync($"/api/roles/{_rolOperadorB}/modulos/{idPersonas}", null);
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/roles/{_rolOperadorB}/modulos")).StatusCode);
        Assert.Equal(0, await Asignaciones(_rolOperadorB));
    }

    [Fact]
    public async Task Un_administrador_de_organizacion_no_puede_otorgar_modulos_que_el_mismo_no_tiene()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var idAuditoria = await IdModulo("auditoria");

        var respuesta = await admin.PostAsync($"/api/roles/{_rolOperadorA}/modulos/{idAuditoria}", null);
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(0, await Asignaciones(_rolOperadorA));
    }

    [Fact]
    public async Task Solo_el_administrador_general_modifica_el_catalogo_global()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var operador = await api.ClienteAutenticadoAsync(Correo("operador"));
        var nuevo = new { codigo = $"prueba_{_sufijo}", nombre = "Módulo de prueba", orden = 99 };

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/app-modulos", nuevo)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.PostAsJsonAsync("/api/app-modulos", nuevo)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync("/api/app-modulos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/app-modulos")).StatusCode);
    }

    [Fact]
    public async Task El_administrador_general_gestiona_el_catalogo_y_accede_sin_asignaciones()
    {
        // El usuario 1 se crea una sola vez por base temporal.
        if (await api.ScalarAsync<int>("SELECT count(*)::int FROM usuario WHERE id_usuario = 1") == 0)
            await api.CrearUsuarioConIdAsync(1, $"plataforma.{_sufijo}@ejemplo.test", _orgA);
        var correo = await api.ScalarAsync<string>("SELECT correo FROM usuario WHERE id_usuario = 1");
        var plataforma = await api.ClienteAutenticadoAsync(correo!);

        var crear = await plataforma.PostAsJsonAsync("/api/app-modulos", new { codigo = $"prueba_{_sufijo}", nombre = "Módulo de prueba", orden = 99 });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var creado = await crear.Content.ReadFromJsonAsync<JsonElement>();
        var id = creado.GetProperty("idAppModulo").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await plataforma.PostAsJsonAsync("/api/app-modulos", new { codigo = $"prueba_{_sufijo}", nombre = "Otro" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await plataforma.PutAsJsonAsync($"/api/app-modulos/{id}", new { codigo = "otro_codigo", nombre = "Cambio" })).StatusCode);

        var inactivar = await plataforma.PatchAsJsonAsync($"/api/app-modulos/{id}/estado", new { activo = false });
        Assert.Equal(HttpStatusCode.OK, inactivar.StatusCode);
        Assert.False(await api.ScalarAsync<bool>("SELECT activo FROM app_modulos WHERE id_app_modulo = @p0", id));

        Assert.Equal(HttpStatusCode.OK, (await plataforma.GetAsync("/api/Persona")).StatusCode);
        var mios = await plataforma.GetFromJsonAsync<JsonElement>("/api/app-modulos/mios");
        Assert.DoesNotContain(mios.EnumerateArray(), m => m.GetProperty("codigo").GetString() == $"prueba_{_sufijo}");
    }

    [Fact]
    public async Task Un_operador_sin_el_modulo_de_perfiles_no_gestiona_asignaciones()
    {
        var operador = await api.ClienteAutenticadoAsync(Correo("operador"));
        var idPersonas = await IdModulo("personas");

        Assert.Equal(HttpStatusCode.Forbidden, (await operador.PostAsync($"/api/roles/{_rolOperadorA}/modulos/{idPersonas}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await operador.GetAsync($"/api/roles/{_rolOperadorA}/modulos")).StatusCode);
    }
}
