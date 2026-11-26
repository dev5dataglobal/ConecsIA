using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Responsables y autorizadores por destino (RF-04, HU-014, CA-03, CA-15): asignar, marcar autorizador, retirar sin borrar y
/// reactivar, todo auditado; efecto sobre el anfitrión de los registros de acceso (D-36); permisos por acción y alcance por sede;
/// aislamiento entre organizaciones; concurrencia.
/// </summary>
public sealed class AreaResponsablesTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _sedeA, _sedeA2, _areaA, _areaA2, _membresiaAdmin, _membresiaAna, _membresiaLuis, _membresiaB, _catAutomatica, _tipoPersona, _cc;
    private string _sufijo = "";

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A resp {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B resp {_sufijo}");
        var rolAdmin = await api.CrearRolAsync(_orgA, "Administrador");
        foreach (var m in new[] { "sedes", "personas", "control_accesos" }) await api.AsignarModuloAsync(_orgA, rolAdmin, m);
        await api.CrearUsuarioAsync(Correo("admin"), _orgA, rolAdmin);
        var rolLector = await api.CrearRolAsync(_orgA, "Lector");
        await api.AsignarModuloAsync(_orgA, rolLector, "sedes", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("lector"), _orgA, rolLector);
        await api.CrearUsuarioAsync(Correo("ana"), _orgA, rolLector);
        await api.CrearUsuarioAsync(Correo("luis"), _orgA, rolLector);
        await api.CrearUsuarioAsync(Correo("admin2"), _orgA, rolAdmin);
        var rolB = await api.CrearRolAsync(_orgB, "Administrador B");
        await api.AsignarModuloAsync(_orgB, rolB, "sedes");
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, rolB);

        var ciudad = await api.ScalarAsync<int>("SELECT min(id_ciudad) FROM ciudad");
        _sedeA = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede A', 'UTC', true) RETURNING id_sede", _orgA, ciudad);
        _sedeA2 = await api.ScalarAsync<int>("INSERT INTO sede (id_organizacion, id_ciudad, nombre, zona_horaria, activo) VALUES (@p0, @p1, 'Sede A2', 'UTC', true) RETURNING id_sede", _orgA, ciudad);
        _areaA = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Gerencia', false, true) RETURNING id_area", _orgA, _sedeA);
        _areaA2 = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Bodega', false, true) RETURNING id_area", _orgA, _sedeA2);
        // admin2 solo administra la sede A2.
        await api.EjecutarAsync("INSERT INTO membresia_sede (id_organizacion, id_membresia, id_sede) VALUES (@p0, @p1, @p2)", _orgA, await Membresia("admin2"), _sedeA2);
        _membresiaAdmin = await Membresia("admin");
        _membresiaAna = await Membresia("ana");
        _membresiaLuis = await Membresia("luis");
        _membresiaB = await Membresia("adminb");
        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _tipoPersona = await api.ScalarAsync<int>("INSERT INTO tipo_persona (id_organizacion, nombre, activo) VALUES (@p0, 'Visitante', true) RETURNING id_tipo_persona", _orgA);
        _catAutomatica = await api.ScalarAsync<int>("""
            INSERT INTO categoria_acceso (id_organizacion, codigo, nombre, flujo_aprobacion, modalidades_permitidas, exige_anfitrion, exige_motivo, permite_vehiculos, permite_dispositivos, orden, activo, creado_en)
            VALUES (@p0, 'VISITANTE', 'Visitante', 'Automatico', '{Unica}'::text[], true, false, false, false, 0, true, now()) RETURNING id_categoria_acceso
            """, _orgA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@responsables.test";
    private Task<HttpClient> Cliente(string nombre) => api.ClienteAutenticadoAsync(Correo(nombre));
    private Task<int> Membresia(string nombre) =>
        api.ScalarAsync<int>("SELECT m.id_membresia FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario WHERE u.correo = @p0", Correo(nombre));

    private static async Task<JsonElement> Leer(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(esperado == respuesta.StatusCode, $"Se esperaba {(int)esperado} y llegó {(int)respuesta.StatusCode}: {cuerpo}");
        return cuerpo.Length == 0 ? default : JsonDocument.Parse(cuerpo).RootElement.Clone();
    }

    private static Task<HttpResponseMessage> Asignar(HttpClient cliente, int area, int membresia, bool autorizador = false) =>
        cliente.PostAsJsonAsync($"/api/areas/{area}/responsables", new { idMembresia = membresia, esAutorizador = autorizador });

    private Task<long> Auditorias(string accion, int area, int membresia) =>
        api.ScalarAsync<long>("SELECT count(*) FROM auditoria WHERE entidad = 'AreaResponsable' AND id_registro = @p0 AND accion = @p1 AND id_organizacion = @p2",
            $"{area}:{membresia}", accion, _orgA);

    private async Task<HttpResponseMessage> CrearRegistro(HttpClient cliente, int anfitrion)
    {
        var persona = await Leer(await cliente.PostAsJsonAsync("/api/personas", new
        {
            idTipoPersona = _tipoPersona, idTipoDocumento = _cc, numeroDocumento = Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString(),
            nombres = "Eva", apellidos = $"Visitante {_sufijo}", correo = "eva@responsables.test",
        }), HttpStatusCode.Created);
        return await cliente.PostAsJsonAsync("/api/accesos", new
        {
            idPersona = persona.GetProperty("idPersona").GetInt32(), idCategoriaAcceso = _catAutomatica, idSede = _sedeA, idArea = _areaA,
            idMembresiaResponsable = anfitrion, modalidad = "Unica", fechaInicio = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
        });
    }

    [Fact]
    public async Task Asigna_marca_autorizador_retira_sin_borrar_y_reactiva_con_auditoria()
    {
        var admin = await Cliente("admin");
        var asignado = await Leer(await Asignar(admin, _areaA, _membresiaAna, autorizador: true), HttpStatusCode.OK);
        Assert.True(asignado.GetProperty("esAutorizador").GetBoolean());
        Assert.Equal(Correo("ana"), asignado.GetProperty("usuario").GetString());
        Assert.Equal(Correo("admin"), asignado.GetProperty("asignadoPor").GetString());
        await Leer(await Asignar(admin, _areaA, _membresiaAna), HttpStatusCode.Conflict);

        var cambiado = await Leer(await admin.PutAsJsonAsync($"/api/areas/{_areaA}/responsables/{_membresiaAna}", new { esAutorizador = false }), HttpStatusCode.OK);
        Assert.False(cambiado.GetProperty("esAutorizador").GetBoolean());
        var retirado = await Leer(await admin.PostAsync($"/api/areas/{_areaA}/responsables/{_membresiaAna}/retirar", null), HttpStatusCode.OK);
        Assert.False(retirado.GetProperty("activo").GetBoolean());
        await Leer(await admin.PostAsync($"/api/areas/{_areaA}/responsables/{_membresiaAna}/retirar", null), HttpStatusCode.Conflict);

        // El retirado sigue en la lista (trazabilidad) y puede reactivarse.
        var lista = await Leer(await admin.GetAsync($"/api/areas/{_areaA}/responsables"), HttpStatusCode.OK);
        Assert.False(lista.EnumerateArray().Single(x => x.GetProperty("idMembresia").GetInt32() == _membresiaAna).GetProperty("activo").GetBoolean());
        var reactivado = await Leer(await Asignar(admin, _areaA, _membresiaAna, autorizador: true), HttpStatusCode.OK);
        Assert.True(reactivado.GetProperty("activo").GetBoolean());

        foreach (var accion in new[] { "AsignarResponsable", "CambiarResponsable", "RetirarResponsable", "ReactivarResponsable" })
            Assert.Equal(1, await Auditorias(accion, _areaA, _membresiaAna));
    }

    [Fact]
    public async Task Con_designados_solo_ellos_son_anfitriones_y_retirar_no_cambia_los_registros()
    {
        var admin = await Cliente("admin");
        // Sin designados: cualquier usuario activo puede ser anfitrión.
        await Leer(await CrearRegistro(admin, _membresiaAdmin), HttpStatusCode.Created);

        await Leer(await Asignar(admin, _areaA, _membresiaLuis), HttpStatusCode.OK);
        var opciones = await Leer(await admin.GetAsync("/api/accesos/opciones"), HttpStatusCode.OK);
        var area = opciones.GetProperty("areas").EnumerateArray().Single(x => x.GetProperty("id").GetInt32() == _areaA);
        Assert.Equal([_membresiaLuis], area.GetProperty("responsables").EnumerateArray().Select(x => x.GetInt32()));

        var noDesignado = await Leer(await CrearRegistro(admin, _membresiaAdmin), HttpStatusCode.BadRequest);
        Assert.Contains("no está designado", noDesignado.GetProperty("detail").GetString());
        var creado = await Leer(await CrearRegistro(admin, _membresiaLuis), HttpStatusCode.Created);

        await Leer(await admin.PostAsync($"/api/areas/{_areaA}/responsables/{_membresiaLuis}/retirar", null), HttpStatusCode.OK);
        var detalle = await Leer(await admin.GetAsync($"/api/accesos/{creado.GetProperty("idAutorizacion").GetInt32()}"), HttpStatusCode.OK);
        Assert.Equal(_membresiaLuis, detalle.GetProperty("idMembresiaResponsable").GetInt32());
        // Sin designados activos vuelve a admitirse cualquier usuario activo.
        await Leer(await CrearRegistro(admin, _membresiaAdmin), HttpStatusCode.Created);
    }

    [Fact]
    public async Task Consultar_no_basta_para_asignar_y_el_alcance_por_sede_se_respeta()
    {
        var lector = await Cliente("lector");
        await Leer(await lector.GetAsync($"/api/areas/{_areaA}/responsables"), HttpStatusCode.OK);
        await Leer(await Asignar(lector, _areaA, _membresiaAna), HttpStatusCode.Forbidden);

        var admin2 = await Cliente("admin2");
        await Leer(await Asignar(admin2, _areaA, _membresiaAna), HttpStatusCode.NotFound);
        await Leer(await Asignar(admin2, _areaA2, _membresiaAna), HttpStatusCode.OK);
    }

    [Fact]
    public async Task Otra_organizacion_no_asigna_ni_es_asignada_y_un_destino_inactivo_no_admite_designaciones()
    {
        var adminB = await Cliente("adminb");
        await Leer(await Asignar(adminB, _areaA, _membresiaB), HttpStatusCode.NotFound);
        await Leer(await adminB.GetAsync($"/api/areas/{_areaA}/responsables"), HttpStatusCode.NotFound);

        var admin = await Cliente("admin");
        await Leer(await Asignar(admin, _areaA2, _membresiaB), HttpStatusCode.BadRequest);
        var miembros = await Leer(await admin.GetAsync("/api/areas/miembros"), HttpStatusCode.OK);
        Assert.DoesNotContain(miembros.EnumerateArray(), x => x.GetProperty("idMembresia").GetInt32() == _membresiaB);

        var inactiva = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, 'Archivo', false, false) RETURNING id_area", _orgA, _sedeA);
        await Leer(await Asignar(admin, inactiva, _membresiaAna), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Dos_asignaciones_simultaneas_dejan_una_sola_designacion()
    {
        var area = await api.ScalarAsync<int>("INSERT INTO area (id_organizacion, id_sede, nombre, es_comun_zona_empresarial, activo) VALUES (@p0, @p1, @p2, false, true) RETURNING id_area", _orgA, _sedeA, $"Concurrente {_sufijo}");
        var clientes = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Cliente("admin")));
        var respuestas = await Task.WhenAll(clientes.Select(c => Asignar(c, area, _membresiaAna)));
        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(1L, await api.ScalarAsync<long>("SELECT count(*) FROM area_responsable WHERE id_area = @p0", area));
    }
}
