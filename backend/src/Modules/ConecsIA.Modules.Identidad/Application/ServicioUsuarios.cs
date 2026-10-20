using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Estructura;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Identidad.Application;

public sealed record SolicitudInvitacion(
    string? Correo, string? Nombre, IReadOnlyList<Guid>? Roles, bool AlcanceTotalSedes, IReadOnlyList<Guid>? Sedes);

public sealed record SolicitudRoles(IReadOnlyList<Guid>? Roles);

public sealed record SolicitudAlcance(bool AlcanceTotalSedes, IReadOnlyList<Guid>? Sedes);

public sealed record SolicitudAceptarInvitacion(string? Token, string? Contrasena);

public sealed record UsuarioOrganizacionDto(
    Guid UsuarioId,
    string Nombre,
    string Correo,
    string Estado,
    IReadOnlyList<Guid> Roles,
    bool AlcanceTotalSedes,
    IReadOnlyList<Guid> Sedes);

public sealed record InvitacionCreada(UsuarioOrganizacionDto Usuario, string Token);

public sealed class OpcionesInvitaciones
{
    public const string Seccion = "Invitaciones";

    /// <summary>Vigencia del enlace. Configurable (HU-016); el valor por defecto es una propuesta.</summary>
    public int VigenciaHoras { get; set; } = 72;
}

public interface IRepositorioUsuarios
{
    /// <summary>Membresías de una organización explícita con su usuario (Membresia no tiene filtro global).</summary>
    Task<IReadOnlyList<(Membresia Membresia, Usuario Usuario)>> DeOrganizacionAsync(Guid organizacionId, CancellationToken cancelacion);

    Task<(Membresia Membresia, Usuario Usuario)?> ObtenerParaActualizarAsync(Guid organizacionId, Guid usuarioId, CancellationToken cancelacion);

    Task<Usuario?> UsuarioPorCorreoParaActualizarAsync(string correoNormalizado, CancellationToken cancelacion);

    Task<IReadOnlyList<Rol>> RolesDeLaOrganizacionAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion);

    /// <summary>Busca por hash sin contexto de organización (la aceptación es anónima). Excepción revisada al filtro.</summary>
    Task<Invitacion?> InvitacionPorHashAsync(string hash, CancellationToken cancelacion);

    Task<(Membresia Membresia, Usuario Usuario)> MembresiaDeInvitacionAsync(Invitacion invitacion, CancellationToken cancelacion);

    void Agregar(object entidad);
}

