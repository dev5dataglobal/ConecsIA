using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Identidad;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Estructura.Application;

public sealed record SolicitudCrearArea(string? Nombre, string? Tipo);

public sealed record SolicitudModificarArea(string? Nombre, string? Tipo, uint Version);

public sealed record SolicitudAsignacion(IReadOnlyList<Guid>? Usuarios);

public sealed record AreaDto(
    Guid Id, Guid SedeId, string Nombre, string? Tipo, string Estado, uint Version,
    IReadOnlyList<Guid> Responsables, IReadOnlyList<Guid> Autorizadores,
    DateTimeOffset CreadaEn, DateTimeOffset ActualizadaEn, DateTimeOffset? InactivadaEn)
{
    public static AreaDto Desde(Area a) => new(
        a.Id, a.SedeId, a.Nombre, a.Tipo, a.Estado.ToString(), a.Version,
        Vigentes(a, FuncionEnArea.Responsable), Vigentes(a, FuncionEnArea.Autorizador),
        a.CreadaEn, a.ActualizadaEn, a.InactivadaEn);

    private static List<Guid> Vigentes(Area a, FuncionEnArea funcion) =>
        a.Asignaciones.Where(x => x.Funcion == funcion && x.HastaEn is null).Select(x => x.UsuarioId).ToList();
}

public sealed record AsignacionDto(Guid UsuarioId, string Funcion, DateTimeOffset DesdeEn, Guid? AsignadaPor, DateTimeOffset? HastaEn, Guid? RetiradaPor);

public interface IRepositorioAreas
{
    /// <summary>Con sus asignaciones; solo de la organización del contexto (filtro global).</summary>
    Task<Area?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    Task<IReadOnlyList<Area>> DeSedeAsync(Guid sedeId, CancellationToken cancelacion);

    Task<bool> ExisteNombreAsync(Guid sedeId, string nombreNormalizado, Guid? excluir, CancellationToken cancelacion);

    void Agregar(Area area);

    void EsperarVersion(Area area, uint version);
}

