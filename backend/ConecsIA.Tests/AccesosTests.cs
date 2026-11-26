using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiConecsia.Services;
using ConecsIA.Domain.Entities;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Módulo unificado de control de accesos (ADR-029): un solo punto de creación con categoría, tipo de permanencia y
/// horario; flujo automático por categoría; decisión independiente de vehículos; entrada bloqueada y auditada fuera de
/// horario o vigencia (o alertada si la organización lo configura); episodio, permanencia y salida; idempotencia; cupo
/// único; bitácora inmutable; aislamiento entre organizaciones; y las reglas puras de horario y vigencia.
/// </summary>
public sealed class AccesosTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _sedeA, _areaA, _puntoA, _membresiaAdmin, _membresiaAnfitrion, _tipoPersona, _cc;
    private int _catVisitante, _catRecurrente, _catAutomatica, _sedeA2, _areaA2, _puntoA2;
    private string _sufijo = "";
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);
    private static string EnDias(int dias) => Hoy.AddDays(dias).ToString("yyyy-MM-dd");
    private static int DiaIso(DateOnly d) => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");

        var rolAdmin = await api.CrearRolAsync(_orgA, "Administrador");
        foreach (var m in new[] { "personas", "control_accesos", "autorizaciones", "vehiculos", "configuracion" }) await api.AsignarModuloAsync(_orgA, rolAdmin, m);
        await api.CrearUsuarioAsync(Correo("admin"), _orgA, rolAdmin);
        // Portería: crea registros y registra movimientos, no decide.
        var rolPorteria = await api.CrearRolAsync(_orgA, "Portería");
        foreach (var m in new[] { "personas", "control_accesos" }) await api.AsignarModuloAsync(_orgA, rolPorteria, m);
        await api.CrearUsuarioAsync(Correo("porteria"), _orgA, rolPorteria);
        // Anfitrión: solo consulta; decide los registros de categorías con flujo Anfitrion donde es el responsable.
        var rolAnfitrion = await api.CrearRolAsync(_orgA, "Anfitrión");
        await api.AsignarModuloAsync(_orgA, rolAnfitrion, "control_accesos", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("anfitrion"), _orgA, rolAnfitrion);
        // Solo entradas: Control de accesos · Consultar y Añadir, sin Editar (no registra salidas ni cancela).
        var rolEntradas = await api.CrearRolAsync(_orgA, "Solo entradas");
        await api.AsignarModuloAsync(_orgA, rolEntradas, "control_accesos", consultar: true, anadir: true, editar: false);
        await api.CrearUsuarioAsync(Correo("entradas"), _orgA, rolEntradas);
        // Operadora de otra sede: alcance limitado por membresia_sede.
        await api.CrearUsuarioAsync(Correo("operadora2"), _orgA, rolPorteria);
        var rolB = await api.CrearRolAsync(_orgB, "Administrador B");
        foreach (var m in new[] { "personas", "control_accesos", "autorizaciones" }) await api.AsignarModuloAsync(_orgB, rolB, m);
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, rolB);

        var ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad");
        _sedeA = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede A', 'UTC', true) RETURNING id_sede", _orgA, ciudad);
        _areaA = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Gerencia', false, true) RETURNING id_area", _orgA, _sedeA);
        _puntoA = await api.ScalarAsync<int>("INSERT INTO punto_acceso (id_organizacion, id_sede, nombre, modo_acceso, activo, creado_en) VALUES (@p0, @p1, 'Portería', 'Ambos', true, now()) RETURNING id_punto_acceso", _orgA, _sedeA);
        _sedeA2 = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede A2', 'UTC', true) RETURNING id_sede", _orgA, ciudad);
        _areaA2 = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Bodega', false, true) RETURNING id_area", _orgA, _sedeA2);
        _puntoA2 = await api.ScalarAsync<int>("INSERT INTO punto_acceso (id_organizacion, id_sede, nombre, modo_acceso, activo, creado_en) VALUES (@p0, @p1, 'Portería 2', 'Ambos', true, now()) RETURNING id_punto_acceso", _orgA, _sedeA2);
        await api.EjecutarAsync("INSERT INTO membresia_sede (id_organizacion, id_membresia, id_sede) VALUES (@p0, @p1, @p2)", _orgA, await Membresia(Correo("operadora2")), _sedeA2);
        _membresiaAdmin = await Membresia(Correo("admin"));
        _membresiaAnfitrion = await Membresia(Correo("anfitrion"));
        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersona = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Visitante', true) RETURNING id_tipo_persona", _orgA);
        _catVisitante = await Categoria(_orgA, "VISITANTE", "Visitante", "Anfitrion", "{Unica,Diaria,Temporal}");
        _catRecurrente = await Categoria(_orgA, "RECURRENTE", "Contratista", "Seguridad", "{Recurrente,Permanente}");
        _catAutomatica = await Categoria(_orgA, "DOMICILIARIO", "Domiciliario", "Automatico", "{Unica,Temporal}");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- Apoyo ----------

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));
    private Task<int> Membresia(string correo) =>
        api.ScalarAsync<int>("SELECT m.id_membresia FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario WHERE u.correo = @p0", correo);
    private Task<int> Categoria(int org, string codigo, string nombre, string flujo, string modalidades) =>
        api.ScalarAsync<int>("""
            INSERT INTO categoria_acceso (id_organizacion, codigo, nombre, flujo_aprobacion, modalidades_permitidas, exige_anfitrion, exige_motivo, permite_vehiculos, permite_dispositivos, orden, activo, creado_en)
            VALUES (@p0, @p1, @p2, @p3, @p4::text[], true, true, true, true, 0, true, now()) RETURNING id_categoria_acceso
            """, org, codigo, nombre, flujo, modalidades);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(esperado == respuesta.StatusCode, $"Se esperaba {(int)esperado} y llegó {(int)respuesta.StatusCode}: {cuerpo}");
        return cuerpo.Length == 0 ? default : JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private async Task<int> PersonaAsync(HttpClient cliente)
    {
        var persona = await Leer(await cliente.PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = _tipoPersona, idTipoDocumento = _cc, numeroDocumento = Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString(),
            nombres = "Ana", apellidos = $"Visitante {_sufijo}", correo = "ana@ejemplo.test",
        }), HttpStatusCode.Created);
        return persona.GetProperty("idPersona").GetInt32();
    }

    private Dictionary<string, object?> Registro(int persona, int categoria, string modalidad, params (string Clave, object? Valor)[] cambios)
    {
        var r = new Dictionary<string, object?>
        {
            ["idPersona"] = persona, ["idCategoriaAcceso"] = categoria, ["idSede"] = _sedeA, ["idArea"] = _areaA,
            ["idMembresiaResponsable"] = _membresiaAdmin, ["modalidad"] = modalidad, ["fechaInicio"] = EnDias(0), ["motivo"] = "Reunión de seguimiento",
        };
        foreach (var (clave, valor) in cambios) r[clave] = valor;
        return r;
    }

    private static object TodoElDia(params int[] dias) => dias.Select(d => new { diaSemana = d, horaInicio = "00:00:00", horaFin = "23:59:59" }).ToArray();

    private static async Task<int> Crear(HttpClient cliente, object registro) =>
        (await Leer(await cliente.PostAsJsonAsync("/api/accesos", registro), HttpStatusCode.Created)).GetProperty("idAutorizacion").GetInt32();

    private static Task<HttpResponseMessage> Decidir(HttpClient cliente, int id, string accion, string? motivo = null) =>
        cliente.PostAsJsonAsync($"/api/accesos/{id}/{accion}", new { motivo });

    private static Task<HttpResponseMessage> Movimiento(HttpClient cliente, string ruta, int punto, string? clave = null)
    {
        var mensaje = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = JsonContent.Create(new { idPuntoAcceso = punto }) };
        mensaje.Headers.Add("Idempotency-Key", clave ?? Guid.NewGuid().ToString());
        return cliente.SendAsync(mensaje);
    }

    // ---------- Creación por tipo de permanencia ----------

    [Fact]
    public async Task Crea_accesos_temporal_recurrente_y_permanente_con_su_horario_desde_un_solo_endpoint()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync(admin);

        var temporal = await Leer(await admin.PostAsJsonAsync("/api/accesos", Registro(persona, _catVisitante, "Temporal",
            ("fechaFin", EnDias(1)), ("horaInicio", "08:00:00"), ("horaFin", "17:00:00"))), HttpStatusCode.Created);
        Assert.Equal("Pendiente", temporal.GetProperty("estado").GetString());
        Assert.Equal("Visitante", temporal.GetProperty("categoria").GetString());

        var persona2 = await PersonaAsync(admin);
        var recurrente = await Leer(await admin.PostAsJsonAsync("/api/accesos", Registro(persona2, _catRecurrente, "Recurrente",
            ("fechaFin", EnDias(30)), ("franjas", new[] { new { diaSemana = 1, horaInicio = "07:00:00", horaFin = "12:00:00" }, new { diaSemana = 1, horaInicio = "13:00:00", horaFin = "17:00:00" } }))),
            HttpStatusCode.Created);
        Assert.Equal(2, recurrente.GetProperty("horario").GetArrayLength());

        // Franjas que se solapan: 400 por FluentValidation.
        var solapadas = await admin.PostAsJsonAsync("/api/accesos", Registro(await PersonaAsync(admin), _catRecurrente, "Recurrente",
            ("franjas", new[] { new { diaSemana = 2, horaInicio = "07:00:00", horaFin = "12:00:00" }, new { diaSemana = 2, horaInicio = "11:00:00", horaFin = "15:00:00" } })));
        await Leer(solapadas, HttpStatusCode.BadRequest);

        // Permanente: bloqueado por la política hasta habilitarla en la configuración.
        var persona3 = await PersonaAsync(admin);
        await Leer(await admin.PostAsJsonAsync("/api/accesos", Registro(persona3, _catRecurrente, "Permanente")), HttpStatusCode.Conflict);
        await Leer(await admin.PutAsJsonAsync("/api/accesos/configuracion", new { permitePermanente = true }), HttpStatusCode.OK);
        var permanente = await Leer(await admin.PostAsJsonAsync("/api/accesos", Registro(persona3, _catRecurrente, "Permanente", ("usaHorarioBase", true))), HttpStatusCode.Created);
        Assert.Null(permanente.GetProperty("fechaFin").GetString());

        // La categoría limita los tipos de permanencia.
        await Leer(await admin.PostAsJsonAsync("/api/accesos", Registro(await PersonaAsync(admin), _catVisitante, "Recurrente", ("franjas", TodoElDia(1)))), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Categoria_con_flujo_automatico_nace_aprobada_sin_decisor_humano()
    {
        var porteria = await Cliente("porteria");
        var persona = await PersonaAsync(porteria);
        var registro = await Leer(await porteria.PostAsJsonAsync("/api/accesos", Registro(persona, _catAutomatica, "Unica")), HttpStatusCode.Created);
        Assert.Equal("Aprobada", registro.GetProperty("estado").GetString());
        Assert.Equal("Politica", registro.GetProperty("origen").GetString());
        var id = registro.GetProperty("idAutorizacion").GetInt32();
        Assert.Null(await api.ScalarAsync<int?>("SELECT id_usuario_decide FROM autorizacion WHERE id_autorizacion = @p0", id));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE entidad = 'Autorizacion' AND id_registro = @p0 AND accion = 'AprobarPorPolitica'", id.ToString()));
    }

    // ---------- Decisiones ----------

    [Fact]
    public async Task Aprueba_a_la_persona_y_rechaza_su_vehiculo_con_motivo_registrado()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync(admin);
        var vehiculo = await api.ScalarAsync<int>("INSERT INTO vehiculo (id_organizacion, placa, placa_normalizada, tipo, activo) VALUES (@p0, @p1, @p1, 'Automóvil', true) RETURNING id_vehiculo", _orgA, $"Z{_sufijo[..5].ToUpperInvariant()}");
        await api.EjecutarAsync("INSERT INTO persona_vehiculo (id_organizacion, id_persona, id_vehiculo, relacion, activo) VALUES (@p0, @p1, @p2, 'Propietario', true)", _orgA, persona, vehiculo);

        var registro = await Leer(await admin.PostAsJsonAsync("/api/accesos", Registro(persona, _catVisitante, "Diaria",
            ("objetos", new[] { new { tipoObjeto = "Vehiculo", idObjeto = vehiculo } }))), HttpStatusCode.Created);
        var id = registro.GetProperty("idAutorizacion").GetInt32();
        var idObjeto = registro.GetProperty("objetos")[0].GetProperty("idAutorizacionObjeto").GetInt32();

        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);
        await Leer(await admin.PostAsJsonAsync($"/api/accesos/{id}/objetos/{idObjeto}/rechazar", new { motivo = (string?)null }), HttpStatusCode.BadRequest);
        var final = await Leer(await admin.PostAsJsonAsync($"/api/accesos/{id}/objetos/{idObjeto}/rechazar", new { motivo = "Sin SOAT vigente" }), HttpStatusCode.OK);

        Assert.Equal("Aprobada", final.GetProperty("estado").GetString());
        var objeto = final.GetProperty("objetos")[0];
        Assert.Equal("Rechazada", objeto.GetProperty("estado").GetString());
        Assert.Equal("Sin SOAT vigente", objeto.GetProperty("motivoDecision").GetString());
        Assert.Equal(Correo("admin"), objeto.GetProperty("decididoPor").GetString());
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM autorizacion_objeto_transicion WHERE id_autorizacion_objeto = @p0 AND estado_nuevo = 'Rechazada' AND motivo = 'Sin SOAT vigente'", idObjeto));
    }

    [Fact]
    public async Task Rechazo_y_revocacion_exigen_motivo_y_solo_decide_quien_tiene_permiso_o_el_anfitrion()
    {
        var porteria = await Cliente("porteria");
        var anfitrion = await Cliente("anfitrion");
        var persona = await PersonaAsync(porteria);
        // Visitante usa flujo Anfitrion: el anfitrión designado decide; portería no.
        var id = await Crear(porteria, Registro(persona, _catVisitante, "Unica", ("idMembresiaResponsable", _membresiaAnfitrion)));
        await Leer(await Decidir(porteria, id, "aprobar"), HttpStatusCode.Forbidden);
        await Leer(await Decidir(anfitrion, id, "rechazar"), HttpStatusCode.BadRequest);
        var rechazado = await Leer(await Decidir(anfitrion, id, "rechazar", "No tiene cita"), HttpStatusCode.OK);
        Assert.Equal("Rechazada", rechazado.GetProperty("estado").GetString());
        await Leer(await Decidir(anfitrion, id, "aprobar"), HttpStatusCode.Conflict);

        // Contratista usa flujo Seguridad: el anfitrión no decide.
        var id2 = await Crear(porteria, Registro(await PersonaAsync(porteria), _catRecurrente, "Recurrente", ("franjas", TodoElDia(1, 2, 3, 4, 5, 6, 7)), ("idMembresiaResponsable", _membresiaAnfitrion)));
        await Leer(await Decidir(anfitrion, id2, "aprobar"), HttpStatusCode.Forbidden);
        var admin = await Cliente("admin");
        await Leer(await Decidir(admin, id2, "aprobar"), HttpStatusCode.OK);
        await Leer(await Decidir(admin, id2, "revocar"), HttpStatusCode.BadRequest);
        await Leer(await Decidir(admin, id2, "revocar", "Terminó el contrato"), HttpStatusCode.OK);
    }

    // ---------- Entradas y salidas ----------

    [Fact]
    public async Task Ingreso_fuera_de_horario_queda_bloqueado_y_auditado_y_con_alertar_ingresa_con_alerta()
    {
        var admin = await Cliente("admin");
        var porteria = await Cliente("porteria");
        var persona = await PersonaAsync(admin);
        var manana = DiaIso(Hoy.AddDays(1));
        var id = await Crear(admin, Registro(persona, _catRecurrente, "Recurrente", ("fechaFin", EnDias(30)), ("franjas", TodoElDia(manana))));
        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);

        var negado = await Leer(await Movimiento(porteria, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(negado.GetProperty("motivos").EnumerateArray(), m => m.GetString()!.Contains("este día"));
        Assert.Equal(0, await api.ScalarAsync<long>("SELECT count(*) FROM permanencia WHERE id_persona = @p0", persona));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM movimiento_acceso WHERE id_autorizacion = @p0 AND resultado = 'Denegado'", id));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE entidad = 'Autorizacion' AND id_registro = @p0 AND accion = 'RegistrarEntrada' AND resultado = 'Denegado'", id.ToString()));

        // Fuera de vigencia (aún no empieza) bloquea siempre, también en modo Alertar.
        await Leer(await admin.PutAsJsonAsync("/api/accesos/configuracion", new { fueraDeHorario = "Alertar" }), HttpStatusCode.OK);
        var futuro = await Crear(admin, Registro(await PersonaAsync(admin), _catVisitante, "Unica", ("fechaInicio", EnDias(2))));
        await Leer(await Decidir(admin, futuro, "aprobar"), HttpStatusCode.OK);
        await Leer(await Movimiento(porteria, $"/api/accesos/{futuro}/entrada", _puntoA), HttpStatusCode.Conflict);

        // Alertar: fuera de horario ingresa y queda la alerta auditada.
        await Leer(await Movimiento(porteria, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.OK);
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE entidad = 'Autorizacion' AND id_registro = @p0 AND accion = 'IngresoFueraDeHorario' AND resultado = 'Alerta'", id.ToString()));
        await api.EjecutarAsync("DELETE FROM parametro_organizacion WHERE id_organizacion = @p0 AND clave = 'accesos.fuera_horario'", _orgA);
    }

    [Fact]
    public async Task Entrada_crea_episodio_y_permanencia_la_salida_la_cierra_y_el_acceso_unico_no_se_consume_dos_veces()
    {
        var admin = await Cliente("admin");
        var porteria = await Cliente("porteria");
        var persona = await PersonaAsync(admin);
        var id = await Crear(admin, Registro(persona, _catVisitante, "Unica"));
        await Leer(await Movimiento(porteria, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.Conflict); // Pendiente.
        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);

        var clave = Guid.NewGuid().ToString();
        var entrada = await Leer(await Movimiento(porteria, $"/api/accesos/{id}/entrada", _puntoA, clave), HttpStatusCode.OK);
        Assert.Equal("Entrada", entrada.GetProperty("tipoMovimiento").GetString());
        Assert.True((await Leer(await porteria.GetAsync($"/api/accesos/{id}"), HttpStatusCode.OK)).GetProperty("dentro").GetBoolean());
        var replica = await Movimiento(porteria, $"/api/accesos/{id}/entrada", _puntoA, clave);
        Assert.Equal(HttpStatusCode.OK, replica.StatusCode);
        Assert.True(replica.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM visita WHERE id_autorizacion = @p0", id));

        var presencia = await Leer(await porteria.GetAsync("/api/accesos/presencia"), HttpStatusCode.OK);
        var fila = presencia.EnumerateArray().Single(x => x.GetProperty("idAutorizacion").GetInt32() == id);
        var permanencia = fila.GetProperty("idPermanencia").GetInt32();
        Assert.Equal("Portería", fila.GetProperty("puntoEntrada").GetString());

        await Leer(await Movimiento(porteria, $"/api/accesos/permanencias/{permanencia}/salida", _puntoA), HttpStatusCode.OK);
        await Leer(await Movimiento(porteria, $"/api/accesos/permanencias/{permanencia}/salida", _puntoA), HttpStatusCode.Conflict);
        Assert.Equal("Finalizada", await api.ScalarAsync<string>("SELECT estado FROM visita WHERE id_autorizacion = @p0", id));
        // Ingreso único ya usado: el segundo intento se niega y queda registrado.
        var segundo = await Leer(await Movimiento(porteria, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(segundo.GetProperty("motivos").EnumerateArray(), m => m.GetString()!.Contains("ya fue utilizado"));

        var historial = await Leer(await porteria.GetAsync($"/api/accesos/historial?idPersona={persona}"), HttpStatusCode.OK);
        Assert.Equal(5, historial.GetProperty("total").GetInt32()); // Denegada (pendiente), entrada, salida, salida repetida denegada, entrada denegada (consumido).
        var csv = await porteria.GetAsync($"/api/accesos/historial/exportar?idPersona={persona}");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Contains("Entrada", await csv.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task La_bitacora_es_inmutable_y_rastrea_el_registro_de_punta_a_punta()
    {
        var admin = await Cliente("admin");
        var id = await Crear(admin, Registro(await PersonaAsync(admin), _catVisitante, "Unica"));
        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.OK);
        var bitacora = await Leer(await admin.GetAsync($"/api/accesos/{id}/bitacora"), HttpStatusCode.OK);
        var acciones = bitacora.EnumerateArray().Select(x => x.GetProperty("accion").GetString()).ToList();
        Assert.Equal(["Crear", "Aprobar", "RegistrarEntrada"], acciones);

        await Assert.ThrowsAnyAsync<Exception>(() => api.EjecutarAsync("UPDATE auditoria SET accion = 'X' WHERE entidad = 'Autorizacion' AND id_registro = @p0", id.ToString()));
        await Assert.ThrowsAnyAsync<Exception>(() => api.EjecutarAsync("DELETE FROM auditoria WHERE entidad = 'Autorizacion' AND id_registro = @p0", id.ToString()));
    }

    // ---------- Aislamiento ----------

    [Fact]
    public async Task Otra_organizacion_no_ve_ni_afecta_registros_ajenos()
    {
        var admin = await Cliente("admin");
        var adminB = await Cliente("adminb");
        var id = await Crear(admin, Registro(await PersonaAsync(admin), _catVisitante, "Unica"));

        await Leer(await adminB.GetAsync($"/api/accesos/{id}"), HttpStatusCode.NotFound);
        await Leer(await adminB.GetAsync($"/api/accesos/{id}/bitacora"), HttpStatusCode.NotFound);
        await Leer(await Decidir(adminB, id, "aprobar"), HttpStatusCode.NotFound);
        await Leer(await Movimiento(adminB, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.NotFound);
        var lista = await Leer(await adminB.GetAsync("/api/accesos"), HttpStatusCode.OK);
        Assert.DoesNotContain(lista.GetProperty("registros").EnumerateArray(), x => x.GetProperty("idAutorizacion").GetInt32() == id);
        // Datos de A con categoría/sede de A enviados por B: no existen para B.
        await Leer(await adminB.PostAsJsonAsync("/api/accesos", Registro(1, _catVisitante, "Unica")), HttpStatusCode.BadRequest);
        Assert.Equal("Pendiente", await api.ScalarAsync<string>("SELECT estado FROM autorizacion WHERE id_autorizacion = @p0", id));
    }

    // ---------- Concurrencia, idempotencia, alcance y permisos ----------

    [Fact]
    public async Task Entradas_simultaneas_crean_una_sola_permanencia_y_consumen_el_unico_una_vez()
    {
        var admin = await Cliente("admin");
        var porteria = await Cliente("porteria");
        var persona = await PersonaAsync(admin);
        var id = await Crear(admin, Registro(persona, _catVisitante, "Unica"));
        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Movimiento(i % 2 == 0 ? admin : porteria, $"/api/accesos/{id}/entrada", _puntoA)));
        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM permanencia WHERE id_persona = @p0", persona));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT ingresos_consumidos FROM autorizacion WHERE id_autorizacion = @p0", id));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM visita WHERE id_autorizacion = @p0", id));
    }

    [Fact]
    public async Task Registros_incompatibles_en_paralelo_dejan_uno_solo_y_dos_decisiones_simultaneas_una_transicion()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync(admin);
        var creados = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => admin.PostAsJsonAsync("/api/accesos", Registro(persona, _catVisitante, "Diaria"))));
        Assert.Equal(1, creados.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(creados.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        var id = await api.ScalarAsync<int>("SELECT id_autorizacion FROM autorizacion WHERE id_persona = @p0", persona);

        var otro = await Cliente("admin");
        var decisiones = await Task.WhenAll(Decidir(admin, id, "aprobar"), Decidir(otro, id, "rechazar", "Cruce de agenda"));
        Assert.Equal(1, decisiones.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM autorizacion_transicion WHERE id_autorizacion = @p0 AND estado_anterior = 'Pendiente'", id));
    }

    [Fact]
    public async Task La_misma_clave_no_duplica_y_reutilizada_con_otro_cuerpo_se_rechaza()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync(admin);
        var id = await Crear(admin, Registro(persona, _catRecurrente, "Recurrente", ("franjas", TodoElDia(1, 2, 3, 4, 5, 6, 7))));
        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);
        var clave = Guid.NewGuid().ToString();
        await Leer(await Movimiento(admin, $"/api/accesos/{id}/entrada", _puntoA, clave), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, $"/api/accesos/{id}/entrada", _puntoA, clave), HttpStatusCode.OK);
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM movimiento_acceso WHERE id_autorizacion = @p0 AND resultado = 'Confirmado'", id));
        var permanencia = await api.ScalarAsync<int>("SELECT id_permanencia FROM permanencia WHERE id_persona = @p0", persona);
        await Leer(await Movimiento(admin, $"/api/accesos/permanencias/{permanencia}/salida", _puntoA, clave), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task La_salida_no_depende_de_la_vigencia_y_finalizar_no_cierra_el_recurrente()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync(admin);
        var id = await Crear(admin, Registro(persona, _catRecurrente, "Recurrente", ("franjas", TodoElDia(1, 2, 3, 4, 5, 6, 7))));
        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await Decidir(admin, id, "revocar", "Incidente en planta"), HttpStatusCode.OK);
        var permanencia = await api.ScalarAsync<int>("SELECT id_permanencia FROM permanencia WHERE id_persona = @p0", persona);
        await Leer(await Movimiento(admin, $"/api/accesos/permanencias/{permanencia}/salida", _puntoA), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, $"/api/accesos/{id}/entrada", _puntoA), HttpStatusCode.Conflict);

        // Un recurrente vigente sigue habilitando tras finalizar el episodio.
        var otro = await Crear(admin, Registro(await PersonaAsync(admin), _catRecurrente, "Recurrente", ("franjas", TodoElDia(1, 2, 3, 4, 5, 6, 7))));
        await Leer(await Decidir(admin, otro, "aprobar"), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, $"/api/accesos/{otro}/entrada", _puntoA), HttpStatusCode.OK);
        var p2 = await api.ScalarAsync<int>("SELECT p.id_permanencia FROM permanencia p JOIN visita v ON v.id_visita = p.id_visita WHERE v.id_autorizacion = @p0", otro);
        await Leer(await Movimiento(admin, $"/api/accesos/permanencias/{p2}/salida", _puntoA), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, $"/api/accesos/{otro}/entrada", _puntoA), HttpStatusCode.OK);
        Assert.Equal("Aprobada", await api.ScalarAsync<string>("SELECT estado FROM autorizacion WHERE id_autorizacion = @p0", otro));
        Assert.Equal(2, await api.ScalarAsync<long>("SELECT count(*) FROM visita WHERE id_autorizacion = @p0", otro));
    }

    [Fact]
    public async Task El_operador_solo_ve_y_opera_sus_sedes_y_los_permisos_de_entrada_y_salida_son_independientes()
    {
        var admin = await Cliente("admin");
        var operadora = await Cliente("operadora2");
        var entradas = await Cliente("entradas");
        var enA = await Crear(admin, Registro(await PersonaAsync(admin), _catVisitante, "Diaria"));
        await Leer(await Decidir(admin, enA, "aprobar"), HttpStatusCode.OK);

        await Leer(await operadora.GetAsync($"/api/accesos/{enA}"), HttpStatusCode.NotFound);
        await Leer(await Movimiento(operadora, $"/api/accesos/{enA}/entrada", _puntoA), HttpStatusCode.NotFound);
        await Leer(await operadora.PostAsJsonAsync("/api/accesos", Registro(await PersonaAsync(admin), _catVisitante, "Diaria")), HttpStatusCode.Forbidden);
        var opciones = await Leer(await operadora.GetAsync("/api/accesos/opciones"), HttpStatusCode.OK);
        Assert.Equal([_sedeA2], opciones.GetProperty("sedes").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()));

        var enA2 = await Crear(admin, Registro(await PersonaAsync(admin), _catVisitante, "Diaria", ("idSede", _sedeA2), ("idArea", _areaA2)));
        await Leer(await Decidir(admin, enA2, "aprobar"), HttpStatusCode.OK);
        // Un punto de otra sede no sirve.
        await Leer(await Movimiento(operadora, $"/api/accesos/{enA2}/entrada", _puntoA), HttpStatusCode.BadRequest);

        // Sin Editar: entra pero no registra salidas.
        await Leer(await Movimiento(entradas, $"/api/accesos/{enA2}/entrada", _puntoA2), HttpStatusCode.OK);
        var permanencia = await api.ScalarAsync<int>("SELECT p.id_permanencia FROM permanencia p JOIN visita v ON v.id_visita = p.id_visita WHERE v.id_autorizacion = @p0", enA2);
        await Leer(await Movimiento(entradas, $"/api/accesos/permanencias/{permanencia}/salida", _puntoA2), HttpStatusCode.Forbidden);
        await Leer(await Movimiento(operadora, $"/api/accesos/permanencias/{permanencia}/salida", _puntoA2), HttpStatusCode.OK);
    }

    [Fact]
    public async Task Los_endpoints_anteriores_de_visitas_y_autorizaciones_ya_no_existen()
    {
        var admin = await Cliente("admin");
        foreach (var ruta in new[] { "/api/visitas", "/api/autorizaciones-acceso", "/api/autorizaciones-objeto" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(ruta)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync(ruta, new { })).StatusCode);
        }
    }

    [Fact]
    public async Task La_base_exige_categoria_y_registro_de_acceso_en_filas_nuevas()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync(admin);
        var usuario = await api.ScalarAsync<int>("SELECT id_usuario FROM usuario WHERE correo = @p0", Correo("admin"));
        await Assert.ThrowsAnyAsync<Exception>(() => api.EjecutarAsync("""
            INSERT INTO autorizacion (id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_crea, tipo_emisor, alcance, modalidad, fecha_inicio, fecha_fin, ingresos_consumidos, estado, version, creado_en, origen, usa_horario_base)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, 'Empresa', 'Privado', 'Diaria', current_date, current_date, 0, 'Pendiente', 1, now(), 'Manual', false)
            """, _orgA, persona, _sedeA, _areaA, _membresiaAdmin, usuario));
        await Assert.ThrowsAnyAsync<Exception>(() => api.EjecutarAsync("""
            INSERT INTO visita (id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_registra, estado, version, creado_en, excepcion_tipo_acceso)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, 'Ingreso', 1, now(), false)
            """, _orgA, persona, _sedeA, _areaA, _membresiaAdmin, usuario));
        // Rechazo sin motivo.
        var id = await Crear(admin, Registro(persona, _catVisitante, "Unica"));
        await Assert.ThrowsAnyAsync<Exception>(() => api.EjecutarAsync("UPDATE autorizacion SET estado = 'Rechazada', motivo_decision = NULL WHERE id_autorizacion = @p0", id));
    }

    // ---------- Reglas puras ----------

    [Fact]
    public void Reglas_de_franjas_y_vigencia()
    {
        Assert.NotNull(ReglasAcceso.ValidarFranjas([new(1, new(8, 0), new(12, 0)), new(1, new(11, 0), new(14, 0))]));
        Assert.Null(ReglasAcceso.ValidarFranjas([new(1, new(8, 0), new(12, 0)), new(1, new(12, 0), new(14, 0))]));
        Assert.NotNull(ReglasAcceso.ValidarFranjas([new(8, new(8, 0), new(9, 0))]));

        var lunes = new DateTime(2026, 11, 2, 10, 0, 0);
        var a = new Autorizacion { Estado = "Aprobada", Modalidad = "Recurrente", FechaInicio = new(2026, 11, 1) };
        List<ReglasAcceso.Franja> horario = [new(1, new(7, 0), new(9, 0)), new(1, new(9, 30), new(12, 0))];
        Assert.True(ReglasAcceso.Evaluar(a, horario, lunes).Habilita);
        Assert.Equal("Vigente", ReglasAcceso.Vigencia(a, horario, lunes));
        Assert.Equal("FueraDeHorario", ReglasAcceso.Vigencia(a, horario, lunes.AddHours(3)));
        var fuera = ReglasAcceso.Evaluar(a, horario, lunes.AddDays(1));
        Assert.Empty(fuera.Bloqueos);
        Assert.Single(fuera.Horario);

        // Horario base: la sede sobrescribe a la organización.
        var conBase = new Autorizacion { IdSede = 5, UsaHorarioBase = true };
        var efectivo = ReglasAcceso.HorarioEfectivo(conBase, [],
            [new HorarioBase { IdSede = null, DiaSemana = 1, HoraInicio = new(6, 0), HoraFin = new(18, 0) }, new HorarioBase { IdSede = 5, DiaSemana = 2, HoraInicio = new(8, 0), HoraFin = new(10, 0) }]);
        Assert.Equal(2, Assert.Single(efectivo).DiaSemana);

        Assert.Equal("Consumida", ReglasAcceso.Vigencia(new Autorizacion { Estado = "Aprobada", Modalidad = "Unica", FechaInicio = new(2026, 11, 2), FechaFin = new(2026, 11, 2), IngresosConsumidos = 1 }, [], lunes));
        Assert.NotNull(ReglasAcceso.ValidarTransicion("Pendiente", "Revocar"));
        Assert.NotNull(ReglasAcceso.ValidarTransicion("Rechazada", "Cancelar"));
        Assert.Null(ReglasAcceso.ValidarTransicion("Aprobada", "Cancelar"));
        Assert.True(ReglasAcceso.MotivoObligatorio("Revocar") && !ReglasAcceso.MotivoObligatorio("Aprobar"));

        // Temporal: los instantes de inicio y fin son vigencia (bloquean siempre, aun con «Alertar»).
        var temporal = new Autorizacion { Estado = "Aprobada", Modalidad = "Temporal", FechaInicio = new(2026, 11, 2), FechaFin = new(2026, 11, 3), HoraInicio = new(9, 0), HoraFin = new(17, 0) };
        Assert.Single(ReglasAcceso.Evaluar(temporal, [], new DateTime(2026, 11, 2, 8, 0, 0)).Bloqueos);
        Assert.True(ReglasAcceso.Evaluar(temporal, [], new DateTime(2026, 11, 2, 20, 0, 0)).Habilita);
        Assert.Equal("Programada", ReglasAcceso.Vigencia(temporal, [], new DateTime(2026, 11, 2, 8, 0, 0)));
        Assert.Equal("Vencida", ReglasAcceso.Vigencia(temporal, [], new DateTime(2026, 11, 3, 17, 0, 0)));
        // Única con franja: fuera de la franja es horario (bloquea o alerta según la organización).
        var unica = new Autorizacion { Estado = "Aprobada", Modalidad = "Unica", FechaInicio = new(2026, 11, 2), FechaFin = new(2026, 11, 2), HoraInicio = new(9, 0), HoraFin = new(11, 0) };
        var temprano = ReglasAcceso.Evaluar(unica, [], new DateTime(2026, 11, 2, 8, 0, 0));
        Assert.Empty(temprano.Bloqueos);
        Assert.Single(temprano.Horario);
        // Pendiente o revocado nunca habilitan; Permanente sin franjas habilita a cualquier hora.
        Assert.False(ReglasAcceso.Evaluar(new Autorizacion { Estado = "Pendiente", Modalidad = "Permanente", FechaInicio = new(2026, 1, 1) }, [], lunes).Habilita);
        Assert.False(ReglasAcceso.Evaluar(new Autorizacion { Estado = "Revocada", Modalidad = "Permanente", FechaInicio = new(2026, 1, 1) }, [], lunes).Habilita);
        Assert.True(ReglasAcceso.Evaluar(new Autorizacion { Estado = "Aprobada", Modalidad = "Permanente", FechaInicio = new(2026, 1, 1) }, [], lunes.AddHours(13)).Habilita);
        // Horario base: sin franjas de la sede, se usa el de la organización.
        var soloOrg = ReglasAcceso.HorarioEfectivo(new Autorizacion { IdSede = 9, UsaHorarioBase = true }, [], [new HorarioBase { IdSede = null, DiaSemana = 3, HoraInicio = new(6, 0), HoraFin = new(18, 0) }]);
        Assert.Equal(3, Assert.Single(soloOrg).DiaSemana);
        // Normalizar: Recurrente sin franjas ni horario base, y franjas en una Única, son inválidos.
        Assert.NotNull(ReglasAcceso.Normalizar("Recurrente", new(2026, 11, 2), null, null, null, [], false, lunes).Error);
        Assert.NotNull(ReglasAcceso.Normalizar("Unica", new(2026, 11, 2), null, null, null, [new(1, new(8, 0), new(9, 0))], false, lunes).Error);
        Assert.Null(ReglasAcceso.Normalizar("Recurrente", new(2026, 11, 2), null, null, null, [], true, lunes).Error);
    }
}