/// <summary>
/// HU-016 y HU-018: usuarios de la organización por invitación, roles, alcance por sede e
/// inactivación. La organización sale siempre del contexto; nadie concede permisos o alcance que no tiene.
/// </summary>
public sealed class ServicioUsuarios(
    IRepositorioUsuarios repositorio,
    IConsultaSedes sedes,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj,
    OpcionesInvitaciones opciones)
{
    public const string CodigoYaEsMiembro = "USUARIO_YA_ES_MIEMBRO";
    private const string TipoObjeto = "Usuario";

    private Guid Organizacion => contexto.OrganizacionId!.Value;

    public async Task<IReadOnlyList<UsuarioOrganizacionDto>> ListarAsync(CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.UsuariosConsultar);
        return (await repositorio.DeOrganizacionAsync(Organizacion, cancelacion))
            .Select(x => Dto(x.Membresia, x.Usuario))
            .OrderBy(u => u.Nombre, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<UsuarioOrganizacionDto> ObtenerAsync(Guid usuarioId, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.UsuariosConsultar);
        var (membresia, usuario) = await CargarAsync(usuarioId, cancelacion);
        return Dto(membresia, usuario);
    }

    public async Task<InvitacionCreada> InvitarAsync(SolicitudInvitacion solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.UsuariosInvitar);
        var roles = solicitud.Roles ?? [];
        await ExigirRolesSinEscalamientoAsync(roles, cancelacion);
        await ExigirAlcanceAsignableAsync(solicitud.AlcanceTotalSedes, solicitud.Sedes ?? [], cancelacion);

        // D-03b: un correo existente recibe una membresía nueva; si ya es miembro de esta organización, 409.
        var usuario = await repositorio.UsuarioPorCorreoParaActualizarAsync(Usuario.NormalizarCorreo(solicitud.Correo!), cancelacion);
        if (usuario is null)
        {
            usuario = Usuario.Crear(solicitud.Correo!, solicitud.Nombre!);
            repositorio.Agregar(usuario);
        }
        else if ((await repositorio.ObtenerParaActualizarAsync(Organizacion, usuario.Id, cancelacion)) is not null)
        {
            throw new ConflictoException(CodigoYaEsMiembro, "Ese correo ya pertenece a un usuario de la organización.");
        }

        var membresia = Membresia.CrearInvitada(usuario.Id, Organizacion, roles, solicitud.AlcanceTotalSedes, solicitud.Sedes ?? []);
        repositorio.Agregar(membresia);
        var (invitacion, token) = Invitacion.Crear(
            Organizacion, usuario.Id, membresia.Id, contexto.UsuarioId, reloj.UtcNow, TimeSpan.FromHours(opciones.VigenciaHoras));
        repositorio.Agregar(invitacion);
        auditoria.Registrar(new EntradaAuditoria("usuario.invitar", TipoObjeto, usuario.Id, ResultadoAuditoria.Exitoso,
            ValoresNuevos: new { Roles = roles, solicitud.AlcanceTotalSedes, Sedes = solicitud.Sedes ?? [], invitacion.ExpiraEn }));

        await unidad.GuardarAsync(cancelacion);
        return new InvitacionCreada(Dto(membresia, usuario), token);
    }

    public async Task<UsuarioOrganizacionDto> AsignarRolesAsync(Guid usuarioId, SolicitudRoles solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.UsuariosAdministrar);
        var (membresia, usuario) = await CargarAsync(usuarioId, cancelacion);
        var roles = solicitud.Roles ?? [];
        await ExigirRolesSinEscalamientoAsync(roles.Except(membresia.Roles).ToList(), cancelacion);

        var anterior = new { Roles = membresia.Roles.ToList() };
        membresia.AsignarRoles(roles);
        auditoria.Registrar(new EntradaAuditoria("usuario.roles", TipoObjeto, usuario.Id, ResultadoAuditoria.Exitoso, anterior, new { Roles = roles }));

        await unidad.GuardarAsync(cancelacion);
        return Dto(membresia, usuario);
    }

    public async Task<UsuarioOrganizacionDto> AsignarAlcanceAsync(Guid usuarioId, SolicitudAlcance solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.UsuariosAdministrar);
        var (membresia, usuario) = await CargarAsync(usuarioId, cancelacion);
        await ExigirAlcanceAsignableAsync(solicitud.AlcanceTotalSedes, solicitud.Sedes ?? [], cancelacion);

        var anterior = new { membresia.AlcanceTotalSedes, Sedes = membresia.SedesAsignadas.ToList() };
        membresia.AsignarAlcance(solicitud.AlcanceTotalSedes, solicitud.Sedes ?? []);
        auditoria.Registrar(new EntradaAuditoria("usuario.alcance", TipoObjeto, usuario.Id, ResultadoAuditoria.Exitoso, anterior,
            new { membresia.AlcanceTotalSedes, Sedes = membresia.SedesAsignadas.ToList() }));

        await unidad.GuardarAsync(cancelacion);
        return Dto(membresia, usuario);
    }

    /// <summary>Inactiva la membresía y revoca las sesiones del usuario. El usuario y su autoría se conservan.</summary>
    public async Task<UsuarioOrganizacionDto> InactivarAsync(Guid usuarioId, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.UsuariosAdministrar);
        var (membresia, usuario) = await CargarAsync(usuarioId, cancelacion);
        var anterior = new { Estado = Estado(membresia) };
        membresia.Inactivar();
        usuario.RevocarSesiones();
        auditoria.Registrar(new EntradaAuditoria("usuario.inactivar", TipoObjeto, usuario.Id, ResultadoAuditoria.Exitoso, anterior,
            new { Estado = Estado(membresia), SesionesRevocadas = true }));

        await unidad.GuardarAsync(cancelacion);
        return Dto(membresia, usuario);
    }

    public async Task<UsuarioOrganizacionDto> ReactivarAsync(Guid usuarioId, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.UsuariosAdministrar);
        var (membresia, usuario) = await CargarAsync(usuarioId, cancelacion);
        var anterior = new { Estado = Estado(membresia) };
        membresia.Reactivar();
        auditoria.Registrar(new EntradaAuditoria("usuario.reactivar", TipoObjeto, usuario.Id, ResultadoAuditoria.Exitoso, anterior,
            new { Estado = Estado(membresia) }));

        await unidad.GuardarAsync(cancelacion);
        return Dto(membresia, usuario);
    }

    private async Task<(Membresia, Usuario)> CargarAsync(Guid usuarioId, CancellationToken cancelacion) =>
        await repositorio.ObtenerParaActualizarAsync(Organizacion, usuarioId, cancelacion)
        ?? throw new RecursoNoEncontradoException("el usuario");

    /// <summary>Roles de la organización (otro = 404) cuyos permisos el actor posee todos (anti-escalamiento).</summary>
    private async Task ExigirRolesSinEscalamientoAsync(IReadOnlyList<Guid> ids, CancellationToken cancelacion)
    {
        if (ids.Count == 0) return;
        var roles = await repositorio.RolesDeLaOrganizacionAsync(ids, cancelacion);
        if (roles.Count != ids.Distinct().Count()) throw new RecursoNoEncontradoException("el rol");
        if (roles.SelectMany(r => r.Permisos).Any(p => !contexto.TienePermiso(p)))
        {
            throw new AccesoDenegadoException(ServicioRoles.CodigoEscalamiento, "No puedes asignar roles con permisos que no tienes.");
        }
    }

    /// <summary>
    /// HU-018: las sedes deben ser de la organización (otra = 404) y estar dentro del alcance del actor;
    /// solo quien tiene alcance total puede conceder alcance total.
    /// </summary>
    private async Task ExigirAlcanceAsignableAsync(bool total, IReadOnlyList<Guid> ids, CancellationToken cancelacion)
    {
        if (total && !contexto.AlcanceTotalSedes)
        {
            throw new AccesoDenegadoException(AccesoDenegadoException.CodigoFueraDeAlcance, "Solo quien tiene alcance total puede concederlo.");
        }

        if (total || ids.Count == 0) return;
        var existentes = await sedes.ExistentesAsync(ids, cancelacion);
        if (ids.Any(id => !existentes.Contains(id))) throw new RecursoNoEncontradoException("la sede");
        foreach (var id in ids) AccesoDenegadoException.ExigirSede(contexto, id);
    }

    private static string Estado(Membresia m) => m.PendienteDeAceptar ? "Pendiente" : m.Activa ? "Activo" : "Inactivo";

    private static UsuarioOrganizacionDto Dto(Membresia m, Usuario u) =>
        new(u.Id, u.Nombre, u.Correo, Estado(m), m.Roles, m.AlcanceTotalSedes, m.SedesAsignadas);
}

