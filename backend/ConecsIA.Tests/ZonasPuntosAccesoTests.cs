using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Zonas y puntos de acceso (RF-05, ADR-027): Sede → Zona → Punto de acceso, unicidad por sede, aislamiento por organización,
/// permisos del módulo «sedes», eliminación solo sin historial, restricción de autorizaciones por zona (opcional u obligatoria
/// según la política) y validación del punto del movimiento contra las zonas permitidas, conservando el historial.
/// </summary>
public sealed class ZonasPuntosAccesoTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _sedeA, _sedeA2, _sedeB, _areaA, _membresiaAdmin, _tipoPersonaA, _cc;
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

        var ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad");
        _sedeA = await Sede(_orgA, "Sede A", ciudad);
        _sedeA2 = await Sede(_orgA, "Sede A2", ciudad);
        _sedeB = await Sede(_orgB, "Sede B", ciudad);
        _areaA = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Gerencia', false, true) RETURNING id_area", _orgA, _sedeA);
        _membresiaAdmin = await api.ScalarAsync<int>("SELECT m.id_membresia FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario WHERE u.correo = @p0", Correo("admin"));
        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersonaA = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Visitante', true) RETURNING id_tipo_persona", _orgA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- Apoyo ----------

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));
    private Task<int> Sede(int org, string nombre, int ciudad) =>
        api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, @p2, 'UTC', true) RETURNING id_sede", org, ciudad, nombre);

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(esperado == respuesta.StatusCode, $"Se esperaba {(int)esperado} y llegó {(int)respuesta.StatusCode}: {cuerpo}");
        return cuerpo.Length == 0 ? default : JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private static async Task<int> Zona(HttpClient cliente, int sede, string nombre) =>
        (await Leer(await cliente.PostAsJsonAsync("/api/zonas", new { idSede = sede, nombre }), HttpStatusCode.Created)).GetProperty("idZona").GetInt32();

    private static async Task<int> Punto(HttpClient cliente, int zona, string nombre, string modo = "Ambos") =>
        (await Leer(await cliente.PostAsJsonAsync("/api/puntos-acceso", new { idZona = zona, nombre, modoAcceso = modo }), HttpStatusCode.Created)).GetProperty("idPuntoAcceso").GetInt32();

    private async Task<int> PersonaAsync(HttpClient cliente)
    {
        var documento = Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString();
        return (await Leer(await cliente.PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = _tipoPersonaA, idTipoDocumento = _cc, numeroDocumento = documento, nombres = "Ana", apellidos = $"Visitante {_sufijo}", correo = "ana@ejemplo.test",
        }), HttpStatusCode.Created)).GetProperty("idPersona").GetInt32();
    }

    private object Solicitud(int persona, params int[] zonas) => new
    {
        idPersona = persona, idSede = _sedeA, idArea = _areaA, idMembresiaResponsable = _membresiaAdmin, modalidad = "Diaria", fechaInicio = Hoy,
        motivo = "Reunión", zonas,
    };

    /// <summary>Autorización aprobada (con zonas) y visita vinculada lista para registrar la entrada.</summary>
    private async Task<(int Autorizacion, int Visita)> VisitaAutorizadaAsync(HttpClient admin, params int[] zonas)
    {
        var persona = await PersonaAsync(admin);
        var autorizacion = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-acceso", Solicitud(persona, zonas)), HttpStatusCode.Created))
            .GetProperty("idAutorizacion").GetInt32();
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
    public async Task Crea_zonas_en_una_sede_y_puntos_en_una_zona_sin_duplicados_y_con_trazabilidad()
    {
        var admin = await Cliente("admin");
        var zona = await Zona(admin, _sedeA, "Parqueadero");
        var punto = await Punto(admin, zona, "Entrada de parqueadero", "Entrada");

        var detalle = await Leer(await admin.GetAsync($"/api/puntos-acceso/{punto}"), HttpStatusCode.OK);
        Assert.Equal(zona, detalle.GetProperty("idZona").GetInt32());
        Assert.Equal(_sedeA, detalle.GetProperty("idSede").GetInt32());
        Assert.Equal(Correo("admin"), detalle.GetProperty("creadoPor").GetString());

        // Duplicados en el mismo ámbito (sin distinguir mayúsculas ni espacios); el mismo nombre en otra sede sí se admite.
        await Leer(await admin.PostAsJsonAsync("/api/zonas", new { idSede = _sedeA, nombre = "  PARQUEADERO " }), HttpStatusCode.Conflict);
        await Zona(admin, _sedeA2, "Parqueadero");
        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idZona = zona, nombre = "entrada de parqueadero", modoAcceso = "Ambos" }), HttpStatusCode.Conflict);
        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idZona = zona, nombre = "Torniquete", modoAcceso = "Lateral" }), HttpStatusCode.BadRequest);

        // Editar registra quién y cuándo, y deja auditoría con valores anterior y nuevo.
        var editado = await Leer(await admin.PutAsJsonAsync($"/api/zonas/{zona}", new { idSede = _sedeA, nombre = "Parqueadero visitantes", descripcion = "Sótano" }), HttpStatusCode.OK);
        Assert.Equal(Correo("admin"), editado.GetProperty("actualizadoPor").GetString());
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'Zona' AND accion = 'zona.modificar' AND id_registro = @p0 AND valor_anterior LIKE '%Parqueadero%'", zona.ToString()));

        // Filtros por sede, zona y estado.
        var porZona = await Leer(await admin.GetAsync($"/api/puntos-acceso?idZona={zona}&activo=true"), HttpStatusCode.OK);
        Assert.Equal(punto, Assert.Single(porZona.EnumerateArray()).GetProperty("idPuntoAcceso").GetInt32());
        var porSede = await Leer(await admin.GetAsync($"/api/zonas?idSede={_sedeA2}"), HttpStatusCode.OK);
        Assert.All(porSede.EnumerateArray(), z => Assert.Equal(_sedeA2, z.GetProperty("idSede").GetInt32()));
    }

    [Fact]
    public async Task No_se_asocia_un_punto_a_una_zona_de_otra_sede_u_organizacion_y_otra_organizacion_no_ve_los_registros()
    {
        var admin = await Cliente("admin");
        var adminB = await Cliente("adminb");
        var zonaA = await Zona(admin, _sedeA, "Bodega");
        var zonaA2 = await Zona(admin, _sedeA2, "Planta");
        var zonaB = await Zona(adminB, _sedeB, "Oficinas");
        var punto = await Punto(admin, zonaA, "Acceso bodega");

        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idZona = zonaB, nombre = "Intruso", modoAcceso = "Ambos" }), HttpStatusCode.BadRequest);
        await Leer(await admin.PostAsJsonAsync("/api/zonas", new { idSede = _sedeB, nombre = "Intrusa" }), HttpStatusCode.BadRequest);
        await Leer(await admin.PutAsJsonAsync($"/api/puntos-acceso/{punto}", new { idZona = zonaA2, nombre = "Acceso bodega", modoAcceso = "Ambos" }), HttpStatusCode.BadRequest);

        await Leer(await adminB.GetAsync($"/api/zonas/{zonaA}"), HttpStatusCode.NotFound);
        await Leer(await adminB.GetAsync($"/api/puntos-acceso/{punto}"), HttpStatusCode.NotFound);
        await Leer(await adminB.PostAsync($"/api/puntos-acceso/{punto}/inactivar", null), HttpStatusCode.NotFound);
        Assert.DoesNotContain((await Leer(await adminB.GetAsync("/api/zonas"), HttpStatusCode.OK)).EnumerateArray(), z => z.GetProperty("idZona").GetInt32() == zonaA);

        // La base también lo impide: la FK compuesta exige la misma organización y sede.
        await Assert.ThrowsAnyAsync<Exception>(() => api.EjecutarAsync(
            "INSERT INTO punto_acceso (id_organizacion, id_sede, id_zona, nombre, modo_acceso, activo, creado_en) VALUES (@p0, @p1, @p2, 'Directo', 'Ambos', true, now())", _orgA, _sedeA2, zonaA));
    }

    [Fact]
    public async Task Solo_los_roles_con_el_modulo_sedes_administran_y_el_lector_solo_consulta()
    {
        var lector = await Cliente("lector");
        var admin = await Cliente("admin");
        var zona = await Zona(admin, _sedeA, "Oficinas");

        await Leer(await lector.GetAsync("/api/zonas"), HttpStatusCode.OK);
        await Leer(await lector.PostAsJsonAsync("/api/zonas", new { idSede = _sedeA, nombre = "Nueva" }), HttpStatusCode.Forbidden);
        await Leer(await lector.PostAsync($"/api/zonas/{zona}/inactivar", null), HttpStatusCode.Forbidden);
        await Leer(await lector.DeleteAsync($"/api/zonas/{zona}"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Solo_se_eliminan_registros_sin_historial_y_los_inactivos_no_admiten_nuevas_configuraciones()
    {
        var admin = await Cliente("admin");
        var zona = await Zona(admin, _sedeA, "Zona deportiva");
        var punto = await Punto(admin, zona, "Acceso deportivo");

        await Leer(await admin.DeleteAsync($"/api/zonas/{zona}"), HttpStatusCode.Conflict);
        await Leer(await admin.DeleteAsync($"/api/puntos-acceso/{punto}"), HttpStatusCode.NoContent);
        await Leer(await admin.DeleteAsync($"/api/zonas/{zona}"), HttpStatusCode.NoContent);

        var inactiva = await Zona(admin, _sedeA, "Archivo");
        await Leer(await admin.PostAsync($"/api/zonas/{inactiva}/inactivar", null), HttpStatusCode.OK);
        await Leer(await admin.PostAsJsonAsync("/api/puntos-acceso", new { idZona = inactiva, nombre = "Acceso archivo", modoAcceso = "Ambos" }), HttpStatusCode.BadRequest);
        var persona = await PersonaAsync(admin);
        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-acceso", Solicitud(persona, inactiva)), HttpStatusCode.BadRequest);
        var opciones = await Leer(await admin.GetAsync("/api/autorizaciones-acceso/opciones"), HttpStatusCode.OK);
        Assert.DoesNotContain(opciones.GetProperty("zonas").EnumerateArray(), z => z.GetProperty("id").GetInt32() == inactiva);
    }

    // ---------- Autorizaciones y movimientos ----------

    [Fact]
    public async Task El_movimiento_registra_el_punto_y_solo_se_admite_en_una_zona_permitida()
    {
        var admin = await Cliente("admin");
        var recepcion = await Zona(admin, _sedeA, "Recepción");
        var planta = await Zona(admin, _sedeA, "Planta");
        var porteria = await Punto(admin, recepcion, "Portería principal");
        var accesoPlanta = await Punto(admin, planta, "Acceso a planta");
        var zonaOtraSede = await Zona(admin, _sedeA2, "Bodega A2");
        var puntoOtraSede = await Punto(admin, zonaOtraSede, "Portería A2");

        // Zonas de otra sede no se admiten en la autorización.
        var persona = await PersonaAsync(admin);
        await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-acceso", Solicitud(persona, zonaOtraSede)), HttpStatusCode.BadRequest);

        var (autorizacion, visita) = await VisitaAutorizadaAsync(admin, recepcion);
        var detalleAutorizacion = await Leer(await admin.GetAsync($"/api/autorizaciones-acceso/{autorizacion}"), HttpStatusCode.OK);
        Assert.Equal(recepcion, Assert.Single(detalleAutorizacion.GetProperty("zonas").EnumerateArray()).GetProperty("id").GetInt32());

        var fuera = await Leer(await Movimiento(admin, visita, "entrada", accesoPlanta), HttpStatusCode.Conflict);
        Assert.Contains("zonas autorizadas", fuera.GetProperty("detail").GetString());
        await Leer(await Movimiento(admin, visita, "entrada", puntoOtraSede), HttpStatusCode.BadRequest);
        await Leer(await Movimiento(admin, visita, "entrada", porteria), HttpStatusCode.OK);
        Assert.Equal(recepcion, await api.ScalarAsync<int>("SELECT id_zona FROM movimiento_acceso WHERE id_visita = @p0 AND tipo_movimiento = 'Entrada'", visita));
        Assert.Equal(porteria, await api.ScalarAsync<int>("SELECT id_punto_acceso FROM movimiento_acceso WHERE id_visita = @p0 AND tipo_movimiento = 'Entrada'", visita));

        // Desactivar y mover el punto no altera el historial; ya no se usa en nuevos movimientos ni puede eliminarse.
        await Leer(await admin.PutAsJsonAsync($"/api/puntos-acceso/{porteria}", new { idZona = planta, nombre = "Portería principal", modoAcceso = "Ambos" }), HttpStatusCode.OK);
        await Leer(await admin.PostAsync($"/api/puntos-acceso/{porteria}/inactivar", null), HttpStatusCode.OK);
        await Leer(await Movimiento(admin, visita, "salida", porteria), HttpStatusCode.BadRequest);
        await Leer(await admin.DeleteAsync($"/api/puntos-acceso/{porteria}"), HttpStatusCode.Conflict);
        await Leer(await admin.PostAsync($"/api/zonas/{recepcion}/inactivar", null), HttpStatusCode.OK);
        var detalle = await Leer(await admin.GetAsync($"/api/visitas/{visita}"), HttpStatusCode.OK);
        var movimiento = Assert.Single(detalle.GetProperty("movimientos").EnumerateArray());
        Assert.Equal("Portería principal", movimiento.GetProperty("puntoAcceso").GetString());
        Assert.Equal("Recepción", movimiento.GetProperty("zona").GetString());
        Assert.Equal(recepcion, await api.ScalarAsync<int>("SELECT id_zona FROM movimiento_acceso WHERE id_visita = @p0", visita));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM autorizacion_zona WHERE id_autorizacion = @p0 AND id_zona = @p1", autorizacion, recepcion));
    }

    [Fact]
    public async Task Sin_zonas_la_autorizacion_no_restringe_y_la_politica_puede_exigirlas()
    {
        var admin = await Cliente("admin");
        var zona = await Zona(admin, _sedeA, "Laboratorio");
        var otra = await Zona(admin, _sedeA, "Cafetería");
        var punto = await Punto(admin, otra, "Acceso cafetería");

        var (_, visita) = await VisitaAutorizadaAsync(admin);
        await Leer(await Movimiento(admin, visita, "entrada", punto), HttpStatusCode.OK);

        // Solicitud pendiente: sus zonas se pueden editar; decidida, ya no.
        var persona = await PersonaAsync(admin);
        var pendiente = (await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-acceso", Solicitud(persona)), HttpStatusCode.Created)).GetProperty("idAutorizacion").GetInt32();
        var editada = await Leer(await admin.PutAsJsonAsync($"/api/autorizaciones-acceso/{pendiente}/zonas", new { zonas = new[] { zona, otra } }), HttpStatusCode.OK);
        Assert.Equal(2, editada.GetProperty("zonas").GetArrayLength());
        await Leer(await admin.PostAsJsonAsync($"/api/autorizaciones-acceso/{pendiente}/rechazar", new { motivo = "No aplica" }), HttpStatusCode.OK);
        await Leer(await admin.PutAsJsonAsync($"/api/autorizaciones-acceso/{pendiente}/zonas", new { zonas = new[] { zona } }), HttpStatusCode.Conflict);

        await api.EjecutarAsync("INSERT INTO parametro_organizacion (id_organizacion, clave, valor, actualizado_en) VALUES (@p0, 'autorizaciones.exige_zonas', 'true', now())", _orgA);
        try
        {
            Assert.True((await Leer(await admin.GetAsync("/api/autorizaciones-acceso/opciones"), HttpStatusCode.OK)).GetProperty("exigeZonas").GetBoolean());
            var otraPersona = await PersonaAsync(admin);
            await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-acceso", Solicitud(otraPersona)), HttpStatusCode.BadRequest);
            await Leer(await admin.PostAsJsonAsync("/api/autorizaciones-acceso", Solicitud(otraPersona, zona)), HttpStatusCode.Created);
        }
        finally
        {
            await api.EjecutarAsync("DELETE FROM parametro_organizacion WHERE id_organizacion = @p0 AND clave = 'autorizaciones.exige_zonas'", _orgA);
        }
    }
}
