using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Puntos de acceso por sede (RF-05, ADR-028): Sede → Punto de acceso, sin zonas de acceso. Unicidad por sede, aislamiento por
/// organización, permisos del módulo «sedes», eliminación solo sin historial, marca común de la zona empresarial (solo si la
/// sede tiene zona) y movimientos que identifican sede y punto; la autorización vale en cualquier punto de su sede.
/// </summary>
public sealed class PuntosAccesoTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _sedeA, _sedeA2, _sedeB, _areaA, _membresiaAdmin, _tipoPersonaA, _cc, _ciudad;
    private string _sufijo = "";
    private static string Hoy => DateTime.UtcNow.ToString("yyyy-MM-dd");

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");
        var rolAdmin = await api.CrearRolAsync(_orgA, "Administrador");
        foreach (var m in new[] { "sedes", "personas", "visitas", "control_accesos", "autorizaciones" }) await api.AsignarModuloAsync(_orgA, rolAdmin, m);
        await api.CrearUsuarioAsync(Correo("admin"), _orgA, rolAdmin);
        var rolLector = await api.CrearRolAsync(_orgA, "Lector");
        await api.AsignarModuloAsync(_orgA, rolLector, "sedes", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("lector"), _orgA, rolLector);
        var rolB = await api.CrearRolAsync(_orgB, "Administrador B");
        await api.AsignarModuloAsync(_orgB, rolB, "sedes");
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, rolB);

        _ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad WHERE activo");
        _sedeA = await Sede(_orgA, "Sede A");
        _sedeA2 = await Sede(_orgA, "Sede A2");
        _sedeB = await Sede(_orgB, "Sede B");
        _areaA = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Gerencia', false, true) RETURNING id_area", _orgA, _sedeA);
        _membresiaAdmin = await api.ScalarAsync<int>("SELECT m.id_membresia FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario WHERE u.correo = @p0", Correo("admin"));
        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersonaA = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Visitante', true) RETURNING id_tipo_persona", _orgA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- Apoyo ----------

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));
    private Task<int> Sede(int org, string nombre) =>
        api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, @p2, 'UTC', true) RETURNING id_sede", org, _ciudad, nombre);
    private Task<int> ZonaEmpresarial(string nombre) =>
        api.ScalarAsync<int>("INSERT INTO zona_empresarial (nombre, activa, creada_en) VALUES (@p0, true, now()) RETURNING id_zona_empresarial", $"{nombre} {_sufijo}");

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(esperado == respuesta.StatusCode, $"Se esperaba {(int)esperado} y llegó {(int)respuesta.StatusCode}: {cuerpo}");
        return cuerpo.Length == 0 ? default : JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private static async Task<int> Punto(HttpClient cliente, int sede, string nombre, string modo = "Ambos", bool comun = false) =>
        (await Leer(await cliente.PostAsJsonAsync("/api/puntos-acceso", new { idSede = sede, nombre, modoAcceso = modo, esComunZonaEmpresarial = comun }), HttpStatusCode.Created))
            .GetProperty("idPuntoAcceso").GetInt32();

    private async Task<int> PersonaAsync(HttpClient cliente)
    {
        var documento = Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString();
        return (await Leer(await cliente.PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = documento, nombres = "Ana", apellidos = $"Visitante {_sufijo}", correo = "ana@ejemplo.test",
        }), HttpStatusCode.Created)).GetProperty("idPersona").GetInt32();
    }

    /// <summary>Autorización aprobada y visita vinculada lista para registrar la entrada.</summary>
    private async Task<(int Autorizacion, int Visita)> VisitaAutorizadaAsync(HttpClient admin)
    {
        var persona = await PersonaAsync(admin);
        var autorizacion = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-acceso", new
        {
            idPersona = persona, idSede = _sedeA, idArea = _areaA, idMembresiaResponsable = _membresiaAdmin, modalidad = "Diaria", fechaInicio = Hoy, motivo = "Reunión",
        }), HttpStatusCode.Created)).GetProperty("idAutorizacion").GetInt32();
        await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-acceso/{autorizacion}/aprobar", new { motivo = (string?)null }), HttpStatusCode.OK);
        var visita = (await Leer(await admin.PostAsJsonAsync("/api/visitas", new
        {
            idPersona = persona, idSede = _sedeA, idArea = _areaA, idMembresiaResponsable = _membresiaAdmin, motivo = "Reunión", idAutorizacion = autorizacion,
        }), HttpStatusCode.Created)).GetProperty("visita").GetProperty("idVisita").GetInt32();
        return (autorizacion, visita);
    }

    private static Task<HttpResponseMessage> Movimiento(HttpClient cliente, int visita, string tipo, int punto)
    {
        var mensaje = new HttpRequestMessage(HttpMethod.Post, $"/api/visitas/{visita}/{tipo}") { Content = JsonContent.Create(new { idPuntoAcceso = punto }) };
        mensaje.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return cliente.SendAsync(mensaje);
    }

    // ---------- Administración ----------

    [Fact]
    public async Task Crea_puntos_en_una_sede_sin_duplicados_y_con_trazabilidad()
    {
        var admin = await Cliente("admin");
        var punto = await Punto(admin, _sedeA, "Entrada de parqueadero", "Entrada");

        var detalle = await Leer(await admin.GetAsync($"/api/puntos-acceso/{punto}"), HttpStatusCode.OK);
        Assert.Equal(_sedeA, detalle.GetProperty("idSede").GetInt32());
        Assert.False(detalle.TryGetProperty("idZona", out _));
        Assert.Equal(Correo("admin"), detalle.GetProperty("creadoPor").GetString());

        // Duplicados en la misma sede (sin distinguir mayúsculas ni espacios); el mismo nombre en otra sede sí se admite.
        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idSede = _sedeA, nombre = "  ENTRADA DE PARQUEADERO ", modoAcceso = "Ambos" }), HttpStatusCode.Conflict);
        await Punto(admin, _sedeA2, "Entrada de parqueadero");
        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idSede = _sedeA, nombre = "Torniquete", modoAcceso = "Lateral" }), HttpStatusCode.BadRequest);

        // Editar registra quién y cuándo, y deja auditoría con valores anterior y nuevo; el punto no cambia de sede.
        var editado = await Leer(await admin.PutAsJsonAsync($"/api/puntos-acceso/{punto}", new { idSede = _sedeA, nombre = "Parqueadero visitantes", modoAcceso = "Ambos" }), HttpStatusCode.OK);
        Assert.Equal(Correo("admin"), editado.GetProperty("actualizadoPor").GetString());
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'PuntoAcceso' AND accion = 'punto_acceso.modificar' AND id_registro = @p0 AND valor_anterior LIKE '%parqueadero%'", punto.ToString()));
        await Leer(await admin.PutAsJsonAsync($"/api/puntos-acceso/{punto}", new { idSede = _sedeA2, nombre = "Parqueadero visitantes", modoAcceso = "Ambos" }), HttpStatusCode.BadRequest);

        // Filtros por sede y estado.
        var porSede = await Leer(await admin.GetAsync($"/api/puntos-acceso?idSede={_sedeA}&activo=true"), HttpStatusCode.OK);
        Assert.Equal(punto, Assert.Single(porSede.EnumerateArray()).GetProperty("idPuntoAcceso").GetInt32());
    }

    [Fact]
    public async Task No_se_asocia_un_punto_a_una_sede_de_otra_organizacion_y_otra_organizacion_no_ve_los_registros()
    {
        var admin = await Cliente("admin");
        var adminB = await Cliente("adminb");
        var punto = await Punto(admin, _sedeA, "Acceso bodega");

        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idSede = _sedeB, nombre = "Intruso", modoAcceso = "Ambos" }), HttpStatusCode.BadRequest);
        await Leer(await adminB.GetAsync($"/api/puntos-acceso/{punto}"), HttpStatusCode.NotFound);
        await Leer(await adminB.PostAsync($"/api/puntos-acceso/{punto}/inactivar", null), HttpStatusCode.NotFound);
        Assert.DoesNotContain((await Leer(await adminB.GetAsync("/api/puntos-acceso"), HttpStatusCode.OK)).EnumerateArray(), p => p.GetProperty("idPuntoAcceso").GetInt32() == punto);

        // La base también lo impide: la FK compuesta exige la misma organización de la sede.
        await Assert.ThrowsAnyAsync<Exception>(() => api.EjecutarAsync(
            "INSERT INTO punto_acceso (id_organizacion, id_sede, nombre, modo_acceso, es_comun_zona_empresarial, activo, creado_en) VALUES (@p0, @p1, 'Directo', 'Ambos', false, true, now())", _orgA, _sedeB));
    }

    [Fact]
    public async Task Solo_los_roles_con_el_modulo_sedes_administran_y_el_lector_solo_consulta()
    {
        var lector = await Cliente("lector");
        var admin = await Cliente("admin");
        var punto = await Punto(admin, _sedeA, "Oficinas");

        await Leer(await lector.GetAsync("/api/puntos-acceso"), HttpStatusCode.OK);
        await Leer(await lector.PostAsJsonAsync("/api/puntos-acceso", new { idSede = _sedeA, nombre = "Nuevo", modoAcceso = "Ambos" }), HttpStatusCode.Forbidden);
        await Leer(await lector.PostAsync($"/api/puntos-acceso/{punto}/inactivar", null), HttpStatusCode.Forbidden);
        await Leer(await lector.DeleteAsync($"/api/puntos-acceso/{punto}"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Las_zonas_de_acceso_ya_no_existen_en_la_api()
    {
        var admin = await Cliente("admin");
        await Leer(await admin.GetAsync("/api/zonas"), HttpStatusCode.NotFound);
        var opciones = await Leer(await admin.GetAsync("/api/autorizaciones-acceso/opciones"), HttpStatusCode.OK);
        Assert.False(opciones.TryGetProperty("zonas", out _));
        Assert.False(opciones.TryGetProperty("exigeZonas", out _));
    }

    [Fact]
    public async Task Solo_se_eliminan_puntos_sin_historial_y_una_sede_inactiva_no_admite_nuevos()
    {
        var admin = await Cliente("admin");
        var punto = await Punto(admin, _sedeA2, "Acceso deportivo");
        await Leer(await admin.DeleteAsync($"/api/puntos-acceso/{punto}"), HttpStatusCode.NoContent);

        var inactiva = await Sede(_orgA, "Sede archivo");
        await api.EjecutarAsync("UPDATE sede SET activo = false WHERE id_sede = @p0", inactiva);
        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idSede = inactiva, nombre = "Acceso archivo", modoAcceso = "Ambos" }), HttpStatusCode.BadRequest);
    }

    // ---------- Zona empresarial desde la sede ----------

    [Fact]
    public async Task El_punto_comun_exige_que_la_sede_pertenezca_a_una_zona_empresarial()
    {
        var admin = await Cliente("admin");
        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idSede = _sedeA2, nombre = "Portería del parque", modoAcceso = "Ambos", esComunZonaEmpresarial = true }), HttpStatusCode.BadRequest);

        // La sede toma la zona desde su formulario: basta con que la zona exista y esté activa.
        var zona = await ZonaEmpresarial("Parque industrial");
        var catalogo = await Leer(await admin.GetAsync("/api/sedes/zonas-empresariales"), HttpStatusCode.OK);
        Assert.Contains(catalogo.EnumerateArray(), z => z.GetProperty("id").GetInt32() == zona);
        var sede = await Leer(await admin.GetAsync($"/api/sedes/{_sedeA2}"), HttpStatusCode.OK);
        await Leer(await admin.PutAsJsonAsync($"/api/sedes/{_sedeA2}", new
        {
            idCiudad = sede.GetProperty("idCiudad").GetInt32(), nombre = sede.GetProperty("nombre").GetString(), idZonaEmpresarial = zona,
        }), HttpStatusCode.OK);

        var comun = await Punto(admin, _sedeA2, "Portería del parque", comun: true);
        var detalle = await Leer(await admin.GetAsync($"/api/puntos-acceso/{comun}"), HttpStatusCode.OK);
        Assert.True(detalle.GetProperty("esComunZonaEmpresarial").GetBoolean());
        Assert.Equal($"Parque industrial {_sufijo}", detalle.GetProperty("zonaEmpresarial").GetString());

        // Con elementos comunes la sede no puede cambiar ni quitar su zona (SedeController responde texto plano).
        var quitar = await admin.PutAsJsonAsync($"/api/sedes/{_sedeA2}", new
        {
            idCiudad = sede.GetProperty("idCiudad").GetInt32(), nombre = sede.GetProperty("nombre").GetString(), idZonaEmpresarial = (int?)null,
        });
        Assert.Equal(HttpStatusCode.Conflict, quitar.StatusCode);

        // Una zona inactiva no se asigna.
        var inactiva = await ZonaEmpresarial("Zona cerrada");
        await api.EjecutarAsync("UPDATE zona_empresarial SET activa = false WHERE id_zona_empresarial = @p0", inactiva);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/sedes/{_sedeA}", new { idCiudad = _ciudad, nombre = "Sede A", idZonaEmpresarial = inactiva })).StatusCode);
    }

    // ---------- Autorizaciones y movimientos ----------

    [Fact]
    public async Task El_movimiento_registra_sede_y_punto_y_la_autorizacion_vale_en_cualquier_punto_de_su_sede()
    {
        var admin = await Cliente("admin");
        var porteria = await Punto(admin, _sedeA, "Portería principal");
        await Punto(admin, _sedeA, "Acceso a planta");
        var puntoOtraSede = await Punto(admin, _sedeA2, "Portería A2");

        var (autorizacion, visita) = await VisitaAutorizadaAsync(admin);
        var detalleAutorizacion = await Leer(await admin.GetAsync($"/api/autorizaciones-acceso/{autorizacion}"), HttpStatusCode.OK);
        Assert.False(detalleAutorizacion.TryGetProperty("zonas", out _));

        await Leer(await Movimiento(admin, visita, "entrada", puntoOtraSede), HttpStatusCode.BadRequest);
        await Leer(await Movimiento(admin, visita, "entrada", porteria), HttpStatusCode.OK);
        Assert.Equal(porteria, await api.ScalarAsync<int>("SELECT id_punto_acceso FROM movimiento_acceso WHERE id_visita = @p0 AND tipo_movimiento = 'Entrada'", visita));
        Assert.Equal(_sedeA, await api.ScalarAsync<int>("SELECT id_sede FROM movimiento_acceso WHERE id_visita = @p0 AND tipo_movimiento = 'Entrada'", visita));

        // Inactivar el punto no altera el historial; ya no se usa en nuevos movimientos ni puede eliminarse.
        await Leer(await admin.PostAsync($"/api/puntos-acceso/{porteria}/inactivar", null), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, visita, "salida", porteria), HttpStatusCode.BadRequest);
        await Leer(await admin.DeleteAsync($"/api/puntos-acceso/{porteria}"), HttpStatusCode.Conflict);
        var detalle = await Leer(await admin.GetAsync($"/api/visitas/{visita}"), HttpStatusCode.OK);
        var movimiento = Assert.Single(detalle.GetProperty("movimientos").EnumerateArray());
        Assert.Equal("Portería principal", movimiento.GetProperty("puntoAcceso").GetString());
        Assert.Equal(JsonValueKind.Null, movimiento.GetProperty("zonaHistorica").ValueKind);
    }
}
