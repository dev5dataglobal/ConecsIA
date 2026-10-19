using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Organizaciones;

namespace ConecsIA.UnitTests.Identidad;

/// <summary>HU-007 (ADR-005): el contexto de organización se deriva de una membresía activa verificada.</summary>
public class ResolvedorContextoTests
{
    private static readonly Guid OrgA = Guid.NewGuid();
    private static readonly Guid OrgB = Guid.NewGuid();

    private readonly RepositorioFalso repositorio = new();
    private readonly OrganizacionesFalsas organizaciones = new();
    private ResolvedorContexto Resolvedor => new(repositorio, organizaciones);

    public ResolvedorContextoTests()
    {
        organizaciones.Activas.Add(OrgA);
        organizaciones.Activas.Add(OrgB);
    }

    private Usuario AgregarUsuario(bool activo = true)
    {
        var usuario = Usuario.Crear("persona@ejemplo.test", "Persona Sintética");
        if (!activo) usuario.Inactivar();
        repositorio.Usuarios.Add(usuario);
        return usuario;
    }

    private Membresia AgregarMembresia(Usuario usuario, Guid organizacion, params string[] permisos)
    {
        var membresia = Membresia.Crear(usuario.Id, organizacion, permisos, alcanceTotalSedes: true, sedesAsignadas: []);
        repositorio.Membresias.Add(membresia);
        return membresia;
    }

    [Fact]
    public async Task Usuario_inexistente_no_es_valido()
    {
        var resultado = await Resolvedor.ResolverAsync(Guid.NewGuid(), organizacionSeleccionada: null, CancellationToken.None);

        Assert.Equal(EstadoResolucion.UsuarioNoValido, resultado.Estado);
    }

    [Fact]
    public async Task Usuario_inactivo_no_es_valido()
    {
        var usuario = AgregarUsuario(activo: false);
        AgregarMembresia(usuario, OrgA, "sedes.consultar");

        var resultado = await Resolvedor.ResolverAsync(usuario.Id, null, CancellationToken.None);

        Assert.Equal(EstadoResolucion.UsuarioNoValido, resultado.Estado);
    }

    [Fact]
    public async Task Una_sola_membresia_activa_se_usa_sin_seleccion()
    {
        var usuario = AgregarUsuario();
        AgregarMembresia(usuario, OrgA, "sedes.consultar");

        var resultado = await Resolvedor.ResolverAsync(usuario.Id, null, CancellationToken.None);

        Assert.Equal(EstadoResolucion.Resuelto, resultado.Estado);
        Assert.Equal(OrgA, resultado.Membresia!.OrganizacionId);
    }

    [Fact]
    public async Task Seleccionar_una_organizacion_sin_membresia_se_rechaza()
    {
        var usuario = AgregarUsuario();
        AgregarMembresia(usuario, OrgA, "sedes.consultar");

        var resultado = await Resolvedor.ResolverAsync(usuario.Id, OrgB, CancellationToken.None);

        Assert.Equal(EstadoResolucion.OrganizacionNoPermitida, resultado.Estado);
    }

    [Fact]
    public async Task Varias_membresias_sin_seleccion_exigen_elegir()
    {
        var usuario = AgregarUsuario();
        AgregarMembresia(usuario, OrgA, "sedes.consultar");
        AgregarMembresia(usuario, OrgB, "sedes.consultar");

        var sinSeleccion = await Resolvedor.ResolverAsync(usuario.Id, null, CancellationToken.None);
        var conSeleccion = await Resolvedor.ResolverAsync(usuario.Id, OrgB, CancellationToken.None);

        Assert.Equal(EstadoResolucion.OrganizacionNoSeleccionada, sinSeleccion.Estado);
        Assert.Equal(EstadoResolucion.Resuelto, conSeleccion.Estado);
        Assert.Equal(OrgB, conSeleccion.Membresia!.OrganizacionId);
    }

    [Fact]
    public async Task Membresia_inactiva_no_da_contexto()
    {
        var usuario = AgregarUsuario();
        AgregarMembresia(usuario, OrgA, "sedes.consultar").Inactivar();

        var resultado = await Resolvedor.ResolverAsync(usuario.Id, null, CancellationToken.None);

        Assert.Equal(EstadoResolucion.SinMembresia, resultado.Estado);
    }

