using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConecsIA.Tests.Infraestructura;

namespace ConecsIA.Tests;

/// <summary>
/// Administración de usuarios, organizaciones, perfiles y zonas empresariales; acciones por módulo
/// (consultar / añadir / editar) y contexto de organización.
/// Escenario: organizaciones A y B; en A un administrador (todos los módulos de administración) y perfiles limitados.
/// </summary>
public sealed class AdministracionTests(ApiFactory api) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly string[] ModulosAdministracion = ["usuarios", "perfiles", "organizaciones", "zonas_empresariales"];
    private int _orgA, _orgB, _rolAdminA, _rolOperadorA, _rolAdminB, _tipo, _plan;
    private string _sufijo = "";

    public async Task InitializeAsync()
    {
        _sufijo = Guid.NewGuid().ToString("N")[..8];
        _orgA = await api.CrearOrganizacionAsync($"Org A {_sufijo}");
        _orgB = await api.CrearOrganizacionAsync($"Org B {_sufijo}");
        _tipo = await api.ScalarAsync<int>("SELECT id_tipo_organizacion FROM organizacion WHERE id_organizacion = @p0", _orgA);
        _plan = await api.ScalarAsync<int>("SELECT id_plan FROM organizacion WHERE id_organizacion = @p0", _orgA);
        _rolAdminA = await api.CrearRolAsync(_orgA, "Administrador A");
        _rolOperadorA = await api.CrearRolAsync(_orgA, "Operador A");
        _rolAdminB = await api.CrearRolAsync(_orgB, "Administrador B");
        foreach (var modulo in ModulosAdministracion)
        {
            await api.AsignarModuloAsync(_orgA, _rolAdminA, modulo);
            await api.AsignarModuloAsync(_orgB, _rolAdminB, modulo);
        }
        await api.AsignarModuloAsync(_orgA, _rolOperadorA, "dashboard");
        await api.AsignarModuloAsync(_orgA, _rolAdminA, "dashboard");
        await api.CrearUsuarioAsync(Correo("admin"), _orgA, _rolAdminA);
        await api.CrearUsuarioAsync(Correo("adminb"), _orgB, _rolAdminB);
        if (await api.ScalarAsync<int>("SELECT count(*)::int FROM usuario WHERE id_usuario = 1") == 0)
            await api.CrearUsuarioConIdAsync(1, $"plataforma.{_sufijo}@ejemplo.test", _orgA);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private string Correo(string nombre) => $"{nombre}.{_sufijo}@ejemplo.test";

    private async Task<HttpClient> PlataformaAsync()
    {
        var correo = await api.ScalarAsync<string>("SELECT correo FROM usuario WHERE id_usuario = 1");
        return await api.ClienteAutenticadoAsync(correo!);
    }

    private Task<int> MembresiaAsync(string correo) =>
        api.ScalarAsync<int>("SELECT m.id_membresia FROM membresia_organizacion m JOIN usuario u ON u.id_usuario = m.id_usuario WHERE u.correo = @p0", correo);

    private async Task<HttpResponseMessage> LoginAsync(string correo) =>
        await api.CreateClient().PostAsJsonAsync("/api/auth/login", new { correo, password = ApiFactory.Contrasena });

    private async Task<int> RolConAccionesAsync(int org, string nombre, string modulo, bool consultar, bool anadir, bool editar)
    {
        var rol = await api.CrearRolAsync(org, $"{nombre} {Guid.NewGuid():N}"[..30]);
        await api.AsignarModuloAsync(org, rol, modulo, consultar, anadir, editar);
        return rol;
    }

    private object OrganizacionNueva(string nombre, int? idZona = null) => new
    {
        nombre,
        identificacion = $"NIT-{Guid.NewGuid():N}"[..20],
        telefono = "+57 601 555 0000",
        correo = $"contacto.{Guid.NewGuid():N}@ejemplo.test",
        direccion = "Calle 1 # 2-3",
        idTipoOrganizacion = _tipo,
        idPlan = _plan,
        idZonaEmpresarial = idZona,
        estado = "Activa",
    };


    // ---------- Usuarios ----------

    [Fact]
    public async Task Crear_y_editar_un_usuario_asigna_y_cambia_su_perfil_y_evita_correos_duplicados()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var asignables = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/usuarios/perfiles-asignables");
        Assert.Contains(asignables.EnumerateArray(), x => x.GetProperty("id").GetInt32() == _rolOperadorA);
        Assert.DoesNotContain(asignables.EnumerateArray(), x => x.GetProperty("id").GetInt32() == _rolAdminB);

        var crear = await admin.PostAsJsonAsync("/api/administracion/usuarios", new { correo = Correo("nuevo"), contrasena = ApiFactory.Contrasena, idRoles = new[] { _rolOperadorA } });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var creado = await crear.Content.ReadFromJsonAsync<JsonElement>();
        var idMembresia = creado.GetProperty("idMembresia").GetInt32();
        Assert.Equal(_orgA, creado.GetProperty("idOrganizacion").GetInt32());
        Assert.Equal([_rolOperadorA], creado.GetProperty("idRoles").EnumerateArray().Select(x => x.GetInt32()));

        var duplicado = await admin.PostAsJsonAsync("/api/administracion/usuarios", new { correo = Correo("NUEVO").ToUpperInvariant(), contrasena = ApiFactory.Contrasena, idRoles = Array.Empty<int>() });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        var editar = await admin.PutAsJsonAsync($"/api/administracion/usuarios/{idMembresia}", new { correo = Correo("nuevo"), idRoles = new[] { _rolAdminA }, activo = true });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        Assert.Equal(_rolAdminA, await api.ScalarAsync<int>("SELECT id_rol FROM membresia_rol WHERE id_membresia = @p0", idMembresia));

        // Un perfil de otra organización no se puede asignar.
        var ajeno = await admin.PutAsJsonAsync($"/api/administracion/usuarios/{idMembresia}", new { correo = Correo("nuevo"), idRoles = new[] { _rolAdminB }, activo = true });
        Assert.Equal(HttpStatusCode.BadRequest, ajeno.StatusCode);
        Assert.True(await api.ScalarAsync<int>("SELECT count(*)::int FROM auditoria WHERE entidad = 'Usuario' AND id_organizacion = @p0", _orgA) >= 2);
    }

    [Fact]
    public async Task Inactivar_un_usuario_impide_su_ingreso_y_cierra_su_sesion_activa()
    {
        var correo = Correo("inactivable");
        await api.CrearUsuarioAsync(correo, _orgA, _rolAdminA);
        var sesion = await api.ClienteAutenticadoAsync(correo);
        Assert.Equal(HttpStatusCode.OK, (await sesion.GetAsync("/api/administracion/usuarios")).StatusCode);

        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var inactivar = await admin.PatchAsJsonAsync($"/api/administracion/usuarios/{await MembresiaAsync(correo)}/estado", new { activo = false });
        Assert.Equal(HttpStatusCode.OK, inactivar.StatusCode);
        Assert.False((await inactivar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("activo").GetBoolean());

        Assert.Equal(HttpStatusCode.Unauthorized, (await sesion.GetAsync("/api/administracion/usuarios")).StatusCode);
        var login = await LoginAsync(correo);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Contains("inactivo", await login.Content.ReadAsStringAsync());
        // La cuenta y sus perfiles se conservan.
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM membresia_rol WHERE id_membresia = @p0", await MembresiaAsync(correo)));
    }

    [Fact]
    public async Task Un_usuario_no_puede_inactivarse_a_si_mismo()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var respuesta = await admin.PatchAsJsonAsync($"/api/administracion/usuarios/{await MembresiaAsync(Correo("admin"))}/estado", new { activo = false });
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // ---------- Acciones por módulo ----------

    [Fact]
    public async Task Las_acciones_de_consultar_anadir_y_editar_se_validan_por_separado_en_el_backend()
    {
        var soloConsulta = await RolConAccionesAsync(_orgA, "Consulta", "usuarios", consultar: true, anadir: false, editar: false);
        var soloAnadir = await RolConAccionesAsync(_orgA, "Alta", "usuarios", consultar: false, anadir: true, editar: false);
        await api.CrearUsuarioAsync(Correo("consulta"), _orgA, soloConsulta);
        await api.CrearUsuarioAsync(Correo("alta"), _orgA, soloAnadir);
        var consulta = await api.ClienteAutenticadoAsync(Correo("consulta"));
        var alta = await api.ClienteAutenticadoAsync(Correo("alta"));
        var idMembresia = await MembresiaAsync(Correo("admin"));

        Assert.Equal(HttpStatusCode.OK, (await consulta.GetAsync("/api/administracion/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PostAsJsonAsync("/api/administracion/usuarios", new { correo = Correo("x1"), contrasena = ApiFactory.Contrasena })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PutAsJsonAsync($"/api/administracion/usuarios/{idMembresia}", new { correo = Correo("admin"), activo = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.PatchAsJsonAsync($"/api/administracion/usuarios/{idMembresia}/estado", new { activo = false })).StatusCode);

        // Añadir no implica Consultar.
        Assert.Equal(HttpStatusCode.Forbidden, (await alta.GetAsync("/api/administracion/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await alta.PostAsJsonAsync("/api/administracion/usuarios", new { correo = Correo("x2"), contrasena = ApiFactory.Contrasena })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await alta.PutAsJsonAsync($"/api/administracion/usuarios/{idMembresia}", new { correo = Correo("admin"), activo = true })).StatusCode);

        // La deducción por verbo también protege los demás controladores (POST de personas = Añadir).
        Assert.Equal(HttpStatusCode.Forbidden, (await consulta.GetAsync("/api/personas")).StatusCode);
    }

    [Fact]
    public async Task La_matriz_de_un_perfil_guarda_acciones_separadas_y_no_permite_otorgar_acciones_ajenas()
    {
        var limitado = await RolConAccionesAsync(_orgA, "Gestor", "perfiles", consultar: true, anadir: false, editar: true);
        await api.AsignarModuloAsync(_orgA, limitado, "usuarios", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("gestor"), _orgA, limitado);
        var gestor = await api.ClienteAutenticadoAsync(Correo("gestor"));
        var idUsuarios = await api.ScalarAsync<int>("SELECT id_app_modulo FROM app_modulos WHERE codigo = 'usuarios'");
        var destino = await api.CrearRolAsync(_orgA, $"Destino {_sufijo}");

        var propio = await gestor.PutAsJsonAsync($"/api/roles/{destino}/modulos", new { asignaciones = new[] { new { idAppModulo = idUsuarios, consultar = true, anadir = false, editar = false } } });
        Assert.Equal(HttpStatusCode.OK, propio.StatusCode);
        Assert.Equal("t,f,f", await api.ScalarAsync<string>("SELECT concat_ws(',', puede_consultar, puede_anadir, puede_editar) FROM modulos_permisos WHERE id_rol = @p0", destino));

        var escalar = await gestor.PutAsJsonAsync($"/api/roles/{destino}/modulos", new { asignaciones = new[] { new { idAppModulo = idUsuarios, consultar = true, anadir = true, editar = false } } });
        Assert.Equal(HttpStatusCode.Forbidden, escalar.StatusCode);
        Assert.Equal("t,f,f", await api.ScalarAsync<string>("SELECT concat_ws(',', puede_consultar, puede_anadir, puede_editar) FROM modulos_permisos WHERE id_rol = @p0", destino));

        // Un módulo sin acciones se retira del perfil.
        var retirar = await gestor.PutAsJsonAsync($"/api/roles/{destino}/modulos", new { asignaciones = new[] { new { idAppModulo = idUsuarios, consultar = false, anadir = false, editar = false } } });
        Assert.Equal(HttpStatusCode.OK, retirar.StatusCode);
        Assert.Equal(0, await api.ScalarAsync<int>("SELECT count(*)::int FROM modulos_permisos WHERE id_rol = @p0", destino));
    }

    // ---------- Perfiles ----------

    [Fact]
    public async Task Inactivar_un_perfil_quita_sus_permisos_a_los_usuarios_sin_borrar_asignaciones()
    {
        var rol = await RolConAccionesAsync(_orgA, "Temporal", "usuarios", consultar: true, anadir: false, editar: false);
        await api.CrearUsuarioAsync(Correo("temporal"), _orgA, rol);
        var usuario = await api.ClienteAutenticadoAsync(Correo("temporal"));
        Assert.Equal(HttpStatusCode.OK, (await usuario.GetAsync("/api/administracion/usuarios")).StatusCode);

        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        Assert.Equal(HttpStatusCode.OK, (await admin.PatchAsJsonAsync($"/api/administracion/perfiles/{rol}/estado", new { activo = false })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await usuario.GetAsync("/api/administracion/usuarios")).StatusCode);
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM modulos_permisos WHERE id_rol = @p0", rol));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM membresia_rol WHERE id_rol = @p0", rol));
    }

    [Fact]
    public async Task Crear_y_editar_perfiles_valida_nombres_y_la_organizacion()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var crear = await admin.PostAsJsonAsync("/api/administracion/perfiles", new { nombre = $"Recepción {_sufijo}", descripcion = "Recepción", activo = true });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var id = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idRol").GetInt32();
        Assert.Equal(_orgA, await api.ScalarAsync<int>("SELECT id_organizacion FROM rol WHERE id_rol = @p0", id));

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/administracion/perfiles", new { nombre = $"RECEPCIÓN {_sufijo}".ToUpper() })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/perfiles/{id}", new { nombre = $"Recepción norte {_sufijo}", activo = true })).StatusCode);
        // Un perfil de otra organización no existe para A.
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/administracion/perfiles/{_rolAdminB}", new { nombre = "Tomado", activo = false })).StatusCode);
        Assert.True(await api.ScalarAsync<bool>("SELECT activo FROM rol WHERE id_rol = @p0", _rolAdminB));
    }

    // ---------- Organizaciones ----------

    [Fact]
    public async Task Inactivar_una_organizacion_bloquea_el_ingreso_y_conserva_sus_registros()
    {
        var org = await api.CrearOrganizacionAsync($"Org temporal {_sufijo}");
        var rol = await api.CrearRolAsync(org, "Admin temporal");
        await api.AsignarModuloAsync(org, rol, "usuarios");
        await api.CrearUsuarioAsync(Correo("orgtemporal"), org, rol);
        var sesion = await api.ClienteAutenticadoAsync(Correo("orgtemporal"));

        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PatchAsJsonAsync($"/api/administracion/organizaciones/{org}/estado", new { activo = false })).StatusCode);

        var plataforma = await PlataformaAsync();
        Assert.Equal(HttpStatusCode.OK, (await plataforma.PatchAsJsonAsync($"/api/administracion/organizaciones/{org}/estado", new { activo = false })).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await sesion.GetAsync("/api/administracion/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(Correo("orgtemporal"))).StatusCode);
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM membresia_organizacion WHERE id_organizacion = @p0", org));
        Assert.Equal(1, await api.ScalarAsync<int>("SELECT count(*)::int FROM rol WHERE id_organizacion = @p0", org));
    }

    [Fact]
    public async Task Solo_la_plataforma_crea_organizaciones_y_se_validan_duplicados_y_campos()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var plataforma = await PlataformaAsync();
        var datos = OrganizacionNueva($"Nueva {_sufijo}");

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/administracion/organizaciones", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await plataforma.PostAsJsonAsync("/api/administracion/organizaciones", new { nombre = "Sin datos" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await plataforma.PostAsJsonAsync("/api/administracion/organizaciones", datos)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await plataforma.PostAsJsonAsync("/api/administracion/organizaciones", datos)).StatusCode);

        // Un administrador de organización edita su organización, pero no la de otro ni su estado.
        var propia = await admin.GetFromJsonAsync<JsonElement>($"/api/administracion/organizaciones/{_orgA}");
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/administracion/organizaciones/{_orgB}")).StatusCode);
        var listado = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/organizaciones");
        Assert.Equal([_orgA], listado.EnumerateArray().Select(x => x.GetProperty("idOrganizacion").GetInt32()));
        var edicion = new
        {
            nombre = propia.GetProperty("nombre").GetString(), identificacion = $"NIT-A-{_sufijo}", telefono = "601 555 1111", correo = "a@ejemplo.test",
            idTipoOrganizacion = propia.GetProperty("idTipoOrganizacion").GetInt32(), idPlan = propia.GetProperty("idPlan").GetInt32(), estado = "Inactiva",
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"/api/administracion/organizaciones/{_orgA}", edicion)).StatusCode);
        var valida = new { edicion.nombre, edicion.identificacion, edicion.telefono, edicion.correo, edicion.idTipoOrganizacion, edicion.idPlan, estado = "Activa" };
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/administracion/organizaciones/{_orgA}", valida)).StatusCode);
        Assert.Equal($"NIT-A-{_sufijo}", await api.ScalarAsync<string>("SELECT identificacion FROM organizacion WHERE id_organizacion = @p0", _orgA));
    }

    [Fact]
    public async Task El_logo_es_opcional_y_solo_acepta_imagenes()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/administracion/organizaciones/{_orgA}/logo")).StatusCode);

        using var texto = new MultipartFormDataContent { { new ByteArrayContent("no es imagen"u8.ToArray()), "archivo", "logo.png" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/administracion/organizaciones/{_orgA}/logo", texto)).StatusCode);

        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        using var imagen = new MultipartFormDataContent { { new ByteArrayContent(png), "archivo", "logo.png" } };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/administracion/organizaciones/{_orgA}/logo", imagen)).StatusCode);
        var descarga = await admin.GetAsync($"/api/administracion/organizaciones/{_orgA}/logo");
        Assert.Equal("image/png", descarga.Content.Headers.ContentType?.MediaType);
        using var otra = new MultipartFormDataContent { { new ByteArrayContent(png), "archivo", "logo.png" } };
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync($"/api/administracion/organizaciones/{_orgB}/logo", otra)).StatusCode);
    }

    // ---------- Zonas empresariales ----------

    [Fact]
    public async Task La_plataforma_crea_una_zona_con_su_organizacion_superior_y_gestiona_sus_empresas()
    {
        var plataforma = await PlataformaAsync();
        var empresa1 = await api.CrearOrganizacionAsync($"Empresa 1 {_sufijo}");
        var empresa2 = await api.CrearOrganizacionAsync($"Empresa 2 {_sufijo}");
        var zona = new { nombre = $"Zona Franca {_sufijo}", descripcion = "Parque", activa = true, organizacionSuperior = OrganizacionNueva($"Administradora {_sufijo}"), idEmpresas = new[] { empresa1, empresa2 } };

        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/zonas-empresariales", zona)).StatusCode);

        var crear = await plataforma.PostAsJsonAsync("/api/zonas-empresariales", zona);
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var creada = await crear.Content.ReadFromJsonAsync<JsonElement>();
        var idZona = creada.GetProperty("idZonaEmpresarial").GetInt32();
        var idSuperior = creada.GetProperty("organizacionSuperior").GetProperty("idOrganizacion").GetInt32();
        Assert.True(creada.GetProperty("organizacionSuperior").GetProperty("esOrganizacionSuperior").GetBoolean());
        Assert.Equal(2, creada.GetProperty("empresas").GetArrayLength());

        // Una empresa no puede quedar en dos zonas activas.
        var otra = new { nombre = $"Otra zona {_sufijo}", activa = true, organizacionSuperior = OrganizacionNueva($"Otra admin {_sufijo}"), idEmpresas = new[] { empresa1 } };
        Assert.Equal(HttpStatusCode.Conflict, (await plataforma.PostAsJsonAsync("/api/zonas-empresariales", otra)).StatusCode);

        // Retirar una empresa desactiva el vínculo sin borrarlo.
        var edicion = await plataforma.PutAsJsonAsync($"/api/zonas-empresariales/{idZona}", new { zona.nombre, zona.descripcion, activa = true, organizacionSuperior = zona.organizacionSuperior, idEmpresas = new[] { empresa1 } });
        Assert.Equal(HttpStatusCode.OK, edicion.StatusCode);
        Assert.False(await api.ScalarAsync<bool>("SELECT activa FROM zona_empresarial_organizacion WHERE id_organizacion = @p0 AND id_zona_empresarial = @p1", empresa2, idZona));
        var organizacion = await plataforma.GetFromJsonAsync<JsonElement>($"/api/administracion/organizaciones/{empresa1}");
        Assert.Equal(idZona, organizacion.GetProperty("idZonaEmpresarial").GetInt32());

        // Inactivar la zona inactiva su organización superior sin tocar a las empresas.
        Assert.Equal(HttpStatusCode.OK, (await plataforma.PatchAsJsonAsync($"/api/zonas-empresariales/{idZona}/estado", new { activo = false })).StatusCode);
        Assert.Equal("Inactiva", await api.ScalarAsync<string>("SELECT estado FROM organizacion WHERE id_organizacion = @p0", idSuperior));
        Assert.Equal("Activa", await api.ScalarAsync<string>("SELECT estado FROM organizacion WHERE id_organizacion = @p0", empresa1));
    }

    [Fact]
    public async Task Una_empresa_ve_su_zona_pero_no_las_demas_empresas()
    {
        var plataforma = await PlataformaAsync();
        var crear = await plataforma.PostAsJsonAsync("/api/zonas-empresariales", new { nombre = $"Zona B {_sufijo}", activa = true, organizacionSuperior = OrganizacionNueva($"Superior B {_sufijo}"), idEmpresas = new[] { _orgB } });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);

        var adminB = await api.ClienteAutenticadoAsync(Correo("adminb"));
        var zonas = await adminB.GetFromJsonAsync<JsonElement>("/api/zonas-empresariales");
        var zona = Assert.Single(zonas.EnumerateArray());
        Assert.Equal(0, zona.GetProperty("empresas").GetArrayLength());

        var adminA = await api.ClienteAutenticadoAsync(Correo("admin"));
        Assert.Equal(0, (await adminA.GetFromJsonAsync<JsonElement>("/api/zonas-empresariales")).GetArrayLength());
    }

    // ---------- Contexto de organización ----------

    [Fact]
    public async Task Solo_el_administrador_de_plataforma_cambia_de_organizacion_y_ve_solo_la_seleccionada()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        admin.DefaultRequestHeaders.Add("X-Organizacion-Id", _orgB.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/administracion/usuarios")).StatusCode);

        var plataforma = await PlataformaAsync();
        plataforma.DefaultRequestHeaders.Add("X-Organizacion-Id", _orgB.ToString());
        var usuarios = await plataforma.GetFromJsonAsync<JsonElement>("/api/administracion/usuarios");
        Assert.NotEqual(0, usuarios.GetArrayLength());
        Assert.All(usuarios.EnumerateArray(), x => Assert.Equal(_orgB, x.GetProperty("idOrganizacion").GetInt32()));
        var perfiles = await plataforma.GetFromJsonAsync<JsonElement>("/api/administracion/perfiles");
        Assert.All(perfiles.EnumerateArray(), x => Assert.Equal(_orgB, x.GetProperty("idOrganizacion").GetInt32()));

        // Sin cabecera vuelve a su organización: el contexto no persiste entre peticiones.
        var sinCabecera = await PlataformaAsync();
        var orgPlataforma = await api.ScalarAsync<int>("SELECT id_organizacion FROM membresia_organizacion WHERE id_usuario = 1");
        var propios = await sinCabecera.GetFromJsonAsync<JsonElement>("/api/administracion/usuarios");
        Assert.All(propios.EnumerateArray(), x => Assert.Equal(orgPlataforma, x.GetProperty("idOrganizacion").GetInt32()));

        // No se puede seleccionar una organización inexistente.
        var invalida = await PlataformaAsync();
        invalida.DefaultRequestHeaders.Add("X-Organizacion-Id", "999999");
        Assert.Equal(HttpStatusCode.Forbidden, (await invalida.GetAsync("/api/administracion/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Los_endpoints_rechazan_registros_de_otra_organizacion_aunque_se_manipulen_rutas_o_cuerpos()
    {
        var admin = await api.ClienteAutenticadoAsync(Correo("admin"));
        var membresiaB = await MembresiaAsync(Correo("adminb"));

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/administracion/usuarios/{membresiaB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/administracion/usuarios/{membresiaB}", new { correo = Correo("adminb"), activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PatchAsJsonAsync($"/api/administracion/usuarios/{membresiaB}/estado", new { activo = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/roles/{_rolAdminB}/modulos")).StatusCode);
        // Un identificador de organización en el cuerpo se ignora: el usuario se crea en la organización activa.
        var crear = await admin.PostAsJsonAsync("/api/administracion/usuarios", new { correo = Correo("cuerpo"), contrasena = ApiFactory.Contrasena, idOrganizacion = _orgB });
        Assert.Equal(_orgA, (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idOrganizacion").GetInt32());
        Assert.Equal("Activa", await api.ScalarAsync<string>("SELECT estado FROM membresia_organizacion WHERE id_membresia = @p0", membresiaB));

        var usuarios = await admin.GetFromJsonAsync<JsonElement>("/api/administracion/usuarios?organizacionId=" + _orgB);
        Assert.All(usuarios.EnumerateArray(), x => Assert.Equal(_orgA, x.GetProperty("idOrganizacion").GetInt32()));
    }
}
