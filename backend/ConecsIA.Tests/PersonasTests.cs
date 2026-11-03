using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Módulo de Personas: organización tomada del contexto, unicidad documental por organización, catálogos,
/// permisos por acción, foto privada, teléfonos (1:N) y vehículos (N:M mediante persona_vehiculo).
/// Escenario: organizaciones A y B con un administrador cada una (personas + vehículos) y perfiles limitados en A.
/// </summary>
public sealed class PersonasTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private int _orgA, _orgB, _tipoA, _tipoB, _cc, _pasaporte;
    private string _sufijo = "";

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");
        var rolA = await api.CrearRolAsync(_orgA, "Admin personas A");
        var rolB = await api.CrearRolAsync(_orgB, "Admin personas B");
        foreach (var (org, rol) in new[] { (_orgA, rolA), (_orgB, rolB) })
        {
            await api.AsignarModuloAsync(org, rol, "personas");
            await api.AsignarModuloAsync(org, rol, "vehiculos");
        }
        await api.CrearUsuarioAsync(Correo("admina"), _orgA, rolA);
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, rolB);
        var rolConsulta = await api.CrearRolAsync(_orgA, "Consulta personas");
        await api.AsignarModuloAsync(_orgA, rolConsulta, "personas", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("consulta"), _orgA, rolConsulta);
        var rolSinVehiculos = await api.CrearRolAsync(_orgA, "Personas sin vehículos");
        await api.AsignarModuloAsync(_orgA, rolSinVehiculos, "personas");
        await api.CrearUsuarioAsync(Correo("sinvehiculos"), _orgA, rolSinVehiculos);
        await api.CrearUsuarioAsync(Correo("sinmodulo"), _orgA);

        _cc = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'CC'");
        _pasaporte = await api.ScalarAsync<int>("SELECT id_tipo_documento FROM tipo_documento WHERE codigo = 'PA'");
        _tipoA = await CrearTipoAsync(await Admin("a"), "Visitante");
        _tipoB = await CrearTipoAsync(await Admin("b"), "Visitante");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";
    private Task<HttpClient> Admin(string org) => api.ClienteAutenticadoAsync(Correo($"admin{org}"));
    private static string Documento() => Random.Shared.NextInt64(10_000_000, 9_999_999_999).ToString();

    private static async Task<int> CrearTipoAsync(HttpClient cliente, string nombre)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/personas/tipos-persona", new { nombre, descripcion = "Prueba" });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idTipoPersona").GetInt32();
    }

    private object NuevaPersona(string documento, int? tipo = null, int? tipoDocumento = null, string correo = "ana@ejemplo.test", object? idOrganizacion = null) => new
    {
        idOrganizacion,
        idTipoPersona = tipo ?? _tipoA,
        idTipoDocumento = tipoDocumento ?? _cc,
        numeroDocumento = documento,
        nombres = "Ana María",
        apellidos = "Gómez Ruiz",
        correo,
        observacion = "Registro de prueba",
    };

    private static async Task<JsonElement> CrearAsync(HttpClient cliente, object persona)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/personas", persona);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    // ---------- Registro y organización ----------

    [Fact]
    public async Task Crear_persona_asigna_la_organizacion_del_contexto_e_ignora_la_del_cliente()
    {
        var admin = await Admin("a");
        var creada = await CrearAsync(admin, NuevaPersona(Documento(), idOrganizacion: _orgB));
        var id = creada.GetProperty("idPersona").GetInt32();

        Assert.Equal(_orgA, await api.ScalarAsync<int>("SELECT id_organizacion FROM persona WHERE id_persona = @p0", id));
        Assert.Equal(_orgA, creada.GetProperty("idOrganizacion").GetInt32());
        Assert.True(creada.GetProperty("activo").GetBoolean());
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'Persona' AND accion = 'Crear' AND id_registro = @p0 AND id_organizacion = @p1", id.ToString(), _orgA));
    }

    [Fact]
    public async Task El_documento_no_se_duplica_en_la_organizacion_pero_si_entre_organizaciones_o_tipos()
    {
        var adminA = await Admin("a");
        var documento = Documento();
        await CrearAsync(adminA, NuevaPersona(documento));

        // Mismo tipo y número con espacios externos: duplicado.
        var duplicado = await adminA.PostAsJsonAsync("/api/personas", NuevaPersona($"  {documento} "));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        // Mismo número con otro tipo de documento: es otra identificación.
        await CrearAsync(adminA, NuevaPersona(documento, tipoDocumento: _pasaporte));

        // Mismo documento en otra organización: registro independiente.
        await CrearAsync(await Admin("b"), NuevaPersona(documento, tipo: _tipoB));

        var encontrada = await adminA.GetFromJsonAsync<JsonElement>($"/api/personas/buscar-documento?idTipoDocumento={_cc}&numero={documento}");
        Assert.Equal(documento, encontrada.GetProperty("numeroDocumento").GetString());
        Assert.Equal(_orgA, encontrada.GetProperty("idOrganizacion").GetInt32());
    }

    [Fact]
    public async Task Altas_concurrentes_del_mismo_documento_crean_una_sola_persona()
    {
        var admin = await Admin("a");
        var documento = Documento();
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => admin.PostAsJsonAsync("/api/personas", NuevaPersona(documento))));

        Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.Created);
        Assert.All(respuestas.Where(x => x.StatusCode != HttpStatusCode.Created), x => Assert.Equal(HttpStatusCode.Conflict, x.StatusCode));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM persona WHERE id_organizacion = @p0 AND numero_documento_normalizado = @p1", _orgA, documento));
    }

    [Theory]
    [InlineData("12AB34", "ana@ejemplo.test")]   // cédula con letras
    [InlineData("12", "ana@ejemplo.test")]       // cédula demasiado corta
    [InlineData("1234567", "correo-invalido")]   // correo con formato no válido
    [InlineData("1234567", "")]                  // correo obligatorio
    public async Task Rechaza_documento_o_correo_con_formato_invalido(string documento, string correo)
    {
        var respuesta = await (await Admin("a")).PostAsJsonAsync("/api/personas", NuevaPersona(documento, correo: correo));
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Rechaza_tipos_de_persona_de_otra_organizacion_o_inactivos()
    {
        var admin = await Admin("a");
        var ajeno = await admin.PostAsJsonAsync("/api/personas", NuevaPersona(Documento(), tipo: _tipoB));
        Assert.Equal(HttpStatusCode.BadRequest, ajeno.StatusCode);

        var tipo = await CrearTipoAsync(admin, $"Temporal {_sufijo}");
        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"/api/personas/tipos-persona/{tipo}/estado", new { activo = false })).StatusCode);
        var inactivo = await admin.PostAsJsonAsync("/api/personas", NuevaPersona(Documento(), tipo: tipo));
        Assert.Equal(HttpStatusCode.BadRequest, inactivo.StatusCode);
    }

    [Fact]
    public async Task Una_organizacion_no_ve_ni_modifica_personas_de_otra()
    {
        var persona = await CrearAsync(await Admin("a"), NuevaPersona(Documento()));
        var id = persona.GetProperty("idPersona").GetInt32();
        var adminB = await Admin("b");

        Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/personas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.PutAsJsonAsync($"/api/personas/{id}", NuevaPersona(Documento(), tipo: _tipoB))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.PatchAsJsonAsync($"/api/personas/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.GetAsync($"/api/personas/{id}/foto")).StatusCode);
        var lista = await adminB.GetFromJsonAsync<JsonElement>("/api/personas");
        Assert.DoesNotContain(lista.EnumerateArray(), x => x.GetProperty("idPersona").GetInt32() == id);
        Assert.True(await api.ScalarAsync<bool>("SELECT activo FROM persona WHERE id_persona = @p0", id));
    }

    [Fact]
    public async Task Editar_actualiza_datos_audita_y_valida_duplicado_documental()
    {
        var admin = await Admin("a");
        var otro = Documento();
        await CrearAsync(admin, NuevaPersona(otro));
        var id = (await CrearAsync(admin, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();

        var conflicto = await admin.PutAsJsonAsync($"/api/personas/{id}", NuevaPersona(otro));
        Assert.Equal(HttpStatusCode.Conflict, conflicto.StatusCode);

        var nuevo = Documento();
        var editar = await admin.PutAsJsonAsync($"/api/personas/{id}", NuevaPersona(nuevo, correo: "nuevo@ejemplo.test"));
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        var editada = await editar.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("nuevo@ejemplo.test", editada.GetProperty("correo").GetString());
        Assert.Equal(nuevo, editada.GetProperty("numeroDocumento").GetString());
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'Persona' AND accion = 'Modificar' AND id_registro = @p0 AND valor_anterior IS NOT NULL", id.ToString()));
    }

    [Fact]
    public async Task Inactivar_conserva_la_persona_y_no_existe_borrado_fisico()
    {
        var admin = await Admin("a");
        var id = (await CrearAsync(admin, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();

        var inactivar = await admin.PatchAsJsonAsync($"/api/personas/{id}/estado", new { activo = false });
        Assert.Equal(HttpStatusCode.OK, inactivar.StatusCode);
        Assert.False((await inactivar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("activo").GetBoolean());

        var borrar = await admin.DeleteAsync($"/api/personas/{id}");
        Assert.True(borrar.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound);
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM persona WHERE id_persona = @p0", id));

        var inactivas = await admin.GetFromJsonAsync<JsonElement>("/api/personas?estado=Inactivo");
        Assert.Contains(inactivas.EnumerateArray(), x => x.GetProperty("idPersona").GetInt32() == id);
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'Persona' AND accion = 'Inactivar' AND id_registro = @p0", id.ToString()));
    }

    // ---------- Permisos ----------

    [Fact]
    public async Task Las_acciones_respetan_los_permisos_del_modulo()
    {
        var id = (await CrearAsync(await Admin("a"), NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();
        var consulta = await api.ClienteAutenticadoAsync(Correo("consulta"));
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync("/api/personas")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync($"/api/personas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PostAsJsonAsync("/api/personas", NuevaPersona(Documento()))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PutAsJsonAsync($"/api/personas/{id}", NuevaPersona(Documento()))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PatchAsJsonAsync($"/api/personas/{id}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "3001234567", tipo = "Móvil" })).StatusCode);

        var sinModulo = await api.ClienteAutenticadoAsync(Correo("sinmodulo"));
        Assert.Equal(HttpStatusCode.Forbidden, (await sinModulo.GetAsync("/api/personas")).StatusCode);

        // Los vehículos son un módulo opcional: además de Personas exigen el módulo Vehículos.
        var sinVehiculos = await api.ClienteAutenticadoAsync(Correo("sinvehiculos"));
        Assert.Equal(HttpStatusCode.Forbidden, (await sinVehiculos.PostAsJsonAsync($"/api/personas/{id}/vehiculos", new { placa = "ABC123", tipo = "Automóvil" })).StatusCode);
    }

    [Fact]
    public async Task Los_tipos_de_documento_son_globales_y_solo_los_administra_la_plataforma()
    {
        var admin = await Admin("a");
        var catalogos = await admin.GetFromJsonAsync<JsonElement>("/api/personas/catalogos");
        var codigos = catalogos.GetProperty("tiposDocumento").EnumerateArray().Select(x => x.GetProperty("codigo").GetString()).ToList();
        Assert.Superset(new HashSet<string?> { "CC", "TI", "CE", "PA", "PPT", "NIT" }, codigos.ToHashSet());
        Assert.Contains(catalogos.GetProperty("tiposPersona").EnumerateArray(), x => x.GetProperty("id").GetInt32() == _tipoA);
        Assert.DoesNotContain(catalogos.GetProperty("tiposPersona").EnumerateArray(), x => x.GetProperty("id").GetInt32() == _tipoB);

        var crear = await admin.PostAsJsonAsync("/api/personas/tipos-documento", new { codigo = $"X{_sufijo}", nombre = "Otro documento" });
        Assert.Equal(HttpStatusCode.Forbidden, crear.StatusCode);
    }

    [Fact]
    public async Task Los_tipos_de_persona_no_se_duplican_en_la_organizacion()
    {
        var admin = await Admin("a");
        var nombre = $"Contratista {_sufijo}";
        await CrearTipoAsync(admin, nombre);
        var duplicado = await admin.PostAsJsonAsync("/api/personas/tipos-persona", new { nombre = nombre.ToUpperInvariant() });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
        await CrearTipoAsync(await Admin("b"), nombre);
    }

    [Fact]
    public async Task Una_organizacion_creada_por_la_plataforma_recibe_los_tipos_de_persona_iniciales()
    {
        if (await api.ScalarAsync<int>("SELECT count(*)::int FROM usuario WHERE id_usuario = 1") == 0)
            await api.CrearUsuarioConIdAsync(1, $"plataforma.{_sufijo}@ejemplo.test", _orgA);
        var plataforma = await api.ClienteAutenticadoAsync((await api.ScalarAsync<string>("SELECT correo FROM usuario WHERE id_usuario = 1"))!);
        var tipo = await api.ScalarAsync<int>("SELECT id_tipo_organizacion FROM organizacion WHERE id_organizacion = @p0", _orgA);
        var plan = await api.ScalarAsync<int>("SELECT id_plan FROM organizacion WHERE id_organizacion = @p0", _orgA);
        var crear = await plataforma.PostAsJsonAsync("/api/administracion/organizaciones", new
        {
            nombre = $"Nueva {_sufijo}", identificacion = $"NIT-{_sufijo}", telefono = "+57 601 555 0000", correo = $"org.{_sufijo}@ejemplo.test",
            idTipoOrganizacion = tipo, idPlan = plan, estado = "Activa",
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var id = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idOrganizacion").GetInt32();
        foreach (var nombre in new[] { "Empleado", "Visitante", "Contratista", "Proveedor", "Residente", "Cliente" })
            Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM tipo_persona WHERE id_organizacion = @p0 AND nombre = @p1 AND activo", id, nombre));
    }

    // ---------- Foto ----------

    [Fact]
    public async Task La_foto_valida_el_contenido_real_y_se_guarda_en_el_almacenamiento_privado()
    {
        var admin = await Admin("a");
        var id = (await CrearAsync(admin, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();

        var falsa = new MultipartFormDataContent { { new ByteArrayContent("no soy una imagen"u8.ToArray()) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }, "archivo", "foto.png" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/personas/{id}/foto", falsa)).StatusCode);

        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        var valida = new MultipartFormDataContent { { new ByteArrayContent(png) { Headers = { ContentType = new MediaTypeHeaderValue("image/png") } }, "archivo", "foto.png" } };
        var cargar = await admin.PostAsync($"/api/personas/{id}/foto", valida);
        Assert.Equal(HttpStatusCode.OK, cargar.StatusCode);
        Assert.True((await cargar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tieneFoto").GetBoolean());
        Assert.Equal(_orgA, await api.ScalarAsync<int>("SELECT a.id_organizacion FROM persona p JOIN archivo a ON a.id_archivo = p.id_archivo_foto WHERE p.id_persona = @p0", id));

        var descarga = await admin.GetAsync($"/api/personas/{id}/foto");
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("image/png", descarga.Content.Headers.ContentType?.MediaType);
    }

    // ---------- Teléfonos ----------

    [Fact]
    public async Task Los_telefonos_validan_formato_evitan_duplicados_y_mantienen_un_solo_principal()
    {
        var admin = await Admin("a");
        var id = (await CrearAsync(admin, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "12345", tipo = "Móvil" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "3001234567", tipo = "Satelital" })).StatusCode);

        var movil = await admin.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "300 123 4567", tipo = "Móvil", principal = true });
        Assert.Equal(HttpStatusCode.Created, movil.StatusCode);
        var idMovil = (await movil.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idTelefono").GetInt32();
        Assert.Equal("3001234567", await api.ScalarAsync<string>("SELECT numero FROM persona_telefono WHERE id_telefono = @p0", idMovil));

        var duplicado = await admin.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "300-123-4567", tipo = "Trabajo" });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var fijo = await admin.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "6015550000", tipo = "Fijo", principal = true });
        Assert.Equal(HttpStatusCode.Created, fijo.StatusCode);
        var idFijo = (await fijo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idTelefono").GetInt32();
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM persona_telefono WHERE id_persona = @p0 AND principal AND activo", id));
        Assert.True(await api.ScalarAsync<bool>("SELECT principal FROM persona_telefono WHERE id_telefono = @p0", idFijo));

        var editar = await admin.PutAsJsonAsync($"/api/personas/{id}/telefonos/{idMovil}", new { codigoPais = "+57", numero = "3109998877", tipo = "Emergencia", principal = false });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        var quitar = await admin.PatchAsJsonAsync($"/api/personas/{id}/telefonos/{idMovil}/estado", new { activo = false });
        Assert.Equal(HttpStatusCode.OK, quitar.StatusCode);
        var detalle = await admin.GetFromJsonAsync<JsonElement>($"/api/personas/{id}");
        var telefonos = detalle.GetProperty("telefonos").EnumerateArray().ToList();
        Assert.Contains(telefonos, x => x.GetProperty("idTelefono").GetInt32() == idMovil && !x.GetProperty("activo").GetBoolean());
        Assert.Equal(id, telefonos[0].GetProperty("idPersona").GetInt32());
    }

    [Fact]
    public async Task No_se_asocian_ni_modifican_telefonos_de_personas_de_otra_organizacion()
    {
        var adminA = await Admin("a");
        var id = (await CrearAsync(adminA, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();
        var tel = await adminA.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "3001112233", tipo = "Móvil" });
        var idTel = (await tel.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idTelefono").GetInt32();

        var adminB = await Admin("b");
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.PostAsJsonAsync($"/api/personas/{id}/telefonos", new { codigoPais = "+57", numero = "3004445566", tipo = "Móvil" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.PutAsJsonAsync($"/api/personas/{id}/telefonos/{idTel}", new { codigoPais = "+57", numero = "3004445566", tipo = "Móvil" })).StatusCode);

        // Un teléfono no puede moverse a otra persona por la ruta: debe pertenecer a la persona indicada.
        var otra = (await CrearAsync(adminA, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();
        Assert.Equal(HttpStatusCode.NotFound, (await adminA.PutAsJsonAsync($"/api/personas/{otra}/telefonos/{idTel}", new { codigoPais = "+57", numero = "3001112233", tipo = "Móvil" })).StatusCode);
    }

    // ---------- Vehículos ----------

    [Fact]
    public async Task Los_vehiculos_validan_la_placa_y_se_comparten_entre_personas_de_la_misma_organizacion()
    {
        var adminA = await Admin("a");
        var ana = (await CrearAsync(adminA, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();
        var luis = (await CrearAsync(adminA, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();
        var placa = $"K{Random.Shared.Next(10, 99)}{Random.Shared.Next(100, 999)}";

        Assert.Equal(HttpStatusCode.BadRequest, (await adminA.PostAsJsonAsync($"/api/personas/{ana}/vehiculos", new { placa = "A-1", tipo = "Automóvil" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await adminA.PostAsJsonAsync($"/api/personas/{ana}/vehiculos", new { placa, tipo = "Nave" })).StatusCode);

        var crear = await adminA.PostAsJsonAsync($"/api/personas/{ana}/vehiculos", new { placa = placa.ToLowerInvariant().Insert(3, "-"), tipo = "Automóvil", marca = "Renault", modelo = "Logan", color = "Gris", relacion = "Propietario" });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var vehiculo = await crear.Content.ReadFromJsonAsync<JsonElement>();
        var idVehiculo = vehiculo.GetProperty("idVehiculo").GetInt32();
        Assert.Equal(placa, vehiculo.GetProperty("placa").GetString());

        // La misma placa en la misma persona: duplicado.
        Assert.Equal(HttpStatusCode.Conflict, (await adminA.PostAsJsonAsync($"/api/personas/{ana}/vehiculos", new { placa, tipo = "Automóvil" })).StatusCode);

        // La misma placa para otra persona de la organización: se vincula el mismo vehículo (relación N:M).
        var compartido = await adminA.PostAsJsonAsync($"/api/personas/{luis}/vehiculos", new { placa, tipo = "Automóvil", relacion = "Conductor" });
        Assert.Equal(HttpStatusCode.Created, compartido.StatusCode);
        Assert.Equal(idVehiculo, (await compartido.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idVehiculo").GetInt32());
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM vehiculo WHERE id_organizacion = @p0 AND placa_normalizada = @p1", _orgA, placa));
        Assert.Equal(2, await api.ScalarAsync<int>("SELECT count(*)::int FROM persona_vehiculo WHERE id_vehiculo = @p0", idVehiculo));

        // Otra organización registra la misma placa como un vehículo propio.
        var adminB = await Admin("b");
        var personaB = (await CrearAsync(adminB, NuevaPersona(Documento(), tipo: _tipoB))).GetProperty("idPersona").GetInt32();
        var enB = await adminB.PostAsJsonAsync($"/api/personas/{personaB}/vehiculos", new { placa, tipo = "Automóvil" });
        Assert.Equal(HttpStatusCode.Created, enB.StatusCode);
        Assert.NotEqual(idVehiculo, (await enB.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idVehiculo").GetInt32());

        // Editar e inactivar el vínculo sin borrar el vehículo ni afectar a la otra persona.
        var editar = await adminA.PutAsJsonAsync($"/api/personas/{ana}/vehiculos/{idVehiculo}", new { placa, tipo = "Camioneta", marca = "Renault", modelo = "Duster", color = "Blanco", relacion = "Propietario" });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        Assert.Equal("Duster", await api.ScalarAsync<string>("SELECT modelo FROM vehiculo WHERE id_vehiculo = @p0", idVehiculo));
        Assert.Equal(HttpStatusCode.OK, (await adminA.PatchAsJsonAsync($"/api/personas/{ana}/vehiculos/{idVehiculo}/estado", new { activo = false })).StatusCode);
        Assert.False(await api.ScalarAsync<bool>("SELECT activo FROM persona_vehiculo WHERE id_persona = @p0 AND id_vehiculo = @p1", ana, idVehiculo));
        Assert.True(await api.ScalarAsync<bool>("SELECT activo FROM persona_vehiculo WHERE id_persona = @p0 AND id_vehiculo = @p1", luis, idVehiculo));
        Assert.True(await api.ScalarAsync<bool>("SELECT activo FROM vehiculo WHERE id_vehiculo = @p0", idVehiculo));

        // Reactivar el vínculo al volver a asociarlo.
        Assert.Equal(HttpStatusCode.Created, (await adminA.PostAsJsonAsync($"/api/personas/{ana}/vehiculos", new { placa, tipo = "Camioneta" })).StatusCode);
        Assert.True(await api.ScalarAsync<bool>("SELECT activo FROM persona_vehiculo WHERE id_persona = @p0 AND id_vehiculo = @p1", ana, idVehiculo));
    }

    [Fact]
    public async Task No_se_asocian_vehiculos_a_personas_de_otra_organizacion()
    {
        var adminA = await Admin("a");
        var id = (await CrearAsync(adminA, NuevaPersona(Documento()))).GetProperty("idPersona").GetInt32();
        var crear = await adminA.PostAsJsonAsync($"/api/personas/{id}/vehiculos", new { placa = $"Z{Random.Shared.Next(10, 99)}{Random.Shared.Next(100, 999)}", tipo = "Motocicleta" });
        var idVehiculo = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idVehiculo").GetInt32();

        var adminB = await Admin("b");
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.PostAsJsonAsync($"/api/personas/{id}/vehiculos", new { placa = "QWE123", tipo = "Automóvil" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminB.PatchAsJsonAsync($"/api/personas/{id}/vehiculos/{idVehiculo}/estado", new { activo = false })).StatusCode);
        Assert.True(await api.ScalarAsync<bool>("SELECT activo FROM persona_vehiculo WHERE id_persona = @p0", id));
    }

    [Fact]
    public async Task Las_relaciones_de_personas_tienen_claves_foraneas_en_la_base()
    {
        var fks = await api.ScalarAsync<string>(
            """
            SELECT string_agg(conrelid::regclass || '->' || confrelid::regclass, ',' ORDER BY 1)
            FROM pg_constraint WHERE contype = 'f'
              AND conrelid::regclass::text IN ('persona','persona_telefono','persona_vehiculo','vehiculo','tipo_persona')
            """);
        foreach (var esperada in new[] { "persona->organizacion", "persona->archivo", "persona->tipo_persona", "persona->tipo_documento",
                     "persona_telefono->persona", "persona_vehiculo->persona", "persona_vehiculo->vehiculo", "vehiculo->organizacion", "tipo_persona->organizacion" })
            Assert.Contains(esperada, fks!.Split(','));
        // Varias personas pueden compartir el mismo tipo (índice único erróneo corregido).
        Assert.Equal(0, await api.ScalarAsync<int>("SELECT count(*)::int FROM pg_indexes WHERE indexname = 'IX_persona_id_organizacion_id_tipo_persona' AND indexdef LIKE 'CREATE UNIQUE%'"));
    }
}
