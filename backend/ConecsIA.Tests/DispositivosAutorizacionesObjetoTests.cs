using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Dispositivos de una persona (CRUD con inactivación) y autorizaciones de ingreso de vehículos y dispositivos:
/// ciclo Pendiente → Aprobada/Rechazada → Revocada, validaciones de vigencia, duplicados y estado del objeto,
/// aislamiento por organización, permisos por módulo y auditoría inmutable con actor tomado del servidor.
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

    // ---------- Autorizaciones de objetos ----------

    [Fact]
    public async Task Solicitar_y_aprobar_registra_actores_estados_y_no_autoriza_a_la_persona()
    {
        var solicitante = await Cliente("solicita");
        var admin = await Cliente("admina");
        var (persona, dispositivo) = await PersonaConDispositivoAsync(admin);

        var creada = await Leer(await solicitante.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo, area: _areaA)), HttpStatusCode.Created);
        var id = creada.GetProperty("idAutorizacionObjeto").GetInt32();
        Assert.Equal("Pendiente", creada.GetProperty("estado").GetString());
        Assert.Equal(Correo("solicita"), creada.GetProperty("solicitadoPor").GetString());

        // El solicitante sin permiso de edición no puede decidir.
        await Leer(await solicitante.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/aprobar", new { }), HttpStatusCode.Forbidden);

        var aprobada = await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/aprobar", new { idUsuarioDecide = 1 }), HttpStatusCode.OK);
        Assert.Equal("Aprobada", aprobada.GetProperty("estado").GetString());
        Assert.Equal("Vigente", aprobada.GetProperty("vigencia").GetString());
        Assert.Equal(Correo("admina"), aprobada.GetProperty("decididoPor").GetString());
        var idAdmin = await api.ScalarAsync<int>("SELECT id_usuario FROM usuario WHERE correo = @p0", Correo("admina"));
        Assert.Equal(idAdmin, await api.ScalarAsync<int>("SELECT id_usuario_decide FROM autorizacion_objeto WHERE id_autorizacion_objeto = @p0", id));

        // Historial: creación y aprobación con estado anterior y nuevo.
        var historial = aprobada.GetProperty("historial");
        Assert.Equal(2, historial.GetArrayLength());
        Assert.Equal(JsonValueKind.Null, historial[0].GetProperty("estadoAnterior").ValueKind);
        Assert.Equal("Pendiente", historial[0].GetProperty("estadoNuevo").GetString());
        Assert.Equal("Pendiente", historial[1].GetProperty("estadoAnterior").GetString());
        Assert.Equal("Aprobada", historial[1].GetProperty("estadoNuevo").GetString());
        Assert.Equal(Correo("admina"), historial[1].GetProperty("actor").GetString());
        Assert.Equal(_sedeA, await api.ScalarAsync<int>("SELECT DISTINCT id_sede FROM autorizacion_objeto_transicion WHERE id_autorizacion_objeto = @p0", id));

        // La autorización del objeto no crea autorización de ingreso de la persona.
        Assert.Equal(0, await api.ScalarAsync<int>("SELECT count(*)::int FROM autorizacion WHERE id_persona = @p0", persona));
        Assert.Equal(2, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'AutorizacionObjeto' AND id_registro = @p0", id.ToString()));
    }

    [Fact]
    public async Task Rechazar_y_revocar_exigen_motivo_y_respetan_las_transiciones()
    {
        var admin = await Cliente("admina");
        var (persona, dispositivo) = await PersonaConDispositivoAsync(admin);
        var id = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo)), HttpStatusCode.Created)).GetProperty("idAutorizacionObjeto").GetInt32();

        await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/rechazar", new { motivo = " " }), HttpStatusCode.BadRequest);
        // Revocar una pendiente no es una transición válida.
        await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/revocar", new { motivo = "x" }), HttpStatusCode.Conflict);
        var rechazada = await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/rechazar", new { motivo = "Equipo no requerido" }), HttpStatusCode.OK);
        Assert.Equal("Rechazada", rechazada.GetProperty("estado").GetString());
        Assert.Equal("Equipo no requerido", rechazada.GetProperty("motivoDecision").GetString());
        // Estado terminal: no se aprueba después.
        await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/aprobar", new { }), HttpStatusCode.Conflict);

        var otra = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo)), HttpStatusCode.Created)).GetProperty("idAutorizacionObjeto").GetInt32();
        await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{otra}/aprobar", new { }), HttpStatusCode.OK);
        var revocada = await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{otra}/revocar", new { motivo = "Salida del contratista" }), HttpStatusCode.OK);
        Assert.Equal("Revocada", revocada.GetProperty("estado").GetString());
        Assert.Equal("Salida del contratista", await api.ScalarAsync<string>(
            "SELECT motivo FROM autorizacion_objeto_transicion WHERE id_autorizacion_objeto = @p0 AND estado_nuevo = 'Revocada'", otra));
    }

    [Fact]
    public async Task No_se_admiten_autorizaciones_vencidas_duplicadas_ni_de_objetos_o_personas_inactivos()
    {
        var admin = await Cliente("admina");
        var (persona, dispositivo) = await PersonaConDispositivoAsync(admin);

        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo, inicio: EnDias(-10), fin: EnDias(-1))), HttpStatusCode.BadRequest);
        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo, inicio: EnDias(3), fin: EnDias(1))), HttpStatusCode.BadRequest);

        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo, fin: EnDias(5))), HttpStatusCode.Created);
        // Periodo superpuesto para el mismo objeto y sede: duplicada.
        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo, inicio: EnDias(2), fin: EnDias(8))), HttpStatusCode.Conflict);

        // Dispositivo de otra persona: incompatible.
        var (otraPersona, _) = await PersonaConDispositivoAsync(admin);
        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(otraPersona, dispositivo)), HttpStatusCode.BadRequest);

        // Dispositivo inactivo.
        var (persona2, dispositivo2) = await PersonaConDispositivoAsync(admin);
        await Leer(await admin.PatchAsJsonAsync($"/api/personas/{persona2}/dispositivos/{dispositivo2}/estado", new { activo = false }), HttpStatusCode.OK);
        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona2, dispositivo2)), HttpStatusCode.BadRequest);

        // Persona inactiva.
        var (persona3, dispositivo3) = await PersonaConDispositivoAsync(admin);
        await Leer(await admin.PatchAsJsonAsync($"/api/personas/{persona3}/estado", new { activo = false }), HttpStatusCode.OK);
        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona3, dispositivo3)), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Inactivar_el_dispositivo_conserva_sus_autorizaciones_e_impide_aprobar_pendientes()
    {
        var admin = await Cliente("admina");
        var (persona, dispositivo) = await PersonaConDispositivoAsync(admin);
        var id = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo)), HttpStatusCode.Created)).GetProperty("idAutorizacionObjeto").GetInt32();
        await Leer(await admin.PatchAsJsonAsync($"/api/personas/{persona}/dispositivos/{dispositivo}/estado", new { activo = false }), HttpStatusCode.OK);

        await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/aprobar", new { }), HttpStatusCode.Conflict);
        var consulta = await admin.GetFromJsonAsync<JsonElement>($"/api/autorizaciones-objeto/{id}");
        Assert.Equal("Pendiente", consulta.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Autorizacion_de_vehiculo_exige_vinculo_activo_con_la_persona()
    {
        var admin = await Cliente("admina");
        var persona = await CrearPersonaAsync(admin);
        var vehiculo = (await Leer(await admin.PostAsJsonAsync($"/api/personas/{persona}/vehiculos", new { placa = $"AB{Random.Shared.Next(1000, 9999)}", tipo = "Automóvil" }), HttpStatusCode.Created))
            .GetProperty("idVehiculo").GetInt32();
        var otra = await CrearPersonaAsync(admin);

        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(otra, vehiculo: vehiculo)), HttpStatusCode.BadRequest);
        var creada = await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, vehiculo: vehiculo)), HttpStatusCode.Created);
        Assert.Equal("Vehiculo", creada.GetProperty("tipoObjeto").GetString());
        Assert.StartsWith("AB", creada.GetProperty("objeto").GetString());
    }

    [Fact]
    public async Task Las_autorizaciones_se_aislan_por_organizacion_y_se_filtran()
    {
        var adminA = await Cliente("admina");
        var adminB = await Cliente("adminb");
        var (persona, dispositivo) = await PersonaConDispositivoAsync(adminA);
        var id = (await Leer(await adminA.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo)), HttpStatusCode.Created)).GetProperty("idAutorizacionObjeto").GetInt32();

        await Leer(await adminB.GetAsync($"/api/autorizaciones-objeto/{id}"), HttpStatusCode.NotFound);
        await Leer(await adminB.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/aprobar", new { }), HttpStatusCode.NotFound);
        // B no puede usar la sede de A ni la persona de A.
        var personaB = await CrearPersonaAsync(adminB, _tipoPersonaB);
        await Leer(await adminB.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo, sede: _sedeB)), HttpStatusCode.BadRequest);
        await Leer(await adminB.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(personaB, dispositivo, sede: _sedeB)), HttpStatusCode.BadRequest);
        Assert.Empty((await adminB.GetFromJsonAsync<JsonElement>("/api/autorizaciones-objeto")).EnumerateArray());

        var filtradas = await adminA.GetFromJsonAsync<JsonElement>($"/api/autorizaciones-objeto?idPersona={persona}&estado=Pendiente&idSede={_sedeA}&desde={Hoy}&hasta={EnDias(30)}");
        Assert.Single(filtradas.EnumerateArray());
        Assert.Empty((await adminA.GetFromJsonAsync<JsonElement>($"/api/autorizaciones-objeto?idPersona={persona}&estado=Aprobada")).EnumerateArray());
    }

    [Fact]
    public async Task La_auditoria_y_el_historial_son_inmutables()
    {
        var admin = await Cliente("admina");
        var (persona, dispositivo) = await PersonaConDispositivoAsync(admin);
        var id = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo)), HttpStatusCode.Created)).GetProperty("idAutorizacionObjeto").GetInt32();

        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => api.EjecutarAsync("UPDATE autorizacion_objeto_transicion SET estado_nuevo = 'Aprobada' WHERE id_autorizacion_objeto = @p0", id));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => api.EjecutarAsync("DELETE FROM autorizacion_objeto_transicion WHERE id_autorizacion_objeto = @p0", id));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => api.EjecutarAsync("UPDATE auditoria SET accion = 'X' WHERE entidad = 'AutorizacionObjeto' AND id_registro = @p0", id.ToString()));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => api.EjecutarAsync("DELETE FROM auditoria WHERE entidad = 'AutorizacionObjeto' AND id_registro = @p0", id.ToString()));
    }

    [Fact]
    public async Task Aprobaciones_concurrentes_registran_una_sola_decision()
    {
        var admin = await Cliente("admina");
        var (persona, dispositivo) = await PersonaConDispositivoAsync(admin);
        var id = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-objeto", Solicitud(persona, dispositivo)), HttpStatusCode.Created)).GetProperty("idAutorizacionObjeto").GetInt32();

        var otro = await Cliente("admina");
        var respuestas = await Task.WhenAll(
            admin.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/aprobar", new { }),
            otro.PostAsJsonAsync($"/api/autorizaciones-objeto/{id}/rechazar", new { motivo = "No" }));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM autorizacion_objeto_transicion WHERE id_autorizacion_objeto = @p0 AND estado_anterior = 'Pendiente'", id));
    }
}