/// <summary>
/// HU-014. Toda operación exige permiso y el alcance de la sede del área (HU-018); otra organización
/// = 404. Los usuarios asignados deben ser miembros activos de la organización.
/// </summary>
public sealed class ServicioAreas(
    IRepositorioAreas repositorio,
    IRepositorioSedes sedes,
    IConsultaUsuarios usuarios,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string CodigoNombreDuplicado = "AREA_NOMBRE_DUPLICADO";
    public const string CodigoUsuarioInactivo = "USUARIO_INACTIVO";
    private const string TipoObjeto = "Area";

    public async Task<IReadOnlyList<AreaDto>> ListarAsync(Guid sedeId, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.AreasConsultar);
        await SedeEnAlcanceAsync(sedeId, cancelacion);
        return (await repositorio.DeSedeAsync(sedeId, cancelacion)).Select(AreaDto.Desde).ToList();
    }

    public async Task<AreaDto> ObtenerAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.AreasConsultar);
        return AreaDto.Desde(await CargarAsync(id, cancelacion));
    }

    public async Task<IReadOnlyList<AsignacionDto>> HistorialAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.AreasConsultar);
        var area = await CargarAsync(id, cancelacion);
        return area.Asignaciones.OrderBy(a => a.DesdeEn)
            .Select(a => new AsignacionDto(a.UsuarioId, a.Funcion.ToString(), a.DesdeEn, a.AsignadaPor, a.HastaEn, a.RetiradaPor))
            .ToList();
    }

    public async Task<AreaDto> CrearAsync(Guid sedeId, SolicitudCrearArea solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.AreasCrear);
        await SedeEnAlcanceAsync(sedeId, cancelacion);
        await ExigirNombreLibreAsync(sedeId, solicitud.Nombre!, null, cancelacion);

        var area = Area.Crear(contexto.OrganizacionId!.Value, sedeId, solicitud.Nombre!, solicitud.Tipo, reloj.UtcNow);
        repositorio.Agregar(area);
        auditoria.Registrar(new EntradaAuditoria("area.crear", TipoObjeto, area.Id, ResultadoAuditoria.Exitoso,
            ValoresNuevos: new { area.SedeId, area.Nombre, area.Tipo }));

        await unidad.GuardarAsync(cancelacion);
        return AreaDto.Desde(area);
    }

    public async Task<AreaDto> ModificarAsync(Guid id, SolicitudModificarArea solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.AreasModificar);
        var area = await CargarAsync(id, cancelacion);
        await ExigirNombreLibreAsync(area.SedeId, solicitud.Nombre!, id, cancelacion);

        var anterior = new { area.Nombre, area.Tipo };
        repositorio.EsperarVersion(area, solicitud.Version);
        area.Modificar(solicitud.Nombre!, solicitud.Tipo, reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("area.modificar", TipoObjeto, area.Id, ResultadoAuditoria.Exitoso, anterior, new { area.Nombre, area.Tipo }));

        await unidad.GuardarAsync(cancelacion);
        return AreaDto.Desde(area);
    }

    public async Task<AreaDto> InactivarAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.AreasInactivar);
        var area = await CargarAsync(id, cancelacion);
        area.Inactivar(reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("area.inactivar", TipoObjeto, area.Id, ResultadoAuditoria.Exitoso,
            new { Estado = "Activa" }, new { Estado = "Inactiva", area.InactivadaEn }));

        await unidad.GuardarAsync(cancelacion);
        return AreaDto.Desde(area);
    }

    public async Task<AreaDto> AsignarAsync(Guid id, FuncionEnArea funcion, SolicitudAsignacion solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.AreasAsignar);
        var area = await CargarAsync(id, cancelacion);
        var ids = (solicitud.Usuarios ?? []).Distinct().ToList();
        var estados = await usuarios.ActivosEnOrganizacionAsync(ids, cancelacion);
        if (ids.Any(u => !estados.ContainsKey(u))) throw new RecursoNoEncontradoException("el usuario");
        if (ids.Any(u => !estados[u])) throw new ReglaNegocioException(CodigoUsuarioInactivo, "No se puede asignar un usuario inactivo.");

        var (agregados, retirados) = area.AsignarVigentes(funcion, ids, contexto.UsuarioId, reloj.UtcNow);
        var accion = funcion == FuncionEnArea.Autorizador ? "area.autorizadores" : "area.responsables";
        auditoria.Registrar(new EntradaAuditoria(accion, TipoObjeto, area.Id, ResultadoAuditoria.Exitoso,
            new { Retirados = retirados }, new { Agregados = agregados }));

        await unidad.GuardarAsync(cancelacion);
        return AreaDto.Desde(area);
    }

    private async Task<Area> CargarAsync(Guid id, CancellationToken cancelacion)
    {
        var area = await repositorio.ObtenerAsync(id, cancelacion) ?? throw new RecursoNoEncontradoException("el área");
        AccesoDenegadoException.ExigirSede(contexto, area.SedeId);
        return area;
    }

    private async Task SedeEnAlcanceAsync(Guid sedeId, CancellationToken cancelacion)
    {
        _ = await sedes.ObtenerAsync(sedeId, cancelacion) ?? throw new RecursoNoEncontradoException("la sede");
        AccesoDenegadoException.ExigirSede(contexto, sedeId);
    }

    private async Task ExigirNombreLibreAsync(Guid sedeId, string nombre, Guid? excluir, CancellationToken cancelacion)
    {
        if (await repositorio.ExisteNombreAsync(sedeId, Area.NormalizarNombre(nombre), excluir, cancelacion))
        {
            throw new ConflictoException(CodigoNombreDuplicado, "Ya existe un área con ese nombre en la sede.");
        }
    }
}

internal sealed class ValidadorCrearArea : AbstractValidator<SolicitudCrearArea>
{
    public ValidadorCrearArea()
    {
        RuleFor(s => s.Nombre).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio.")
            .MaximumLength(Area.LargoNombre).OverridePropertyName("nombre");
        RuleFor(s => s.Tipo).MaximumLength(Area.LargoTipo).OverridePropertyName("tipo");
    }
}

internal sealed class ValidadorModificarArea : AbstractValidator<SolicitudModificarArea>
{
    public ValidadorModificarArea()
    {
        RuleFor(s => s.Nombre).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio.")
            .MaximumLength(Area.LargoNombre).OverridePropertyName("nombre");
        RuleFor(s => s.Tipo).MaximumLength(Area.LargoTipo).OverridePropertyName("tipo");
    }
}