    [Fact]
    public async Task Organizacion_inactiva_no_da_contexto()
    {
        var usuario = AgregarUsuario();
        AgregarMembresia(usuario, OrgA, "sedes.consultar");
        organizaciones.Activas.Remove(OrgA);

        var resultado = await Resolvedor.ResolverAsync(usuario.Id, null, CancellationToken.None);

        Assert.Equal(EstadoResolucion.SinMembresia, resultado.Estado);
    }

    [Fact]
    public void Los_permisos_se_comparan_por_clave_nunca_por_nombre_de_rol()
    {
        var membresia = Membresia.Crear(Guid.NewGuid(), OrgA, ["Administrador"], alcanceTotalSedes: true, sedesAsignadas: []);

        Assert.False(membresia.TienePermiso("sedes.crear"));
    }

    [Fact]
    public async Task Los_permisos_efectivos_unen_los_roles_activos_asignados_y_las_claves_directas()
    {
        var usuario = AgregarUsuario();
        var ahora = DateTimeOffset.UtcNow;
        var activo = Rol.Crear(OrgA, "Consulta", null, ["roles.consultar"], null, ahora);
        var inactivo = Rol.Crear(OrgA, "Viejo", null, ["sedes.crear"], null, ahora);
        inactivo.Inactivar(ahora);
        var deOtraOrganizacion = Rol.Crear(OrgB, "Ajeno", null, ["sedes.inactivar"], null, ahora);
        repositorio.Roles.AddRange([activo, inactivo, deOtraOrganizacion]);
        repositorio.Membresias.Add(Membresia.Crear(
            usuario.Id, OrgA, ["sedes.consultar"], alcanceTotalSedes: true, sedesAsignadas: [],
            roles: [activo.Id, inactivo.Id, deOtraOrganizacion.Id]));

        var resultado = await Resolvedor.ResolverAsync(usuario.Id, null, CancellationToken.None);

        Assert.Equal(["roles.consultar", "sedes.consultar"], resultado.PermisosEfectivos!.Order());
    }

    private sealed class RepositorioFalso : IRepositorioIdentidad
    {
        public List<Usuario> Usuarios { get; } = [];
        public List<Membresia> Membresias { get; } = [];

        public Task<Usuario?> ObtenerUsuarioAsync(Guid id, CancellationToken cancelacion) =>
            Task.FromResult(Usuarios.SingleOrDefault(u => u.Id == id));

        public Task<IReadOnlyList<Membresia>> MembresiasActivasAsync(Guid usuarioId, CancellationToken cancelacion) =>
            Task.FromResult<IReadOnlyList<Membresia>>(Membresias.Where(m => m.UsuarioId == usuarioId && m.Activa).ToList());

        public List<Rol> Roles { get; } = [];

        public Task<IReadOnlyList<Rol>> RolesActivosAsync(Guid organizacionId, IReadOnlyCollection<Guid> ids, CancellationToken cancelacion) =>
            Task.FromResult<IReadOnlyList<Rol>>(Roles.Where(r => r.OrganizacionId == organizacionId && r.Activo && ids.Contains(r.Id)).ToList());

        public Task<IReadOnlyList<Usuario>> UsuariosActivosAsync(CancellationToken cancelacion) =>
            Task.FromResult<IReadOnlyList<Usuario>>(Usuarios.Where(u => u.Activo).ToList());

        public Task<Usuario?> ObtenerPorCorreoParaActualizarAsync(string correoNormalizado, CancellationToken cancelacion) =>
            Task.FromResult(Usuarios.SingleOrDefault(u => u.Correo == correoNormalizado));

        public void AgregarSesion(SesionUsuario sesion) => throw new NotSupportedException();

        public void AgregarUsuario(Usuario usuario) => Usuarios.Add(usuario);

        public void AgregarMembresia(Membresia membresia) => Membresias.Add(membresia);

        public Task<SesionUsuario?> ObtenerSesionAsync(Guid sesionId, CancellationToken cancelacion) =>
            throw new NotSupportedException();

        public Task<SesionUsuario?> ObtenerSesionParaActualizarAsync(Guid sesionId, CancellationToken cancelacion) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<SesionUsuario>> SesionesVigentesAsync(Guid usuarioId, DateTimeOffset ahora, CancellationToken cancelacion) =>
            throw new NotSupportedException();
    }

    private sealed class OrganizacionesFalsas : IConsultaOrganizaciones
    {
        public HashSet<Guid> Activas { get; } = [];

        public Task<OrganizacionResumen?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
            Task.FromResult<OrganizacionResumen?>(new OrganizacionResumen(id, "Org", Activas.Contains(id)));
    }
}
