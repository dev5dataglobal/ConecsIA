using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Solicitudes de autorización de ingreso de personas (Control de accesos): persona existente sin duplicarla, modalidades
/// y su coherencia, estado inicial Pendiente, aprobación/rechazo solo con Autorizaciones · Editar, trazabilidad
/// (solicitante, decisor, instante, motivo, historial y auditoría), incompatibles bajo concurrencia, aislamiento y efecto
/// en el ingreso: pendiente o rechazada nunca habilitan una entrada; la única se consume una vez.
/// </summary>
public sealed class AutorizacionesAccesoTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _sedeA, _areaA, _areaA2, _puntoA, _sedeB, _areaB;
    private int _membresiaAdmin, _membresiaAdminB, _usuarioAdmin, _usuarioSolicitante, _tipoPersonaA, _cc;
    private string _sufijo = "";
    private static DateOnly HoyFecha => DateOnly.FromDateTime(DateTime.UtcNow);
    private static string EnDias(int dias) => HoyFecha.AddDays(dias).ToString("yyyy-MM-dd");

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");

        // Administrador de organización: decide (Autorizaciones · Editar).
        var rolAdmin = await api.CrearRolAsync(_orgA, "Administrador");
        foreach (var m in new[] { "personas", "visitas", "control_accesos", "autorizaciones" }) await api.AsignarModuloAsync(_orgA, rolAdmin, m);
        _usuarioAdmin = await api.CrearUsuarioAsync(Correo("admin"), _orgA, rolAdmin);
        // Solicitante: registra personas y solicita su acceso, pero no decide.
        var rolSolicitante = await api.CrearRolAsync(_orgA, "Recepción");
        foreach (var m in new[] { "personas", "visitas", "control_accesos" }) await api.AsignarModuloAsync(_orgA, rolSolicitante, m);
        await api.AsignarModuloAsync(_orgA, rolSolicitante, "autorizaciones", consultar: true, anadir: true, editar: false);
        _usuarioSolicitante = await api.CrearUsuarioAsync(Correo("recepcion"), _orgA, rolSolicitante);
        var rolConsulta = await api.CrearRolAsync(_orgA, "Consulta");
        await api.AsignarModuloAsync(_orgA, rolConsulta, "control_accesos", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("consulta"), _orgA, rolConsulta);
        var rolB = await api.CrearRolAsync(_orgB, "Administrador B");
        foreach (var m in new[] { "personas", "control_accesos", "autorizaciones" }) await api.AsignarModuloAsync(_orgB, rolB, m);
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, rolB);

        var ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad");
        _sedeA = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede A', 'UTC', true) RETURNING id_sede", _orgA, ciudad);
        _sedeB = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede B', 'UTC', true) RETURNING id_sede", _orgB, ciudad);
        _areaA = await Area(_orgA, _sedeA, "Gerencia");
        _areaA2 = await Area(_orgA, _sedeA, "Bodega");
        _areaB = await Area(_orgB, _sedeB, "Recepción B");
        _puntoA = await api.ScalarAsync<int>("INSERT INTO punto_acceso (id_organizacion, id_sede, nombre, modo_acceso, activo, creado_en) VALUES (@p0, @p1, 'Portería', 'Ambos', true, now()) RETURNING id_punto_acceso", _orgA, _sedeA);
        _membresiaAdmin = await Membresia(Correo("admin"));
        _membresiaAdminB = await Membresia(Correo("adminb"));
        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersonaA = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Contratista', true) RETURNING id_tipo_persona", _orgA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- Apoyo ----------

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));
    private Task<int> Area(int org, int sede, string nombre) =>
        api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, @p2, false, true) RETURNING id_area", org, sede, nombre);
    private Task<int> Membresia(string correo) =>
        api.ScalarAsync<int>("SELECT m.id_membresia FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario WHERE u.correo = @p0", correo);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(esperado == respuesta.StatusCode, $"Se esperaba {(int)esperado} y llegó {(int)respuesta.StatusCode}: {cuerpo}");
        return cuerpo.Length == 0 ? default : JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private async Task<(int Id, string Documento)> PersonaAsync(HttpClient cliente)
    {
        var documento = Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString();
        var persona = await Leer(await cliente.PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = documento, nombres = "Luis", apellidos = $"Contratista {_sufijo}", correo = "luis@ejemplo.test",
        }), HttpStatusCode.Created);
        return (persona.GetProperty("idPersona").GetInt32(), documento);
    }

    private Dictionary<string, object?> Solicitud(int persona, string modalidad, int? area = null) => new()
    {
        ["idPersona"] = persona, ["idSede"] = _sedeA, ["idArea"] = area ?? _areaA, ["idMembresiaResponsable"] = _membresiaAdmin,
        ["modalidad"] = modalidad, ["fechaInicio"] = EnDias(0), ["motivo"] = "Mantenimiento de equipos",
    };

    private static Dictionary<string, object?> Con(Dictionary<string, object?> s, params (string Clave, object? Valor)[] cambios)
    {
        var copia = new Dictionary<string, object?>(s);
        foreach (var (clave, valor) in cambios) copia[clave] = valor;
        return copia;
    }

    private static Task<HttpResponseMessage> Solicitar(HttpClient cliente, object solicitud) => cliente.PostAsJsonAsync("/api/autorizaciones-acceso", solicitud);

    private static Task<HttpResponseMessage> Decidir(HttpClient cliente, int id, string decision, string? motivo = null) =>
        cliente.PostAsJsonAsync($"/api/autorizaciones-acceso/{id}/{decision}", new { motivo });

    private static Task<HttpResponseMessage> Entrada(HttpClient cliente, int visita, int punto)
    {
        var mensaje = new HttpRequestMessage(HttpMethod.Post, $"/api/visitas/{visita}/entrada") { Content = JsonContent.Create(new { idPuntoAcceso = punto }) };
        mensaje.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return cliente.SendAsync(mensaje);
    }

    private object Visita(int persona, int? autorizacion) => new
    {
        idPersona = persona, idSede = _sedeA, idArea = _areaA, idMembresiaResponsable = _membresiaAdmin, motivo = "Mantenimiento", idAutorizacion = autorizacion,
    };

    // ---------- Flujo completo ----------

    [Fact]
    public async Task Registrar_persona_y_solicitar_su_acceso_no_la_duplica_y_la_pendiente_no_habilita_el_ingreso()
    {
        var recepcion = await Cliente("recepcion");
        var admin = await Cliente("admin");
        var (persona, documento) = await PersonaAsync(recepcion);

        var creada = await Leer(await Solicitar(recepcion, Solicitud(persona, "Unica")), HttpStatusCode.Created);
        var id = creada.GetProperty("idAutorizacion").GetInt32();
        Assert.Equal("Pendiente", creada.GetProperty("estado").GetString());
        Assert.Equal(1, creada.GetProperty("cupoIngresos").GetInt32());
        Assert.Equal(Correo("recepcion"), creada.GetProperty("solicitadoPor").GetString());
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM persona WHERE id_organizacion = @p0 AND numero_documento = @p1", _orgA, documento));
        Assert.Equal(_usuarioSolicitante, await api.ScalarAsync<int>("SELECT id_usuario_crea FROM autorizacion WHERE id_autorizacion = @p0", id));
        Assert.Equal(_orgA, await api.ScalarAsync<int>("SELECT id_organizacion FROM autorizacion WHERE id_autorizacion = @p0", id));
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE entidad = 'Autorizacion' AND id_registro = @p0 AND accion = 'Solicitar' AND id_usuario = @p1", id.ToString(), _usuarioSolicitante));

        // Pendiente: la visita queda pendiente y la entrada se niega sin abrir permanencia.
        var visita = await Leer(await recepcion.PostAsJsonAsync("/api/visitas", Visita(persona, id)), HttpStatusCode.Created);
        var idVisita = visita.GetProperty("visita").GetProperty("idVisita").GetInt32();
        Assert.Equal("Pendiente", visita.GetProperty("visita").GetProperty("estado").GetString());
        var negada = await Leer(await Entrada(recepcion, idVisita, _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("pendiente", negada.GetProperty("detail").GetString());
        Assert.Equal(0, await api.ScalarAsync<long>("SELECT count(*) FROM permanencia WHERE id_visita = @p0", idVisita));

        // El solicitante no puede aprobar; el administrador sí, y queda registrado quién y cuándo.
        await Leer(await Decidir(recepcion, id, "aprobar"), HttpStatusCode.Forbidden);
        var aprobada = await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.OK);
        Assert.Equal("Aprobada", aprobada.GetProperty("estado").GetString());
        Assert.Equal("Vigente", aprobada.GetProperty("vigencia").GetString());
        Assert.Equal(Correo("admin"), aprobada.GetProperty("decididoPor").GetString());
        Assert.NotEqual(JsonValueKind.Null, aprobada.GetProperty("decididoEn").ValueKind);
        Assert.Equal(2, aprobada.GetProperty("historial").GetArrayLength());
        Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE entidad = 'Autorizacion' AND id_registro = @p0 AND accion = 'Aprobar' AND id_usuario = @p1", id.ToString(), _usuarioAdmin));

        // Aprobada: la entrada pasa y consume la única; una segunda visita con la misma autorización no ingresa.
        await Leer(await Entrada(recepcion, idVisita, _puntoA), HttpStatusCode.OK);
        var consumida = await Leer(await admin.GetAsync($"/api/autorizaciones-acceso/{id}"), HttpStatusCode.OK);
        Assert.Equal("Consumida", consumida.GetProperty("vigencia").GetString());
        var mensaje = new HttpRequestMessage(HttpMethod.Post, $"/api/visitas/{idVisita}/salida") { Content = JsonContent.Create(new { idPuntoAcceso = _puntoA }) };
        mensaje.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        await Leer(await recepcion.SendAsync(mensaje), HttpStatusCode.OK);
        var segunda = (await Leer(await recepcion.PostAsJsonAsync("/api/visitas", Visita(persona, id)), HttpStatusCode.Created)).GetProperty("visita").GetProperty("idVisita").GetInt32();
        var usada = await Leer(await Entrada(recepcion, segunda, _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("ingreso único ya fue utilizada", usada.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task El_rechazo_exige_motivo_se_conserva_en_el_historial_y_no_habilita_visitas_ni_otra_decision()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var id = (await Leer(await Solicitar(admin, Solicitud(persona, "Diaria")), HttpStatusCode.Created)).GetProperty("idAutorizacion").GetInt32();
        var visita = (await Leer(await admin.PostAsJsonAsync("/api/visitas", Visita(persona, id)), HttpStatusCode.Created)).GetProperty("visita").GetProperty("idVisita").GetInt32();

        await Leer(await Decidir(admin, id, "rechazar", "  "), HttpStatusCode.BadRequest);
        var rechazada = await Leer(await Decidir(admin, id, "rechazar", "No presenta la planilla de seguridad social"), HttpStatusCode.OK);
        Assert.Equal("Rechazada", rechazada.GetProperty("estado").GetString());
        Assert.Equal("No presenta la planilla de seguridad social", rechazada.GetProperty("motivoDecision").GetString());
        var historial = rechazada.GetProperty("historial");
        Assert.Equal("Rechazada", historial[1].GetProperty("estadoNuevo").GetString());
        Assert.Equal(Correo("admin"), historial[1].GetProperty("actor").GetString());

        await Leer(await Decidir(admin, id, "aprobar"), HttpStatusCode.Conflict);
        await Leer(await admin.PostAsJsonAsync("/api/visitas", Visita(persona, id)), HttpStatusCode.BadRequest);
        // La visita vinculada antes del rechazo tampoco ingresa: queda Rechazada.
        await Leer(await Entrada(admin, visita, _puntoA), HttpStatusCode.Conflict);
        Assert.Equal("Rechazada", await api.ScalarAsync<string>("SELECT estado FROM visita WHERE id_visita = @p0", visita));
        Assert.Equal(0, await api.ScalarAsync<long>("SELECT count(*) FROM permanencia WHERE id_visita = @p0", visita));
        // Sigue consultable en el historial.
        var lista = await admin.GetFromJsonAsync<JsonElement>($"/api/autorizaciones-acceso?idPersona={persona}&estado=Rechazada");
        Assert.Equal(1, lista.GetArrayLength());
    }

    // ---------- Modalidades ----------

    [Fact]
    public async Task Cada_modalidad_valida_sus_condiciones_y_guarda_solo_los_campos_que_le_aplican()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);

        // Única y Diaria: un día, no anterior a hoy; franja opcional coherente. La fecha final enviada se ignora.
        await Leer(await Solicitar(admin, Con(Solicitud(persona, "Diaria"), ("fechaInicio", EnDias(-1)))), HttpStatusCode.BadRequest);
        await Leer(await Solicitar(admin, Con(Solicitud(persona, "Unica"), ("fechaInicio", EnDias(1)), ("horaInicio", "10:00"), ("horaFin", "09:00"))), HttpStatusCode.BadRequest);
        var unica = await Leer(await Solicitar(admin, Con(Solicitud(persona, "Unica"), ("fechaInicio", EnDias(1)), ("fechaFin", EnDias(9)))), HttpStatusCode.Created);
        Assert.Equal(EnDias(1), unica.GetProperty("fechaFin").GetString());

        // Temporal: fecha y hora de inicio y fin, fin posterior al inicio.
        var temporal = Con(Solicitud(persona, "Temporal", _areaA2), ("fechaInicio", EnDias(2)), ("horaInicio", "08:00"), ("fechaFin", EnDias(2)), ("horaFin", "08:00"));
        await Leer(await Solicitar(admin, temporal), HttpStatusCode.BadRequest);
        await Leer(await Solicitar(admin, Con(temporal, ("horaFin", null))), HttpStatusCode.BadRequest);
        var t = await Leer(await Solicitar(admin, Con(temporal, ("fechaFin", EnDias(4)), ("horaFin", "18:00"))), HttpStatusCode.Created);
        Assert.Equal("08:00:00", t.GetProperty("horaInicio").GetString());

        // Recurrente: días y franja; periodo opcional. Se valida con otra persona para no cruzarse.
        var (otra, _) = await PersonaAsync(admin);
        var recurrente = Solicitud(otra, "Recurrente");
        await Leer(await Solicitar(admin, Con(recurrente, ("horaInicio", "07:00"), ("horaFin", "17:00"))), HttpStatusCode.BadRequest);
        await Leer(await Solicitar(admin, Con(recurrente, ("diasSemana", new[] { 1, 8 }), ("horaInicio", "07:00"), ("horaFin", "17:00"))), HttpStatusCode.BadRequest);
        await Leer(await Solicitar(admin, Con(recurrente, ("diasSemana", new[] { 1 }), ("horaInicio", "17:00"), ("horaFin", "07:00"))), HttpStatusCode.BadRequest);
        await Leer(await Solicitar(admin, Con(recurrente, ("diasSemana", new[] { 1 }), ("horaInicio", "07:00"), ("horaFin", "17:00"), ("fechaFin", EnDias(-1)))), HttpStatusCode.BadRequest);
        var r = await Leer(await Solicitar(admin, Con(recurrente, ("diasSemana", new[] { 5, 1, 3, 1 }), ("horaInicio", "07:00"), ("horaFin", "17:00"))), HttpStatusCode.Created);
        Assert.Equal(3, r.GetProperty("horarios").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, r.GetProperty("fechaFin").ValueKind);
        Assert.Equal(JsonValueKind.Null, r.GetProperty("horaInicio").ValueKind);

        // Permanente: solo con la política de la organización y sin fecha final.
        var permanente = Solicitud(otra, "Permanente", _areaA2);
        var opciones = await admin.GetFromJsonAsync<JsonElement>("/api/autorizaciones-acceso/opciones");
        Assert.False(opciones.GetProperty("permitePermanente").GetBoolean());
        await Leer(await Solicitar(admin, permanente), HttpStatusCode.Conflict);
        await api.EjecutarAsync("INSERT INTO parametro_organizacion (id_organizacion, clave, valor, actualizado_en) VALUES (@p0, @p1, 'true', now()) ON CONFLICT DO NOTHING", _orgA, ReglasAutorizacion.ParametroPermitePermanente);
        await Leer(await Solicitar(admin, Con(permanente, ("fechaFin", EnDias(30)))), HttpStatusCode.BadRequest);
        var p = await Leer(await Solicitar(admin, permanente), HttpStatusCode.Created);
        Assert.Equal(JsonValueKind.Null, p.GetProperty("fechaFin").ValueKind);

        await Leer(await Solicitar(admin, Con(Solicitud(otra, "Vitalicia"), ("fechaInicio", EnDias(0)))), HttpStatusCode.BadRequest);
    }

    // ---------- Incompatibles y concurrencia ----------

    [Fact]
    public async Task Una_solicitud_que_se_cruza_con_otra_pendiente_o_aprobada_se_rechaza_tambien_en_paralelo()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var temporal = Con(Solicitud(persona, "Temporal"), ("fechaInicio", EnDias(1)), ("horaInicio", "08:00"), ("fechaFin", EnDias(5)), ("horaFin", "18:00"));
        var tareas = Enumerable.Range(0, 5).Select(async _ => (await Solicitar(await Cliente("admin"), temporal)).StatusCode).ToList();
        var codigos = await Task.WhenAll(tareas);
        Assert.Equal(1, codigos.Count(c => c == HttpStatusCode.Created));
        Assert.Equal(4, codigos.Count(c => c == HttpStatusCode.Conflict));

        // Otra modalidad en un día del periodo también es incompatible; en otro destino o fuera del periodo, no.
        var conflicto = await Leer(await Solicitar(admin, Con(Solicitud(persona, "Diaria"), ("fechaInicio", EnDias(3)))), HttpStatusCode.Conflict);
        Assert.Contains("pendiente de aprobación", conflicto.GetProperty("detail").GetString());
        await Leer(await Solicitar(admin, Con(Solicitud(persona, "Diaria", _areaA2), ("fechaInicio", EnDias(3)))), HttpStatusCode.Created);
        var fuera = await Leer(await Solicitar(admin, Con(Solicitud(persona, "Diaria"), ("fechaInicio", EnDias(6)))), HttpStatusCode.Created);

        // Una rechazada deja de bloquear.
        await Leer(await Decidir(admin, fuera.GetProperty("idAutorizacion").GetInt32(), "rechazar", "Fecha errada"), HttpStatusCode.OK);
        await Leer(await Solicitar(admin, Con(Solicitud(persona, "Unica"), ("fechaInicio", EnDias(6)))), HttpStatusCode.Created);
    }

    [Fact]
    public async Task Dos_decisiones_simultaneas_dejan_una_sola_transicion()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var id = (await Leer(await Solicitar(admin, Solicitud(persona, "Diaria")), HttpStatusCode.Created)).GetProperty("idAutorizacion").GetInt32();
        var codigos = await Task.WhenAll(
            Decidir(await Cliente("admin"), id, "aprobar").ContinueWith(x => x.Result.StatusCode),
            Decidir(await Cliente("admin"), id, "rechazar", "Duplicada").ContinueWith(x => x.Result.StatusCode));
        Assert.Equal(1, codigos.Count(c => c == HttpStatusCode.OK));
        Assert.Equal(1, codigos.Count(c => c == HttpStatusCode.Conflict));
        Assert.Equal(2, await api.ScalarAsync<long>("SELECT count(*) FROM autorizacion_transicion WHERE id_autorizacion = @p0", id));
    }

    // ---------- Permisos y aislamiento ----------

    [Fact]
    public async Task Permisos_por_accion_y_otra_organizacion_no_consulta_ni_decide()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var id = (await Leer(await Solicitar(admin, Solicitud(persona, "Diaria")), HttpStatusCode.Created)).GetProperty("idAutorizacion").GetInt32();

        var consulta = await Cliente("consulta");
        await Leer(await consulta.GetAsync($"/api/autorizaciones-acceso/{id}"), HttpStatusCode.OK);
        await Leer(await Solicitar(consulta, Con(Solicitud(persona, "Diaria", _areaA2))), HttpStatusCode.Forbidden);
        await Leer(await Decidir(consulta, id, "aprobar"), HttpStatusCode.Forbidden);

        var adminB = await Cliente("adminb");
        await Leer(await adminB.GetAsync($"/api/autorizaciones-acceso/{id}"), HttpStatusCode.NotFound);
        await Leer(await Decidir(adminB, id, "aprobar"), HttpStatusCode.NotFound);
        Assert.Equal(0, (await adminB.GetFromJsonAsync<JsonElement>($"/api/autorizaciones-acceso?idPersona={persona}")).GetArrayLength());
        // Una persona de otra organización no puede usarse aunque se conozca su id.
        var solicitudB = new { idPersona = persona, idSede = _sedeB, idArea = _areaB, idMembresiaResponsable = _membresiaAdminB, modalidad = "Diaria", fechaInicio = EnDias(0), motivo = "Prueba" };
        await Leer(await Solicitar(adminB, solicitudB), HttpStatusCode.BadRequest);
        Assert.Equal("Pendiente", await api.ScalarAsync<string>("SELECT estado FROM autorizacion WHERE id_autorizacion = @p0", id));
    }

    // ---------- Reglas puras ----------

    [Fact]
    public void La_franja_temporal_y_la_vigencia_se_calculan_en_la_hora_local()
    {
        var hoy = new DateOnly(2026, 10, 28);
        var temporal = new Autorizacion { Modalidad = "Temporal", Estado = "Aprobada", FechaInicio = hoy, FechaFin = hoy.AddDays(1), HoraInicio = new(8, 0), HoraFin = new(18, 0) };
        Assert.Contains("empieza a regir", Assert.Single(ReglasAutorizacion.MotivosFranja(temporal, hoy.ToDateTime(new(7, 59)))));
        Assert.Empty(ReglasAutorizacion.MotivosFranja(temporal, hoy.ToDateTime(new(23, 0))));
        Assert.Contains("venció", Assert.Single(ReglasAutorizacion.MotivosFranja(temporal, hoy.AddDays(1).ToDateTime(new(18, 0)))));
        Assert.Equal("Programada", ReglasAutorizacion.Vigencia("Aprobada", "Temporal", hoy, hoy.AddDays(1), new(8, 0), new(18, 0), null, 0, hoy.ToDateTime(new(7, 0))));
        Assert.Equal("Vencida", ReglasAutorizacion.Vigencia("Aprobada", "Temporal", hoy, hoy.AddDays(1), new(8, 0), new(18, 0), null, 0, hoy.AddDays(1).ToDateTime(new(18, 1))));

        var diaria = new Autorizacion { Modalidad = "Diaria", Estado = "Aprobada", FechaInicio = hoy, FechaFin = hoy, HoraInicio = new(9, 0), HoraFin = new(12, 0) };
        Assert.Contains("Fuera de la franja", Assert.Single(ReglasAutorizacion.MotivosFranja(diaria, hoy.ToDateTime(new(12, 0)))));
        Assert.Empty(ReglasAutorizacion.MotivosFranja(diaria, hoy.ToDateTime(new(9, 0))));
        Assert.Equal("NoAplica", ReglasAutorizacion.Vigencia("Pendiente", "Diaria", hoy, hoy, null, null, null, 0, hoy.ToDateTime(new(9, 0))));
        Assert.Equal("Consumida", ReglasAutorizacion.Vigencia("Aprobada", "Unica", hoy, hoy, null, null, 1, 1, hoy.ToDateTime(new(9, 0))));

        // Recurrente con periodo que no contiene ninguno de los días elegidos.
        var (_, error) = ReglasAutorizacion.Normalizar(new AutorizacionAccesoEntrada
        {
            Modalidad = "Recurrente", FechaInicio = new DateOnly(2026, 11, 2), FechaFin = new DateOnly(2026, 11, 3), DiasSemana = [6, 7],
            HoraInicio = new(8, 0), HoraFin = new(9, 0),
        }, hoy.ToDateTime(new(8, 0)));
        Assert.Equal("Ningún día seleccionado cae dentro del periodo.", error);
    }
}
