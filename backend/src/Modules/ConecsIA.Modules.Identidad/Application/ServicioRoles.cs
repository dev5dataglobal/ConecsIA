using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Identidad.Application;

public sealed record SolicitudCrearRol(string? Nombre, string? Descripcion, IReadOnlyList<string>? Permisos, string? Plantilla);

public sealed record SolicitudModificarRol(string? Nombre, string? Descripcion, IReadOnlyList<string>? Permisos, uint Version);

public sealed record RolDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    IReadOnlyList<string> Permisos,
    string? Plantilla,
    string Estado,
    uint Version,
    DateTimeOffset CreadoEn,
    DateTimeOffset ActualizadoEn,
    DateTimeOffset? InactivadoEn)
{
    public static RolDto Desde(Rol r) => new(
        r.Id, r.Nombre, r.Descripcion, r.Permisos, r.Plantilla, r.Activo ? "Activo" : "Inactivo",
        r.Version, r.CreadoEn, r.ActualizadoEn, r.InactivadoEn);
}

public interface IRepositorioRoles
{
    /// <summary>Solo dentro de la organización del contexto (filtro global): otra organización = null.</summary>
    Task<Rol?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    Task<IReadOnlyList<Rol>> ListarAsync(bool? activos, CancellationToken cancelacion);

    Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluir, CancellationToken cancelacion);

    /// <summary>¿Hay otro rol activo de la organización que conceda el permiso?</summary>
    Task<bool> OtroRolActivoConcedeAsync(string permiso, Guid excluir, CancellationToken cancelacion);

    void Agregar(Rol rol);

    void EsperarVersion(Rol rol, uint version);
}

