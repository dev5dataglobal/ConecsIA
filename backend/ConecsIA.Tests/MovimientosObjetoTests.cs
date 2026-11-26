using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;
using Npgsql;

namespace ConecsIA.Tests;

/// <summary>
/// Entradas y salidas de vehículos y dispositivos en portería (RF-16, RF-17, CA-23..CA-25; ADR-030): vínculo con el episodio
/// y el movimiento de la persona, decisión propia del objeto, «dentro» derivado, aviso sin cierre automático al salir la
/// persona (D-10), idempotencia, concurrencia, permisos por módulo, aislamiento entre organizaciones e historial inmutable.
/// </summary>
public sealed class MovimientosObjetoTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _sedeA, _areaA, _puntoA, _puntoSoloEntrada, _membresiaAdmin, _tipoPersona, _cc, _catAutomatica, _catSeguridad, _tipoDispositivo;
    private string _sufijo = "";
    private static string Hoy => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A obj {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B obj {_sufijo}");
        var rolAdmin = await api.CrearRolAsync(_orgA, "Administrador");
        foreach (var m in new[] { "personas", "control_accesos", "autorizaciones", "vehiculos", "dispositivos" }) await api.AsignarModuloAsync(_orgA, rolAdmin, m);
        await api.CrearUsuarioAsync(Correo("admin"), _orgA, rolAdmin);
        // Portería sin módulos de vehículos ni dispositivos.
        var rolPorteria = await api.CrearRolAsync(_orgA, "Portería");
        foreach (var m in new[] { "personas", "control_accesos" }) await api.AsignarModuloAsync(_orgA, rolPorteria, m);
        await api.CrearUsuarioAsync(Correo("porteria"), _orgA, rolPorteria);
        // Solo entradas: no registra salidas.
        var rolEntradas = await api.CrearRolAsync(_orgA, "Solo entradas");
        await api.AsignarModuloAsync(_orgA, rolEntradas, "control_accesos", consultar: true, anadir: true, editar: false);
        await api.AsignarModuloAsync(_orgA, rolEntradas, "vehiculos");
        await api.CrearUsuarioAsync(Correo("entradas"), _orgA, rolEntradas);
        var rolB = await api.CrearRolAsync(_orgB, "Administrador B");
        foreach (var m in new[] { "personas", "control_accesos", "autorizaciones", "vehiculos", "dispositivos" }) await api.AsignarModuloAsync(_orgB, rolB, m);
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, rolB);

        var ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad");
        _sedeA = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede A', 'UTC', true) RETURNING id_sede", _orgA, ciudad);
        _areaA = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Gerencia', false, true) RETURNING id_area", _orgA, _sedeA);
        _puntoA = await api.ScalarAsync<int>("INSERT INTO punto_acceso (id_organizacion, id_sede, nombre, modo_acceso, activo, creado_en) VALUES (@p0, @p1, 'Portería vehicular', 'Ambos', true, now()) RETURNING id_punto_acceso", _orgA, _sedeA);
        _puntoSoloEntrada = await api.ScalarAsync<int>("INSERT INTO punto_acceso (id_organizacion, id_sede, nombre, modo_acceso, activo, creado_en) VALUES (@p0, @p1, 'Torniquete entrada', 'Entrada', true, now()) RETURNING id_punto_acceso", _orgA, _sedeA);
        _membresiaAdmin = await api.ScalarAsync<int>("SELECT m.id_membresia FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario WHERE u.correo = @p0", Correo("admin"));
        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersona = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Contratista', true) RETURNING id_tipo_persona", _orgA);
        _tipoDispositivo = await api.ScalarAsync<int>("INSERT INTO tipo_dispositivo (id_organizacion, nombre, activo) VALUES (@p0, 'Portátil', true) RETURNING id_tipo_dispositivo", _orgA);
        _catAutomatica = await Categoria("PROVEEDOR", "Proveedor", "Automatico");
        _catSeguridad = await Categoria("CONTRATISTA", "Contratista", "Seguridad");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- Apoyo ----------

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@objetos.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));

    private Task<int> Categoria(string codigo, string nombre, string flujo) =>
        api.ScalarAsync<int>("""
            INSERT INTO categoria_acceso (id_organizacion, codigo, nombre, flujo_aprobacion, modalidades_permitidas, exige_anfitrion, exige_motivo, permite_vehiculos, permite_dispositivos, orden, activo, creado_en)
            VALUES (@p0, @p1, @p2, @p3, '{Unica,Diaria,Temporal}'::text[], true, false, true, true, 0, true, now()) RETURNING id_categoria_acceso
            """, _orgA, codigo, nombre, flujo);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(esperado == respuesta.StatusCode, $"Se esperaba {(int)esperado} y llegó {(int)respuesta.StatusCode}: {cuerpo}");
        return cuerpo.Length == 0 ? default : JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private async Task<int> PersonaAsync()
    {
        var persona = await Leer(await (await Cliente("admin")).PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = _tipoPersona, idTipoDocumento = _cc, numeroDocumento = Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString(),
            nombres = "Luis", apellidos = $"Conductor {_sufijo}", correo = "luis@objetos.test",
        }), HttpStatusCode.Created);
        return persona.GetProperty("idPersona").GetInt32();
    }

    private async Task<int> VehiculoAsync(params int[] personas)
    {
        var placa = $"V{Random.Shared.Next(10000, 99999)}";
        var vehiculo = await api.ScalarAsync<int>("INSERT INTO vehiculo (id_organizacion, placa, placa_normalizada, tipo, marca, activo) VALUES (@p0, @p1, @p1, 'Camioneta', 'Toyota', true) RETURNING id_vehiculo", _orgA, placa);
        foreach (var persona in personas)
            await api.EjecutarAsync("INSERT INTO persona_vehiculo (id_organizacion, id_persona, id_vehiculo, relacion, activo) VALUES (@p0, @p1, @p2, 'Conductor', true)", _orgA, persona, vehiculo);
        return vehiculo;
    }

    private Task<int> DispositivoAsync(int persona) =>
        api.ScalarAsync<int>("""
            INSERT INTO dispositivo (id_organizacion, id_persona_responsable, id_tipo_dispositivo, codigo_identificador, activo, creado_en)
            VALUES (@p0, @p1, @p2, @p3, true, now()) RETURNING id_dispositivo
            """, _orgA, persona, _tipoDispositivo, $"PC-{Random.Shared.Next(100000, 999999)}");

    /// <summary>Crea el registro (con el objeto) y devuelve (idRegistro, idAutorizacionObjeto).</summary>
    private async Task<(int Registro, int Objeto)> RegistroConObjetoAsync(HttpClient cliente, int persona, string tipo, int idObjeto, int? categoria = null)
    {
        var registro = await Leer(await cliente.PostAsJsonAsync("/api/accesos", new
        {
            idPersona = persona, idCategoriaAcceso = categoria ?? _catAutomatica, idSede = _sedeA, idArea = _areaA, idMembresiaResponsable = _membresiaAdmin,
            modalidad = "Temporal", fechaInicio = Hoy, fechaFin = Hoy, horaInicio = "00:00:00", horaFin = "23:59:59", motivo = "Entrega de materiales",
            objetos = new[] { new { tipoObjeto = tipo, idObjeto } },
        }), HttpStatusCode.Created);
        return (registro.GetProperty("idAutorizacion").GetInt32(), registro.GetProperty("objetos")[0].GetProperty("idAutorizacionObjeto").GetInt32());
    }

    private static Task<HttpResponseMessage> Post(HttpClient cliente, string ruta, int punto, string? clave = null)
    {
        var mensaje = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = JsonContent.Create(new { idPuntoAcceso = punto }) };
        mensaje.Headers.Add("Idempotency-Key", clave ?? Guid.NewGuid().ToString());
        return cliente.SendAsync(mensaje);
    }

    private static List<string> Motivos(JsonElement problema) => problema.GetProperty("motivos").EnumerateArray().Select(x => x.GetString()!).ToList();

    private Task<long> ContarMovimientos(int idObjeto) =>
        api.ScalarAsync<long>("SELECT count(*) FROM movimiento_objeto WHERE id_autorizacion_objeto = @p0", idObjeto);

    // ---------- Pruebas ----------

    [Fact]
    public async Task La_entrada_del_vehiculo_exige_a_la_persona_dentro_y_queda_vinculada_a_su_episodio()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Vehiculo", await VehiculoAsync(persona));

        var antes = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(antes), m => m.Contains("registra primero su entrada"));
        Assert.Equal(0, await ContarMovimientos(objeto));

        var entradaPersona = await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);
        var entrada = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.OK);
        Assert.Equal("Entrada", entrada.GetProperty("tipoMovimiento").GetString());
        Assert.Equal(entradaPersona.GetProperty("idVisita").GetInt32(), entrada.GetProperty("idVisita").GetInt32());

        var fila = await api.ScalarAsync<string>("""
            SELECT concat_ws('|', id_visita, id_movimiento_acceso, id_sede, id_punto_acceso, id_persona_responsable, tipo_objeto,
                (SELECT correo FROM usuario WHERE id_usuario = id_usuario_registra)) FROM movimiento_objeto WHERE id_movimiento_objeto = @p0
            """, entrada.GetProperty("idMovimientoObjeto").GetInt64());
        Assert.Equal(string.Join('|', entradaPersona.GetProperty("idVisita").GetInt32(), entradaPersona.GetProperty("idMovimiento").GetInt64(), _sedeA, _puntoA, persona, "Vehiculo", Correo("admin")), fila);

        var detalle = await Leer(await admin.GetAsync($"/api/accesos/{registro}"), HttpStatusCode.OK);
        Assert.True(detalle.GetProperty("objetos")[0].GetProperty("dentro").GetBoolean());
        var dentro = await Leer(await admin.GetAsync($"/api/accesos/objetos-dentro?idSede={_sedeA}"), HttpStatusCode.OK);
        var item = dentro.EnumerateArray().Single(x => x.GetProperty("idAutorizacionObjeto").GetInt32() == objeto);
        Assert.True(item.GetProperty("personaDentro").GetBoolean());
        Assert.StartsWith("Vehículo V", item.GetProperty("objeto").GetString());
    }

    [Fact]
    public async Task La_salida_de_la_persona_no_cierra_el_vehiculo_y_lo_avisa()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Vehiculo", await VehiculoAsync(persona));
        var entradaPersona = await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.OK);

        var presencia = await Leer(await admin.GetAsync($"/api/accesos/presencia?idSede={_sedeA}"), HttpStatusCode.OK);
        Assert.Single(presencia.EnumerateArray().Single(x => x.GetProperty("idAutorizacion").GetInt32() == registro).GetProperty("objetosDentro").EnumerateArray());

        var salidaPersona = await Leer(await Post(admin, $"/api/accesos/permanencias/{entradaPersona.GetProperty("idPermanencia").GetInt32()}/salida", _puntoA), HttpStatusCode.OK);
        Assert.Single(salidaPersona.GetProperty("objetosDentro").EnumerateArray());
        Assert.Equal(1, await ContarMovimientos(objeto));
        var dentro = await Leer(await admin.GetAsync("/api/accesos/objetos-dentro"), HttpStatusCode.OK);
        Assert.False(dentro.EnumerateArray().Single(x => x.GetProperty("idAutorizacionObjeto").GetInt32() == objeto).GetProperty("personaDentro").GetBoolean());

        // La salida del objeto no exige a la persona dentro y conserva su episodio.
        var salida = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/salida", _puntoA), HttpStatusCode.OK);
        Assert.True(salida.GetProperty("personaFuera").GetBoolean());
        Assert.True(salida.GetProperty("duracionSegundos").GetInt64() >= 0);
        Assert.Equal(entradaPersona.GetProperty("idVisita").GetInt32(), salida.GetProperty("idVisita").GetInt32());
        dentro = await Leer(await admin.GetAsync("/api/accesos/objetos-dentro"), HttpStatusCode.OK);
        Assert.DoesNotContain(dentro.EnumerateArray(), x => x.GetProperty("idAutorizacionObjeto").GetInt32() == objeto);

        // Una segunda salida es una inconsistencia visible que no modifica el historial.
        var repetida = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/salida", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(repetida), m => m.Contains("no tiene una entrada abierta"));
        Assert.Equal(2, await ContarMovimientos(objeto));
        Assert.Equal(1L, await api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE entidad = 'AutorizacionObjeto' AND id_registro = @p0 AND accion = 'RegistrarSalidaObjeto' AND resultado = 'Denegado'", objeto.ToString()));
    }

    [Fact]
    public async Task Reenviar_la_misma_solicitud_no_duplica_y_entradas_simultaneas_dejan_una_sola()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Vehiculo", await VehiculoAsync(persona));
        await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);

        var clave = Guid.NewGuid().ToString();
        var primera = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA, clave), HttpStatusCode.OK);
        var replica = await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA, clave);
        Assert.Equal("true", replica.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal(primera.GetProperty("idMovimientoObjeto").GetInt64(), (await Leer(replica, HttpStatusCode.OK)).GetProperty("idMovimientoObjeto").GetInt64());
        Assert.Equal(1, await ContarMovimientos(objeto));

        await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/salida", _puntoA), HttpStatusCode.OK);
        var clientes = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Cliente("admin")));
        var respuestas = await Task.WhenAll(clientes.Select(c => Post(c, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA)));
        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(3, await ContarMovimientos(objeto));
    }

    [Fact]
    public async Task Un_vehiculo_pendiente_rechazado_o_de_un_registro_no_aprobado_no_ingresa()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Vehiculo", await VehiculoAsync(persona), _catSeguridad);
        // El registro aún no está aprobado: tampoco entra la persona.
        var sinAprobar = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(sinAprobar), m => m.Contains("registro de acceso está pendiente"));

        await Leer(await admin.PostAsJsonAsync($"/api/accesos/{registro}/aprobar", new { motivo = (string?)null }), HttpStatusCode.OK);
        await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);
        var pendiente = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(pendiente), m => m.Contains("pendiente de aprobación"));

        await Leer(await admin.PostAsJsonAsync($"/api/accesos/{registro}/objetos/{objeto}/rechazar", new { motivo = "Sin SOAT vigente" }), HttpStatusCode.OK);
        var rechazado = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(rechazado), m => m.Contains("rechazada"));
        Assert.Equal(0, await ContarMovimientos(objeto));
    }

    [Fact]
    public async Task Un_vehiculo_compartido_no_puede_estar_dentro_dos_veces_y_sale_por_su_registro()
    {
        var admin = await Cliente("admin");
        var persona1 = await PersonaAsync();
        var persona2 = await PersonaAsync();
        var vehiculo = await VehiculoAsync(persona1, persona2);
        var (registro1, objeto1) = await RegistroConObjetoAsync(admin, persona1, "Vehiculo", vehiculo);
        var (registro2, objeto2) = await RegistroConObjetoAsync(admin, persona2, "Vehiculo", vehiculo);
        await Leer(await Post(admin, $"/api/accesos/{registro1}/entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await Post(admin, $"/api/accesos/{registro2}/entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await Post(admin, $"/api/accesos/{registro1}/objetos/{objeto1}/entrada", _puntoA), HttpStatusCode.OK);

        var doble = await Leer(await Post(admin, $"/api/accesos/{registro2}/objetos/{objeto2}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(doble), m => m.Contains("ya está dentro"));
        var otraSalida = await Leer(await Post(admin, $"/api/accesos/{registro2}/objetos/{objeto2}/salida", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(otraSalida), m => m.Contains($"registro de acceso #{registro1}"));
        // El detalle del segundo registro también lo muestra dentro (estado físico del vehículo).
        Assert.True((await Leer(await admin.GetAsync($"/api/accesos/{registro2}"), HttpStatusCode.OK)).GetProperty("objetos")[0].GetProperty("dentro").GetBoolean());
    }

    [Fact]
    public async Task Una_revocacion_posterior_no_impide_la_salida_del_dispositivo()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Dispositivo", await DispositivoAsync(persona));
        await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);
        var entrada = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoSoloEntrada), HttpStatusCode.OK);
        Assert.Equal("Dispositivo", entrada.GetProperty("tipoObjeto").GetString());

        await Leer(await admin.PostAsJsonAsync($"/api/accesos/{registro}/objetos/{objeto}/revocar", new { motivo = "Equipo reportado" }), HttpStatusCode.OK);
        // El punto solo de entrada no registra salidas.
        await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/salida", _puntoSoloEntrada), HttpStatusCode.BadRequest);
        await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/salida", _puntoA), HttpStatusCode.OK);
        // Ya revocado, no puede volver a ingresar.
        var reingreso = await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.Conflict);
        Assert.Contains(Motivos(reingreso), m => m.Contains("revocada"));
    }

    [Fact]
    public async Task Exige_el_modulo_del_objeto_y_la_accion_de_control_de_accesos()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Vehiculo", await VehiculoAsync(persona));
        await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);

        await Leer(await Post(await Cliente("porteria"), $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.Forbidden);
        var porteriaDentro = await Leer(await (await Cliente("porteria")).GetAsync("/api/accesos/objetos-dentro"), HttpStatusCode.OK);
        Assert.Empty(porteriaDentro.EnumerateArray());

        var entradas = await Cliente("entradas");
        await Leer(await Post(entradas, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await Post(entradas, $"/api/accesos/{registro}/objetos/{objeto}/salida", _puntoA), HttpStatusCode.Forbidden);
        Assert.Equal(1, await ContarMovimientos(objeto));
    }

    [Fact]
    public async Task Otra_organizacion_no_registra_ni_ve_los_objetos_dentro()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Vehiculo", await VehiculoAsync(persona));
        await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.OK);

        var adminB = await Cliente("adminb");
        await Leer(await Post(adminB, $"/api/accesos/{registro}/objetos/{objeto}/salida", _puntoA), HttpStatusCode.NotFound);
        Assert.Empty((await Leer(await adminB.GetAsync("/api/accesos/objetos-dentro"), HttpStatusCode.OK)).EnumerateArray());
        Assert.Equal(1, await ContarMovimientos(objeto));
    }

    [Fact]
    public async Task El_historial_de_movimientos_de_objetos_es_de_solo_insercion()
    {
        var admin = await Cliente("admin");
        var persona = await PersonaAsync();
        var (registro, objeto) = await RegistroConObjetoAsync(admin, persona, "Vehiculo", await VehiculoAsync(persona));
        await Leer(await Post(admin, $"/api/accesos/{registro}/entrada", _puntoA), HttpStatusCode.OK);
        await Leer(await Post(admin, $"/api/accesos/{registro}/objetos/{objeto}/entrada", _puntoA), HttpStatusCode.OK);

        await Assert.ThrowsAsync<PostgresException>(() => api.EjecutarAsync("UPDATE movimiento_objeto SET observacion = 'x' WHERE id_autorizacion_objeto = @p0", objeto));
        await Assert.ThrowsAsync<PostgresException>(() => api.EjecutarAsync("DELETE FROM movimiento_objeto WHERE id_autorizacion_objeto = @p0", objeto));
        // Una salida sin entrada que cerrar viola el CHECK.
        await Assert.ThrowsAsync<PostgresException>(() => api.EjecutarAsync("""
            INSERT INTO movimiento_objeto (id_organizacion, tipo_objeto, id_vehiculo, id_autorizacion_objeto, id_autorizacion, id_visita, id_persona_responsable, id_sede, id_punto_acceso, id_usuario_registra, tipo_movimiento, ocurrido_en)
            SELECT id_organizacion, tipo_objeto, id_vehiculo, id_autorizacion_objeto, id_autorizacion, id_visita, id_persona_responsable, id_sede, id_punto_acceso, id_usuario_registra, 'Salida', now()
            FROM movimiento_objeto WHERE id_autorizacion_objeto = @p0
            """, objeto));
        Assert.Equal(1, await ContarMovimientos(objeto));
    }
}
