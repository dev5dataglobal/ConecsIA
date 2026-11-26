using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Dispositivos de una persona (CRUD con inactivación), aislamiento por organización y permisos por módulo. Las
/// autorizaciones de ingreso de vehículos y dispositivos se prueban con el registro de acceso unificado (AccesosTests, ADR-029).
/// </summary>
public sealed class DispositivosAutorizacionesObjetoTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _tipoPersonaA, _tipoPersonaB, _cc, _sedeA, _areaA, _sedeB;
    private string _sufijo = "";
    private static string Hoy => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
    private static string EnDias(int dias) => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(dias).ToString("yyyy-MM-dd");

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");
        foreach (var (org, nombre) in new[] { (_orgA, "admina"), (_orgB, "adminb") })
        {
            var rol = await api.CrearRolAsync(org, $"Admin {nombre}");
            foreach (var modulo in new[] { "personas", "vehiculos", "dispositivos", "autorizaciones" }) await api.AsignarModuloAsync(org, rol, modulo);
            await api.CrearUsuarioAsync(Correo(nombre), org, rol);
        }
        // Solicitante: puede registrar y solicitar, pero no decidir (Autorizaciones sin Editar).
        var rolSolicita = await api.CrearRolAsync(_orgA, "Solicitante");
        await api.AsignarModuloAsync(_orgA, rolSolicita, "personas", consultar: true, anadir: false, editar: false);
        await api.AsignarModuloAsync(_orgA, rolSolicita, "vehiculos", consultar: true, anadir: true, editar: false);
        await api.AsignarModuloAsync(_orgA, rolSolicita, "dispositivos", consultar: true, anadir: true, editar: false);
        await api.AsignarModuloAsync(_orgA, rolSolicita, "autorizaciones", consultar: true, anadir: true, editar: false);
        await api.CrearUsuarioAsync(Correo("solicita"), _orgA, rolSolicita);
        // Sin módulo de dispositivos.
        var rolSinDispositivos = await api.CrearRolAsync(_orgA, "Sin dispositivos");
        await api.AsignarModuloAsync(_orgA, rolSinDispositivos, "personas");
        await api.AsignarModuloAsync(_orgA, rolSinDispositivos, "autorizaciones");
        await api.CrearUsuarioAsync(Correo("sindispositivos"), _orgA, rolSinDispositivos);

        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersonaA = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Visitante', true) RETURNING id_tipo_persona", _orgA);
        _tipoPersonaB = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Visitante', true) RETURNING id_tipo_persona", _orgB);
        var ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad");
        _sedeA = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede A', 'UTC', true) RETURNING id_sede", _orgA, ciudad);
        _areaA = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Recepción', false, true) RETURNING id_area", _orgA, _sedeA);
        _sedeB = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede B', 'UTC', true) RETURNING id_sede", _orgB, ciudad);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));
    private static string Documento() => Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString();
    private static string Serial() => $"SN-{Guid.NewGuid():N}"[..14].ToUpperInvariant();

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(esperado == respuesta.StatusCode, $"Se esperaba {(int)esperado} y llegó {(int)respuesta.StatusCode}: {cuerpo}");
        return cuerpo.Length == 0 ? default : JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private async Task<int> CrearPersonaAsync(HttpClient cliente, int? tipo = null, object? idUsuarioCrea = null)
    {
        var persona = await Leer(await cliente.PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = tipo ?? _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = Documento(),
            nombres = "Luis", apellidos = "Pérez", correo = "luis@ejemplo.test", idUsuarioCrea,
        }), HttpStatusCode.Created);
        return persona.GetProperty("idPersona").GetInt32();
    }

    private static async Task<int> CrearTipoDispositivoAsync(HttpClient cliente, string nombre)
    {
        var tipo = await Leer(await cliente.PostAsJsonAsync("/api/dispositivos/tipos", new { nombre }), HttpStatusCode.Created);
        return tipo.GetProperty("idTipoDispositivo").GetInt32();
    }

    private async Task<(int Persona, int Dispositivo)> PersonaConDispositivoAsync(HttpClient cliente)
    {
        var persona = await CrearPersonaAsync(cliente);
        var tipo = await CrearTipoDispositivoAsync(cliente, $"Portátil {Guid.NewGuid():N}"[..20]);
        var dispositivo = await Leer(await cliente.PostAsJsonAsync($"/api/personas/{persona}/dispositivos", new
        {
            idTipoDispositivo = tipo, codigoIdentificador = Serial(), marca = "Dell", modelo = "Latitude", color = "Gris",
        }), HttpStatusCode.Created);
        return (persona, dispositivo.GetProperty("idDispositivo").GetInt32());
    }

    private object Solicitud(int persona, int? dispositivo = null, int? vehiculo = null, string? inicio = null, string? fin = null, int? sede = null, int? area = null) => new
    {
        idPersona = persona,
        tipoObjeto = dispositivo is null ? "Vehiculo" : "Dispositivo",
        idDispositivo = dispositivo,
        idVehiculo = vehiculo,
        idSede = sede ?? _sedeA,
        idArea = area,
        fechaInicio = inicio ?? Hoy,
        fechaFin = fin ?? EnDias(5),
        horaInicio = "07:00",
        horaFin = "18:00",
        motivo = "Mantenimiento de equipos",
    };

    // ---------- Persona: quién la registró ----------

    [Fact]
    public async Task El_registrador_de_la_persona_sale_de_la_identidad_autenticada_y_no_del_cliente()
    {
        var admin = await Cliente("admina");
        var idAdmin = await api.ScalarAsync<int>("SELECT id_usuario FROM usuario WHERE correo = @p0", Correo("admina"));
        var persona = await CrearPersonaAsync(admin, idUsuarioCrea: 1);

        Assert.Equal(idAdmin, await api.ScalarAsync<int>("SELECT id_usuario_crea FROM persona WHERE id_persona = @p0", persona));
        var detalle = await admin.GetFromJsonAsync<JsonElement>($"/api/personas/{persona}");
        Assert.Equal(Correo("admina"), detalle.GetProperty("registradoPor").GetString());

        // Editar enviando otro registrador no lo cambia.
        await Leer(await admin.PutAsJsonAsync($"/api/personas/{persona}", new
        {
            idTipoPersona = _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = detalle.GetProperty("numeroDocumento").GetString(),
            nombres = "Luis", apellidos = "Pérez", correo = "luis@ejemplo.test", idUsuarioCrea = 1,
        }), HttpStatusCode.OK);
        Assert.Equal(idAdmin, await api.ScalarAsync<int>("SELECT id_usuario_crea FROM persona WHERE id_persona = @p0", persona));
    }

    // ---------- Dispositivos ----------

    [Fact]
    public async Task Una_persona_tiene_varios_dispositivos_de_su_organizacion_y_se_inactivan_sin_borrarse()
    {
        var admin = await Cliente("admina");
        var (persona, primero) = await PersonaConDispositivoAsync(admin);
        var tipo = await CrearTipoDispositivoAsync(admin, "Tablet");
        var segundo = await Leer(await admin.PostAsJsonAsync($"/api/personas/{persona}/dispositivos", new
        {
            idTipoDispositivo = tipo, codigoIdentificador = "  tab-001 ", descripcion = "Tablet de inventario", idOrganizacion = _orgB, idPersonaResponsable = 999,
        }), HttpStatusCode.Created);
        Assert.Equal("TAB-001", segundo.GetProperty("codigoIdentificador").GetString());
        var idSegundo = segundo.GetProperty("idDispositivo").GetInt32();
        Assert.Equal(_orgA, await api.ScalarAsync<int>("SELECT id_organizacion FROM dispositivo WHERE id_dispositivo = @p0", idSegundo));
        Assert.Equal(persona, await api.ScalarAsync<int>("SELECT id_persona_responsable FROM dispositivo WHERE id_dispositivo = @p0", idSegundo));

        var lista = await admin.GetFromJsonAsync<JsonElement>($"/api/personas/{persona}/dispositivos");
        Assert.Equal(2, lista.GetArrayLength());

        // Editar.
        await Leer(await admin.PutAsJsonAsync($"/api/personas/{persona}/dispositivos/{idSegundo}", new { idTipoDispositivo = tipo, codigoIdentificador = "TAB-001", color = "Negro" }), HttpStatusCode.OK);
        Assert.Equal("Negro", await api.ScalarAsync<string>("SELECT color FROM dispositivo WHERE id_dispositivo = @p0", idSegundo));

        // Inactivar conserva la fila.
        var inactivo = await Leer(await admin.PatchAsJsonAsync($"/api/personas/{persona}/dispositivos/{primero}/estado", new { activo = false }), HttpStatusCode.OK);
        Assert.False(inactivo.GetProperty("activo").GetBoolean());
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM dispositivo WHERE id_dispositivo = @p0", primero));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'Dispositivo' AND accion = 'Inactivar' AND id_registro = @p0", primero.ToString()));
    }

    [Fact]
    public async Task El_dispositivo_valida_obligatorios_duplicados_y_organizacion()
    {
        var adminA = await Cliente("admina");
        var adminB = await Cliente("adminb");
        var persona = await CrearPersonaAsync(adminA);
        var tipo = await CrearTipoDispositivoAsync(adminA, "Cámara");
        var tipoB = await CrearTipoDispositivoAsync(adminB, "Cámara");

        await Leer(await adminA.PostAsJsonAsync($"/api/personas/{persona}/dispositivos", new { idTipoDispositivo = tipo, codigoIdentificador = "" }), HttpStatusCode.BadRequest);
        // Tipo de otra organización: no se acepta.
        await Leer(await adminA.PostAsJsonAsync($"/api/personas/{persona}/dispositivos", new { idTipoDispositivo = tipoB, codigoIdentificador = "CAM-1" }), HttpStatusCode.BadRequest);
        var serial = Serial();
        await Leer(await adminA.PostAsJsonAsync($"/api/personas/{persona}/dispositivos", new { idTipoDispositivo = tipo, codigoIdentificador = serial }), HttpStatusCode.Created);
        await Leer(await adminA.PostAsJsonAsync($"/api/personas/{persona}/dispositivos", new { idTipoDispositivo = tipo, codigoIdentificador = serial.ToLowerInvariant() }), HttpStatusCode.Conflict);

        // B no ve ni asocia dispositivos a personas de A.
        await Leer(await adminB.GetAsync($"/api/personas/{persona}/dispositivos"), HttpStatusCode.NotFound);
        await Leer(await adminB.PostAsJsonAsync($"/api/personas/{persona}/dispositivos", new { idTipoDispositivo = tipoB, codigoIdentificador = "X-1" }), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sin_modulo_de_dispositivos_no_se_gestionan_dispositivos()
    {
        var admin = await Cliente("admina");
        var (persona, _) = await PersonaConDispositivoAsync(admin);
        var sin = await Cliente("sindispositivos");
        await Leer(await sin.GetAsync($"/api/personas/{persona}/dispositivos"), HttpStatusCode.Forbidden);
    }
}