/// <summary>
/// HU-016: aceptación anónima por token. Define la contraseña si el usuario aún no tiene y activa
/// la membresía. Una aceptación concurrente pierde por concurrencia optimista (409).
/// </summary>
public sealed class ServicioAceptacionInvitaciones(
    IRepositorioUsuarios repositorio,
    IHasherContrasenas hasher,
    IContextoProceso proceso,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string CodigoContrasenaRequerida = "CONTRASENA_REQUERIDA";
    public const int LargoMinimoContrasena = 12;

    public async Task AceptarAsync(SolicitudAceptarInvitacion solicitud, CancellationToken cancelacion)
    {
        var invitacion = await repositorio.InvitacionPorHashAsync(Invitacion.Hashear(solicitud.Token ?? string.Empty), cancelacion)
            ?? throw new RecursoNoDisponibleException(Invitacion.CodigoNoDisponible, "La invitación no existe, venció o ya fue usada.");

        // Las escrituras pertenecen a la organización de la invitación; el proceso no tiene permisos.
        proceso.ComoProceso(invitacion.OrganizacionId, []);
        invitacion.Aceptar(reloj.UtcNow);
        var (membresia, usuario) = await repositorio.MembresiaDeInvitacionAsync(invitacion, cancelacion);
        if (usuario.HashContrasena is null)
        {
            if (solicitud.Contrasena is not { Length: >= LargoMinimoContrasena })
            {
                throw new ReglaNegocioException(CodigoContrasenaRequerida, $"Define una contraseña de al menos {LargoMinimoContrasena} caracteres.");
            }

            usuario.EstablecerContrasena(hasher.Hashear(solicitud.Contrasena));
        }

        membresia.AceptarInvitacion();
        auditoria.Registrar(new EntradaAuditoria("usuario.aceptar_invitacion", "Usuario", usuario.Id, ResultadoAuditoria.Exitoso,
            ValoresNuevos: new { Estado = "Activo" }));
        await unidad.GuardarAsync(cancelacion);
    }
}

internal sealed class ValidadorInvitacion : AbstractValidator<SolicitudInvitacion>
{
    public ValidadorInvitacion()
    {
        RuleFor(s => s.Correo).NotEmpty().EmailAddress().MaximumLength(256).WithMessage("El correo no es válido.").OverridePropertyName("correo");
        RuleFor(s => s.Nombre).NotEmpty().MaximumLength(200).WithMessage("El nombre es obligatorio.").OverridePropertyName("nombre");
    }
}
