using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiConecsia.Services;
using ConecsIA.Domain.Entities;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Visitas y su ciclo operativo: registro (persona existente, destino, responsable, autorización o política),
/// estados, vigencia de la autorización, entrada y salida transaccionales e idempotentes, concurrencia, presencia,
/// aislamiento por organización y sede, permisos separados y auditoría atribuible.
/// </summary>
public sealed class VisitasTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _sedeA, _sedeA2, _areaA, _areaA2, _puntoA, _puntoA2, _puntoSoloSalida, _zonaOtra, _sedeB, _areaB, _puntoB;
    private int _membresiaAdmin, _membresiaAdminB, _usuarioAdmin, _tipoPersonaA, _cc;
    private string _sufijo = "";
    private static string Hoy => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
    private static string EnDias(int dias) => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(dias).ToString("yyyy-MM-dd");

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");

        var rolAdmin = await api.CrearRolAsync(_orgA, "Admin");
        foreach (var m in new[] { "personas", "visitas", "control_accesos" }) await api.AsignarModuloAsync(_orgA, rolAdmin, m);
        _usuarioAdmin = await api.CrearUsuarioAsync(Correo("admin"), _orgA, rolAdmin);
        var rolB = await api.CrearRolAsync(_orgB, "Admin B");
        foreach (var m in new[] { "personas", "visitas", "control_accesos" }) await api.AsignarModuloAsync(_orgB, rolB, m);
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, rolB);

        var rolConsulta = await api.CrearRolAsync(_orgA, "Solo consulta");
        await api.AsignarModuloAsync(_orgA, rolConsulta, "visitas", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("consulta"), _orgA, rolConsulta);
        var rolSinAccesos = await api.CrearRolAsync(_orgA, "Sin control de accesos");
        await api.AsignarModuloAsync(_orgA, rolSinAccesos, "visitas");
        await api.CrearUsuarioAsync(Correo("sinaccesos"), _orgA, rolSinAccesos);
        var rolEntrada = await api.CrearRolAsync(_orgA, "Solo entradas");
        await api.AsignarModuloAsync(_orgA, rolEntrada, "visitas", consultar: true, anadir: false, editar: false);
        await api.AsignarModuloAsync(_orgA, rolEntrada, "control_accesos", consultar: true, anadir: true, editar: false);
        await api.CrearUsuarioAsync(Correo("soloentrada"), _orgA, rolEntrada);

        var ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad");
        _sedeA = await Sede(_orgA, "Sede A", ciudad);
        _sedeA2 = await Sede(_orgA, "Sede A2", ciudad);
        _sedeB = await Sede(_orgB, "Sede B", ciudad);
        _areaA = await Area(_orgA, _sedeA, "Gerencia");
        _areaA2 = await Area(_orgA, _sedeA2, "Bodega");
        _areaB = await Area(_orgB, _sedeB, "Recepción B");
        _puntoA = await Punto(_orgA, _sedeA, "Portería principal", "Ambos");
        _puntoA2 = await Punto(_orgA, _sedeA2, "Portería A2", "Ambos");
        _puntoSoloSalida = await Punto(_orgA, _sedeA, "Salida norte", "Salida");
        _zonaOtra = await api.ScalarAsync<int>("INSERT INTO zona (id_organizacion, id_sede, nombre, es_punto_acceso, activo) VALUES (@p0, @p1, 'Laboratorio', false, true) RETURNING id_zona", _orgA, _sedeA);
        _puntoB = await Punto(_orgB, _sedeB, "Portería B", "Ambos");

        _membresiaAdmin = await Membresia(Correo("admin"));
        _membresiaAdminB = await Membresia(Correo("adminb"));
        // Operador limitado a la sede A2 (membresia_sede).
        var rolOperador = await api.CrearRolAsync(_orgA, "Operador A2");
        foreach (var m in new[] { "visitas", "control_accesos" }) await api.AsignarModuloAsync(_orgA, rolOperador, m);
        await api.CrearUsuarioAsync(Correo("operadora2"), _orgA, rolOperador);
        await api.EjecutarAsync("INSERT INTO membresia_sede (id_organizacion, id_membresia, id_sede) VALUES (@p0, @p1, @p2)", _orgA, await Membresia(Correo("operadora2")), _sedeA2);

        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersonaA = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Visitante', true) RETURNING id_tipo_persona", _orgA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- Apoyo ----------

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));
    private Task<int> Sede(int org, string nombre, int ciudad) =>
        api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, @p2, 'UTC', true) RETURNING id_sede", org, ciudad, nombre);
    private Task<int> Area(int org, int sede, string nombre) =>
        api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, @p2, false, true) RETURNING id_area", org, sede, nombre);
    private Task<int> Punto(int org, int sede, string nombre, string modo) =>
        api.ScalarAsync<int>("INSERT INTO zona (id_organizacion, id_sede, nombre, es_punto_acceso, modo_acceso, activo) VALUES (@p0, @p1, @p2, true, @p3, true) RETURNING id_zona", org, sede, nombre, modo);
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
            idTipoPersona = _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = documento, nombres = "Ana", apellidos = $"Visitante {_sufijo}", correo = "ana@ejemplo.test",
        }), HttpStatusCode.Created);
        return (persona.GetProperty("idPersona").GetInt32(), documento);
    }

    private Task<int> AutorizacionAsync(int persona, string estado = "Aprobada", string modalidad = "Recurrente", string? inicio = null, string? fin = null, int? cupo = null, int? sede = null, int? area = null) =>
        api.ScalarAsync<int>(
            """
            INSERT INTO autorizacion (id_organizacion, id_persona, id_sede, id_area, id_membresia_responsable, id_usuario_crea, tipo_emisor, alcance,
                modalidad, fecha_inicio, fecha_fin, cupo_ingresos, ingresos_consumidos, estado, version, creado_en)
            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, 'Empresa', 'Privado', @p6, @p7::date, @p8::date, @p9, 0, @p10, 0, now()) RETURNING id_autorizacion
            """,
            _orgA, persona, sede ?? _sedeA, area ?? _areaA, _membresiaAdmin, _usuarioAdmin, modalidad, inicio ?? Hoy, (object?)fin ?? DBNull.Value, (object?)cupo ?? DBNull.Value, estado);

    private object Visita(int persona, int? autorizacion = null, int? sede = null, int? area = null, int? responsable = null, string? fecha = null) => new
    {
        idPersona = persona, idSede = sede ?? _sedeA, idArea = area ?? _areaA, idMembresiaResponsable = responsable ?? _membresiaAdmin,
        motivo = "Reunión comercial", idAutorizacion = autorizacion, fechaEsperada = fecha,
    };

    private static async Task<JsonElement> RegistrarAsync(HttpClient cliente, object visita) =>
        (await Leer(await cliente.PostAsJsonAsync("/api/visitas", visita), HttpStatusCode.Created)).GetProperty("visita");

    private static Task<HttpResponseMessage> Movimiento(HttpClient cliente, int visita, string tipo, int punto, string? clave = null)
    {
        var mensaje = new HttpRequestMessage(HttpMethod.Post, $"/api/visitas/{visita}/{tipo}") { Content = JsonContent.Create(new { idPuntoAcceso = punto }) };
        mensaje.Headers.Add("Idempotency-Key", clave ?? Guid.NewGuid().ToString());
        return cliente.SendAsync(mensaje);
    }

    private Task<long> Permanencias(int visita) => api.ScalarAsync<long>("SELECT count(*) FROM permanencia WHERE id_visita = @p0", visita);
    private Task<long> Movimientos(int visita) => api.ScalarAsync<long>("SELECT count(*) FROM movimiento_acceso WHERE id_visita = @p0", visita);
    private Task<string?> EstadoVisita(int visita) => api.ScalarAsync<string>("SELECT estado FROM visita WHERE id_visita = @p0", visita);
    private Task HabilitarSinAutorizacion() =>
        api.EjecutarAsync("INSERT INTO parametro_organizacion (id_organizacion, clave, valor, actualizado_en) VALUES (@p0, @p1, 'true', now()) ON CONFLICT DO NOTHING", _orgA, ReglasVisita.ParametroSinAutorizacion);

    // ---------- Registro y estados ----------

    [Fact]
    public async Task Sin_autorizacion_ni_politica_la_visita_queda_pendiente_y_no_puede_ingresar()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var visita = await RegistrarAsync(admin, Visita(persona));
        var id = visita.GetProperty("idVisita").GetInt32();
        Assert.Equal("Pendiente", visita.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, visita.GetProperty("politicaAplicada").ValueKind);

        var denegada = await Leer(await Movimiento(admin, id, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("pendiente de autorización", denegada.GetProperty("detail").GetString());
        Assert.Equal(0, await Permanencias(id));
        Assert.Equal(0, await Movimientos(id));
        Assert.Equal(1, await api.ScalarAsync<long>(
            "SELECT count(*) FROM auditoria WHERE entidad = 'Visita' AND id_registro = @p0 AND accion = 'RegistrarEntrada' AND resultado = 'Denegado' AND id_usuario = @p1", id.ToString(), _usuarioAdmin));
    }

    [Fact]
    public async Task Ciclo_completo_con_politica_sin_autorizacion_audita_y_actualiza_presencia()
    {
        await HabilitarSinAutorizacion();
        try
        {
            var admin = await Cliente("admin");
            var (persona, _) = await PersonaAsync(admin);
            var visita = await RegistrarAsync(admin, Visita(persona));
            var id = visita.GetProperty("idVisita").GetInt32();
            Assert.Equal("Autorizada", visita.GetProperty("estado").GetString());
            Assert.Equal(ReglasVisita.ParametroSinAutorizacion, visita.GetProperty("politicaAplicada").GetString());
            Assert.Equal(1, await api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE id_registro = @p0 AND accion = 'AplicarPoliticaSinAutorizacion' AND id_usuario = @p1", id.ToString(), _usuarioAdmin));

            var entrada = await Leer(await Movimiento(admin, id, "entrada", _puntoA), HttpStatusCode.OK);
            Assert.Equal("Ingreso", entrada.GetProperty("visita").GetProperty("estado").GetString());
            Assert.Equal("Abierta", entrada.GetProperty("permanencia").GetProperty("estado").GetString());
            Assert.True(entrada.GetProperty("evaluacion").GetProperty("puedeSalir").GetBoolean());

            var presencia = await admin.GetFromJsonAsync<JsonElement>($"/api/visitas/presencia?idSede={_sedeA}");
            Assert.Contains(presencia.EnumerateArray(), x => x.GetProperty("idVisita").GetInt32() == id && x.GetProperty("puntoEntrada").GetString() == "Portería principal");
            Assert.Equal(await api.ScalarAsync<long>("SELECT count(*) FROM permanencia WHERE id_organizacion = @p0 AND id_sede = @p1 AND estado = 'Abierta'", _orgA, _sedeA), presencia.GetArrayLength());

            var salida = await Leer(await Movimiento(admin, id, "salida", _puntoSoloSalida), HttpStatusCode.OK);
            Assert.Equal("Finalizada", salida.GetProperty("visita").GetProperty("estado").GetString());
            Assert.Equal("Cerrada", salida.GetProperty("permanencia").GetProperty("estado").GetString());
            Assert.True(salida.GetProperty("permanencia").GetProperty("duracionSegundos").GetInt64() >= 0);
            var movimientos = salida.GetProperty("movimientos").EnumerateArray().ToList();
            Assert.Equal(["Entrada", "Salida"], movimientos.Select(x => x.GetProperty("tipoMovimiento").GetString()));
            Assert.All(movimientos, x => Assert.Equal(Correo("admin"), x.GetProperty("operador").GetString()));

            presencia = await admin.GetFromJsonAsync<JsonElement>($"/api/visitas/presencia?idSede={_sedeA}");
            Assert.DoesNotContain(presencia.EnumerateArray(), x => x.GetProperty("idVisita").GetInt32() == id);
            foreach (var accion in new[] { "Registrar", "RegistrarEntrada", "RegistrarSalida" })
                Assert.Equal(1, await api.ScalarAsync<long>(
                    "SELECT count(*) FROM auditoria WHERE id_organizacion = @p0 AND entidad = 'Visita' AND id_registro = @p1 AND accion = @p2 AND resultado = 'Exitoso' AND id_usuario = @p3",
                    _orgA, id.ToString(), accion, _usuarioAdmin));

            // Un punto solo de salida no registra entradas.
            var otra = (await RegistrarAsync(admin, Visita(persona))).GetProperty("idVisita").GetInt32();
            await Leer(await Movimiento(admin, otra, "entrada", _puntoSoloSalida), HttpStatusCode.BadRequest);
        }
        finally
        {
            await api.EjecutarAsync("DELETE FROM parametro_organizacion WHERE id_organizacion = @p0", _orgA);
        }
    }

    [Fact]
    public async Task La_autorizacion_aprobada_y_vigente_habilita_y_la_unica_se_consume_una_sola_vez()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var autorizacion = await AutorizacionAsync(persona, modalidad: "Unica", fin: Hoy);
        var primera = await RegistrarAsync(admin, Visita(persona, autorizacion));
        Assert.Equal("Autorizada", primera.GetProperty("estado").GetString());
        await Leer(await Movimiento(admin, primera.GetProperty("idVisita").GetInt32(), "entrada", _puntoA), HttpStatusCode.OK);
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT ingresos_consumidos FROM autorizacion WHERE id_autorizacion = @p0", autorizacion));
        await Leer(await Movimiento(admin, primera.GetProperty("idVisita").GetInt32(), "salida", _puntoA), HttpStatusCode.OK);

        var segunda = (await RegistrarAsync(admin, Visita(persona, autorizacion))).GetProperty("idVisita").GetInt32();
        var denegada = await Leer(await Movimiento(admin, segunda, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("ingreso único ya fue utilizada", denegada.GetProperty("detail").GetString());
        Assert.Equal(0, await Permanencias(segunda));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT ingresos_consumidos FROM autorizacion WHERE id_autorizacion = @p0", autorizacion));
    }

    [Fact]
    public async Task Pendiente_rechazada_o_revocada_no_permiten_ingresar_y_la_visita_refleja_el_estado()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var autorizacion = await AutorizacionAsync(persona, estado: "Pendiente", fin: EnDias(3));
        var id = (await RegistrarAsync(admin, Visita(persona, autorizacion))).GetProperty("idVisita").GetInt32();
        Assert.Equal("Pendiente", await EstadoVisita(id));
        var pendiente = await Leer(await Movimiento(admin, id, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("pendiente de decisión", pendiente.GetProperty("detail").GetString());

        // La decisión (EP-07) la aprueba: la visita puede continuar sin cambiar nada más.
        await api.EjecutarAsync("UPDATE autorizacion SET estado = 'Aprobada' WHERE id_autorizacion = @p0", autorizacion);
        var detalle = await admin.GetFromJsonAsync<JsonElement>($"/api/visitas/{id}");
        Assert.True(detalle.GetProperty("evaluacion").GetProperty("puedeIngresar").GetBoolean());

        await api.EjecutarAsync("UPDATE autorizacion SET estado = 'Rechazada' WHERE id_autorizacion = @p0", autorizacion);
        await Leer(await Movimiento(admin, id, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Equal("Rechazada", await EstadoVisita(id));
        Assert.Equal(0, await Permanencias(id));

        var otraAutorizacion = await AutorizacionAsync(persona, fin: EnDias(3));
        var otra = (await RegistrarAsync(admin, Visita(persona, otraAutorizacion))).GetProperty("idVisita").GetInt32();
        await api.EjecutarAsync("UPDATE autorizacion SET estado = 'Revocada' WHERE id_autorizacion = @p0", otraAutorizacion);
        var revocada = await Leer(await Movimiento(admin, otra, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("revocada", revocada.GetProperty("detail").GetString());
        Assert.Equal("Cancelada", await EstadoVisita(otra));
    }

    [Fact]
    public async Task Fuera_de_vigencia_dia_horario_o_zona_se_rechaza_sin_abrir_permanencia()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);

        var futura = await AutorizacionAsync(persona, inicio: EnDias(1), fin: EnDias(5));
        var idFutura = (await RegistrarAsync(admin, Visita(persona, futura))).GetProperty("idVisita").GetInt32();
        var r1 = await Leer(await Movimiento(admin, idFutura, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("empieza a regir", r1.GetProperty("detail").GetString());

        var conHorario = await AutorizacionAsync(persona, fin: EnDias(5));
        var otroDia = ReglasVisita.DiaIso(DateTime.UtcNow) % 7 + 1;
        await api.EjecutarAsync("INSERT INTO autorizacion_horario (id_organizacion, id_autorizacion, dia_semana, hora_inicio, hora_fin) VALUES (@p0, @p1, @p2, '00:00', '23:59')", _orgA, conHorario, otroDia);
        var idHorario = (await RegistrarAsync(admin, Visita(persona, conHorario))).GetProperty("idVisita").GetInt32();
        var r2 = await Leer(await Movimiento(admin, idHorario, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("día de la semana", r2.GetProperty("detail").GetString());

        var conZona = await AutorizacionAsync(persona, fin: EnDias(5));
        await api.EjecutarAsync("INSERT INTO autorizacion_zona (id_organizacion, id_autorizacion, id_zona) VALUES (@p0, @p1, @p2)", _orgA, conZona, _zonaOtra);
        var idZona = (await RegistrarAsync(admin, Visita(persona, conZona))).GetProperty("idVisita").GetInt32();
        var r3 = await Leer(await Movimiento(admin, idZona, "entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("zonas autorizadas", r3.GetProperty("detail").GetString());

        foreach (var id in new[] { idFutura, idHorario, idZona }) Assert.Equal(0, await Permanencias(id));

        // Autorización de otra sede o destino no se puede vincular.
        var otraSede = await AutorizacionAsync(persona, sede: _sedeA2, area: _areaA2, fin: EnDias(5));
        await Leer(await admin.PostAsJsonAsync("/api/visitas", Visita(persona, otraSede)), HttpStatusCode.BadRequest);
    }

    [Fact]
    public void Horario_es_inclusivo_al_inicio_y_exclusivo_al_fin_en_la_hora_local()
    {
        var visita = new Visita { IdPersona = 1, IdSede = 1, IdArea = 1 };
        var autorizacion = new Autorizacion { IdPersona = 1, IdSede = 1, IdArea = 1, Estado = "Aprobada", Modalidad = "Recurrente", FechaInicio = new DateOnly(2026, 10, 1) };
        var lunes = new DateTime(2026, 10, 26, 8, 0, 0);
        var horarios = new[] { new AutorizacionHorario { DiaSemana = 1, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(17, 0) } };
        Assert.Empty(ReglasVisita.MotivosAutorizacion(autorizacion, horarios, [], visita, null, lunes));
        Assert.NotEmpty(ReglasVisita.MotivosAutorizacion(autorizacion, horarios, [], visita, null, lunes.AddHours(9)));
        Assert.NotEmpty(ReglasVisita.MotivosAutorizacion(autorizacion, horarios, [], visita, null, lunes.AddMinutes(-1)));
        Assert.Equal(new DateTime(2026, 10, 26, 3, 0, 0), ReglasVisita.HoraLocal(new DateTime(2026, 10, 26, 8, 0, 0, DateTimeKind.Utc), "America/Bogota"));
    }

    [Fact]
    public async Task Cancelar_solo_antes_del_ingreso_y_la_salida_ignora_la_revocacion_posterior()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var pendiente = (await RegistrarAsync(admin, Visita(persona))).GetProperty("idVisita").GetInt32();
        await Leer(await admin.PostAsJsonAsync($"/api/visitas/{pendiente}/cancelar", new { motivo = "" }), HttpStatusCode.BadRequest);
        var cancelada = await Leer(await admin.PostAsJsonAsync($"/api/visitas/{pendiente}/cancelar", new { motivo = "El visitante no llegó" }), HttpStatusCode.OK);
        Assert.Equal("Cancelada", cancelada.GetProperty("visita").GetProperty("estado").GetString());
        await Leer(await Movimiento(admin, pendiente, "entrada", _puntoA), HttpStatusCode.Conflict);

        var autorizacion = await AutorizacionAsync(persona, fin: EnDias(2));
        var dentro = (await RegistrarAsync(admin, Visita(persona, autorizacion))).GetProperty("idVisita").GetInt32();
        await Leer(await Movimiento(admin, dentro, "entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await admin.PostAsJsonAsync($"/api/visitas/{dentro}/cancelar", new { motivo = "x" }), HttpStatusCode.Conflict);
        await api.EjecutarAsync("UPDATE autorizacion SET estado = 'Revocada' WHERE id_autorizacion = @p0", autorizacion);
        await Leer(await Movimiento(admin, dentro, "salida", _puntoA), HttpStatusCode.OK);

        // Segunda salida (otra clave) y salida sin entrada: inconsistencias explícitas, sin movimientos nuevos.
        var repetida = await Leer(await Movimiento(admin, dentro, "salida", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("ya fue registrada", repetida.GetProperty("detail").GetString());
        Assert.Equal(2, await Movimientos(dentro));
        var sinEntrada = (await RegistrarAsync(admin, Visita(persona, await AutorizacionAsync(persona, fin: EnDias(2))))).GetProperty("idVisita").GetInt32();
        var inconsistencia = await Leer(await Movimiento(admin, sinEntrada, "salida", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains("no tiene una entrada abierta", inconsistencia.GetProperty("detail").GetString());
        Assert.Equal(0, await Movimientos(sinEntrada));
    }

    // ---------- Idempotencia y concurrencia ----------

    [Fact]
    public async Task Reenviar_la_misma_entrada_no_duplica_y_una_clave_reutilizada_con_otro_cuerpo_se_rechaza()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var id = (await RegistrarAsync(admin, Visita(persona, await AutorizacionAsync(persona, fin: EnDias(2))))).GetProperty("idVisita").GetInt32();

        var sinClave = await admin.PostAsJsonAsync($"/api/visitas/{id}/entrada", new { idPuntoAcceso = _puntoA });
        Assert.Equal(HttpStatusCode.BadRequest, sinClave.StatusCode);

        var clave = Guid.NewGuid().ToString();
        var primera = await Leer(await Movimiento(admin, id, "entrada", _puntoA, clave), HttpStatusCode.OK);
        var reintento = await Movimiento(admin, id, "entrada", _puntoA, clave);
        var segunda = await Leer(reintento, HttpStatusCode.OK);
        Assert.True(reintento.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal(primera.GetProperty("permanencia").GetProperty("idPermanencia").GetInt32(), segunda.GetProperty("permanencia").GetProperty("idPermanencia").GetInt32());
        Assert.Equal(1, await Movimientos(id));
        Assert.Equal(1, await Permanencias(id));

        await Leer(await Movimiento(admin, id, "salida", _puntoA, clave), HttpStatusCode.Conflict);
        var claveSalida = Guid.NewGuid().ToString();
        await Leer(await Movimiento(admin, id, "salida", _puntoA, claveSalida), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, id, "salida", _puntoA, claveSalida), HttpStatusCode.OK);
        Assert.Equal(2, await Movimientos(id));
    }

    [Fact]
    public async Task Dos_entradas_simultaneas_crean_una_sola_permanencia()
    {
        var admin = await Cliente("admin");
        var otro = await Cliente("soloentrada");
        var (persona, _) = await PersonaAsync(admin);
        var id = (await RegistrarAsync(admin, Visita(persona, await AutorizacionAsync(persona, fin: EnDias(2), cupo: 5)))).GetProperty("idVisita").GetInt32();

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Movimiento(i % 2 == 0 ? admin : otro, id, "entrada", _puntoA)));
        Assert.Equal(1, respuestas.Count(x => x.StatusCode == HttpStatusCode.OK));
        Assert.All(respuestas.Where(x => x.StatusCode != HttpStatusCode.OK), x => Assert.Equal(HttpStatusCode.Conflict, x.StatusCode));
        Assert.Equal(1, await Permanencias(id));
        Assert.Equal(1, await Movimientos(id));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT ingresos_consumidos FROM autorizacion a JOIN visita v ON v.id_autorizacion = a.id_autorizacion WHERE v.id_visita = @p0", id));

        // Otra visita de la misma persona en la misma sede no abre una segunda permanencia (D-07, Propuesta).
        await HabilitarSinAutorizacion();
        try
        {
            var segunda = (await RegistrarAsync(admin, Visita(persona))).GetProperty("idVisita").GetInt32();
            var r = await Leer(await Movimiento(admin, segunda, "entrada", _puntoA), HttpStatusCode.Conflict);
            Assert.Contains("ya está dentro de esta sede", r.GetProperty("detail").GetString());
        }
        finally
        {
            await api.EjecutarAsync("DELETE FROM parametro_organizacion WHERE id_organizacion = @p0", _orgA);
        }
    }

    // ---------- Retiro del tipo de permiso de acceso (2026-10-28) ----------

    [Fact]
    public async Task Personas_ya_no_maneja_tipo_de_acceso_y_visitas_no_exige_excepcion()
    {
        var admin = await Cliente("admin");
        var (persona, documento) = await PersonaAsync(admin);
        // Un tipoAcceso enviado por un cliente antiguo se ignora: no se persiste ni se devuelve.
        var editada = await Leer(await admin.PutAsJsonAsync($"/api/personas/{persona}", new
        {
            idTipoPersona = _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = documento, nombres = "Ana", apellidos = $"Visitante {_sufijo}", correo = "ana@ejemplo.test", tipoAcceso = "Recurrente",
        }), HttpStatusCode.OK);
        Assert.False(editada.TryGetProperty("tipoAcceso", out _));
        Assert.Equal(0, await api.ScalarAsync<long>("SELECT count(*) FROM information_schema.columns WHERE table_name = 'persona' AND column_name = 'tipo_acceso'"));

        var visita = await RegistrarAsync(admin, new { idPersona = persona, idSede = _sedeA, idArea = _areaA, idMembresiaResponsable = _membresiaAdmin, motivo = "Reunión", excepcionTipoAcceso = true, motivoExcepcion = "No aplica" });
        Assert.False(visita.GetProperty("excepcionTipoAcceso").GetBoolean());
        Assert.False(visita.TryGetProperty("tipoAccesoPersona", out _));
    }

    // ---------- Aislamiento, alcance y permisos ----------

    [Fact]
    public async Task Otra_organizacion_no_consulta_ni_opera_las_visitas_aunque_conozca_el_id()
    {
        var admin = await Cliente("admin");
        var adminB = await Cliente("adminb");
        var (persona, _) = await PersonaAsync(admin);
        var id = (await RegistrarAsync(admin, Visita(persona, await AutorizacionAsync(persona, fin: EnDias(2))))).GetProperty("idVisita").GetInt32();

        await Leer(await adminB.GetAsync($"/api/visitas/{id}"), HttpStatusCode.NotFound);
        await Leer(await Movimiento(adminB, id, "entrada", _puntoB), HttpStatusCode.NotFound);
        await Leer(await adminB.PostAsJsonAsync($"/api/visitas/{id}/cancelar", new { motivo = "intento" }), HttpStatusCode.NotFound);
        var listaB = await adminB.GetFromJsonAsync<JsonElement>("/api/visitas?tamano=100");
        Assert.DoesNotContain(listaB.GetProperty("registros").EnumerateArray(), x => x.GetProperty("idVisita").GetInt32() == id);
        // B no puede usar la persona, la sede ni el responsable de A.
        await Leer(await adminB.PostAsJsonAsync("/api/visitas", Visita(persona, sede: _sedeB, area: _areaB, responsable: _membresiaAdminB)), HttpStatusCode.BadRequest);
        await Leer(await adminB.PostAsJsonAsync("/api/visitas", Visita(persona, sede: _sedeA)), HttpStatusCode.BadRequest);
        Assert.Equal("Autorizada", await EstadoVisita(id));
    }

    [Fact]
    public async Task El_operador_solo_ve_y_opera_las_sedes_asignadas()
    {
        var admin = await Cliente("admin");
        var operador = await Cliente("operadora2");
        var (persona, _) = await PersonaAsync(admin);
        var enA = (await RegistrarAsync(admin, Visita(persona, await AutorizacionAsync(persona, fin: EnDias(2))))).GetProperty("idVisita").GetInt32();
        var enA2 = (await RegistrarAsync(admin, Visita(persona, await AutorizacionAsync(persona, sede: _sedeA2, area: _areaA2, fin: EnDias(2)), sede: _sedeA2, area: _areaA2))).GetProperty("idVisita").GetInt32();

        await Leer(await operador.GetAsync($"/api/visitas/{enA}"), HttpStatusCode.NotFound);
        await Leer(await Movimiento(operador, enA, "entrada", _puntoA), HttpStatusCode.NotFound);
        await Leer(await operador.PostAsJsonAsync("/api/visitas", Visita(persona)), HttpStatusCode.Forbidden);
        var lista = await operador.GetFromJsonAsync<JsonElement>("/api/visitas?tamano=100");
        Assert.All(lista.GetProperty("registros").EnumerateArray(), x => Assert.Equal(_sedeA2, x.GetProperty("idSede").GetInt32()));
        var opciones = await operador.GetFromJsonAsync<JsonElement>("/api/visitas/opciones");
        Assert.Equal([_sedeA2], opciones.GetProperty("sedes").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()));

        await Leer(await Movimiento(operador, enA2, "entrada", _puntoA2), HttpStatusCode.OK);
        // Un punto de otra sede no sirve para la visita.
        await Leer(await Movimiento(operador, enA2, "salida", _puntoA), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Los_permisos_de_consultar_registrar_entrada_y_salida_son_independientes()
    {
        var admin = await Cliente("admin");
        var (persona, _) = await PersonaAsync(admin);
        var id = (await RegistrarAsync(admin, Visita(persona, await AutorizacionAsync(persona, fin: EnDias(2), cupo: 3)))).GetProperty("idVisita").GetInt32();

        var consulta = await Cliente("consulta");
        await Leer(await consulta.GetAsync($"/api/visitas/{id}"), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PostAsJsonAsync("/api/visitas", Visita(persona))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PostAsJsonAsync($"/api/visitas/{id}/cancelar", new { motivo = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Movimiento(consulta, id, "entrada", _puntoA)).StatusCode);

        var sinAccesos = await Cliente("sinaccesos");
        Assert.Equal(HttpStatusCode.Forbidden, (await Movimiento(sinAccesos, id, "entrada", _puntoA)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sinAccesos.GetAsync("/api/visitas/presencia")).StatusCode);
        // Sin ControlAccesos.Consultar el detalle no expone el historial de movimientos.
        var soloEntrada = await Cliente("soloentrada");
        await Leer(await Movimiento(soloEntrada, id, "entrada", _puntoA), HttpStatusCode.OK);
        Assert.Equal(0, (await sinAccesos.GetFromJsonAsync<JsonElement>($"/api/visitas/{id}")).GetProperty("movimientos").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await Movimiento(soloEntrada, id, "salida", _puntoA)).StatusCode);
        Assert.Equal(1, await Movimientos(id));
    }

    // ---------- Consulta ----------

    [Fact]
    public async Task La_busqueda_por_documento_reutiliza_la_persona_y_la_lista_pagina_y_cuenta_en_servidor()
    {
        var admin = await Cliente("admin");
        var (persona, documento) = await PersonaAsync(admin);
        var encontrada = await Leer(await admin.GetAsync($"/api/personas/buscar-documento?idTipoDocumento={_cc}&numero=%20{documento}%20"), HttpStatusCode.OK);
        Assert.Equal(persona, encontrada.GetProperty("idPersona").GetInt32());
        // Registrar de nuevo el mismo documento no crea un duplicado.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = documento, nombres = "Ana", apellidos = "Duplicada", correo = "ana@ejemplo.test",
        })).StatusCode);

        for (var i = 0; i < 3; i++) await RegistrarAsync(admin, Visita(persona));
        var pagina = await admin.GetFromJsonAsync<JsonElement>($"/api/visitas?buscar={documento}&tamano=2&pagina=2&orden=creado_asc");
        Assert.Equal(3, pagina.GetProperty("total").GetInt32());
        Assert.Equal(1, pagina.GetProperty("registros").GetArrayLength());
        Assert.Equal(3, pagina.GetProperty("indicadores").GetProperty("pendientes").GetInt32());
        var porEstado = await admin.GetFromJsonAsync<JsonElement>($"/api/visitas?buscar={documento}&estado=Autorizada");
        Assert.Equal(0, porEstado.GetProperty("total").GetInt32());
        Assert.Equal(3, porEstado.GetProperty("indicadores").GetProperty("pendientes").GetInt32());
    }
}