/// <summary>
/// HU-017: roles configurables por organización. Permisos del catálogo cerrado, nunca por nombre de
/// rol; nadie concede permisos que no tiene (anti-escalamiento); no se puede dejar la organización
/// sin un rol activo que administre roles (propuesta). Todo se audita con valores anterior/nuevo.
/// </summary>
public sealed class ServicioRoles(
    IRepositorioRoles repositorio,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string CodigoNombreDuplicado = "ROL_NOMBRE_DUPLICADO";
    public const string CodigoEscalamiento = "ESCALAMIENTO_DE_PERMISOS";
    public const string CodigoUltimoAdministrador = "ULTIMO_ROL_ADMINISTRADOR";
    public const string CodigoPlantillaDesconocida = "PLANTILLA_DESCONOCIDA";
    private const string TipoObjeto = "Rol";

    public IReadOnlyList<DefinicionPermiso> Catalogo()
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.RolesConsultar);
        return CatalogoPermisos.Definiciones;
    }

    public IReadOnlyList<PlantillaRol> Plantillas()
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.RolesConsultar);
        return PlantillasRol.Todas;
    }

    public async Task<IReadOnlyList<RolDto>> ListarAsync(bool? activos, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.RolesConsultar);
        return (await repositorio.ListarAsync(activos, cancelacion)).Select(RolDto.Desde).ToList();
    }

    public async Task<RolDto> ObtenerAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.RolesConsultar);
        return RolDto.Desde(await CargarAsync(id, cancelacion));
    }

    public async Task<RolDto> CrearAsync(SolicitudCrearRol solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.RolesAdministrar);
        if (solicitud.Plantilla is { } clave && PlantillasRol.Todas.All(p => p.Clave != clave))
        {
            throw new ReglaNegocioException(CodigoPlantillaDesconocida, "La plantilla de rol no existe.");
        }

        var permisos = solicitud.Permisos ?? [];
        ExigirSinEscalamiento(permisos);
        await ExigirNombreLibreAsync(solicitud.Nombre!, excluir: null, cancelacion);

        var rol = Rol.Crear(contexto.OrganizacionId!.Value, solicitud.Nombre!, solicitud.Descripcion, permisos, solicitud.Plantilla, reloj.UtcNow);
        repositorio.Agregar(rol);
        auditoria.Registrar(new EntradaAuditoria("rol.crear", TipoObjeto, rol.Id, ResultadoAuditoria.Exitoso, ValoresNuevos: Instantanea(rol)));

        await unidad.GuardarAsync(cancelacion);
        return RolDto.Desde(rol);
    }

    public async Task<RolDto> ModificarAsync(Guid id, SolicitudModificarRol solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.RolesAdministrar);
        var rol = await CargarAsync(id, cancelacion);
        var permisos = solicitud.Permisos ?? [];
        // Solo se exige tener lo que se agrega: quitar un permiso que el actor no tiene no escala.
        ExigirSinEscalamiento(permisos.Except(rol.Permisos, StringComparer.Ordinal));
        await ExigirNombreLibreAsync(solicitud.Nombre!, excluir: id, cancelacion);
        if (rol.Concede(Permisos.RolesAdministrar) && !permisos.Contains(Permisos.RolesAdministrar, StringComparer.Ordinal))
        {
            await ExigirOtroAdministradorAsync(rol, cancelacion);
        }

        var anterior = Instantanea(rol);
        repositorio.EsperarVersion(rol, solicitud.Version);
        rol.Modificar(solicitud.Nombre!, solicitud.Descripcion, permisos, reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("rol.modificar", TipoObjeto, rol.Id, ResultadoAuditoria.Exitoso, anterior, Instantanea(rol)));

        await unidad.GuardarAsync(cancelacion);
        return RolDto.Desde(rol);
    }

    public async Task<RolDto> InactivarAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.RolesAdministrar);
        var rol = await CargarAsync(id, cancelacion);
        if (rol.Concede(Permisos.RolesAdministrar)) await ExigirOtroAdministradorAsync(rol, cancelacion);

        var anterior = new { Estado = "Activo" };
        rol.Inactivar(reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria(
            "rol.inactivar", TipoObjeto, rol.Id, ResultadoAuditoria.Exitoso, anterior, new { Estado = "Inactivo", rol.InactivadoEn }));

        await unidad.GuardarAsync(cancelacion);
        return RolDto.Desde(rol);
    }

    private async Task<Rol> CargarAsync(Guid id, CancellationToken cancelacion) =>
        await repositorio.ObtenerAsync(id, cancelacion) ?? throw new RecursoNoEncontradoException("el rol");

    private void ExigirSinEscalamiento(IEnumerable<string> permisos)
    {
        if (permisos.Any(p => !contexto.TienePermiso(p)))
        {
            throw new AccesoDenegadoException(CodigoEscalamiento, "No puedes conceder permisos que no tienes.");
        }
    }

    private async Task ExigirOtroAdministradorAsync(Rol rol, CancellationToken cancelacion)
    {
        if (!await repositorio.OtroRolActivoConcedeAsync(Permisos.RolesAdministrar, rol.Id, cancelacion))
        {
            throw new ReglaNegocioException(CodigoUltimoAdministrador,
                "Es el único rol activo que administra roles: la organización quedaría sin quien los administre.");
        }
    }

    private async Task ExigirNombreLibreAsync(string nombre, Guid? excluir, CancellationToken cancelacion)
    {
        if (await repositorio.ExisteNombreAsync(Rol.NormalizarNombre(nombre), excluir, cancelacion))
        {
            throw new ConflictoException(CodigoNombreDuplicado, "Ya existe un rol con ese nombre en la organización.");
        }
    }

    private static object Instantanea(Rol r) => new { r.Nombre, r.Descripcion, r.Permisos, Estado = r.Activo ? "Activo" : "Inactivo" };
}

internal static class ReglasRol
{
    public static void Aplicar<T>(AbstractValidator<T> v, Func<T, string?> nombre, Func<T, string?> descripcion, Func<T, IReadOnlyList<string>?> permisos)
    {
        v.RuleFor(x => nombre(x)).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio.")
            .MaximumLength(Rol.LargoNombre).WithMessage($"El nombre admite hasta {Rol.LargoNombre} caracteres.")
            .OverridePropertyName("nombre");
        v.RuleFor(x => descripcion(x)).MaximumLength(Rol.LargoDescripcion).OverridePropertyName("descripcion");
        v.RuleFor(x => permisos(x)).NotNull().WithMessage("Indica los permisos del rol (puede ser una lista vacía).")
            .OverridePropertyName("permisos");
        v.RuleForEach(x => permisos(x)).Must(CatalogoPermisos.Existe).WithMessage("El permiso {PropertyValue} no existe en el catálogo.")
            .OverridePropertyName("permisos");
    }
}

internal sealed class ValidadorCrearRol : AbstractValidator<SolicitudCrearRol>
{
    public ValidadorCrearRol() => ReglasRol.Aplicar(this, s => s.Nombre, s => s.Descripcion, s => s.Permisos);
}

internal sealed class ValidadorModificarRol : AbstractValidator<SolicitudModificarRol>
{
    public ValidadorModificarRol() => ReglasRol.Aplicar(this, s => s.Nombre, s => s.Descripcion, s => s.Permisos);
}
