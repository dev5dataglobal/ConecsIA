using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Estructura.Application;

public sealed record SolicitudCrearZona(string? Nombre, string? Descripcion, bool EsPuntoAcceso, ModoPuntoAcceso? Modo);

public sealed record SolicitudModificarZona(string? Nombre, string? Descripcion, bool EsPuntoAcceso, ModoPuntoAcceso? Modo, uint Version);

public sealed record ZonaDto(
    Guid Id, Guid SedeId, string Nombre, string? Descripcion, bool EsPuntoAcceso, string? Modo, string Estado, uint Version,
    DateTimeOffset CreadaEn, DateTimeOffset ActualizadaEn, DateTimeOffset? InactivadaEn)
{
    public static ZonaDto Desde(Zona z) => new(
        z.Id, z.SedeId, z.Nombre, z.Descripcion, z.EsPuntoAcceso, z.Modo?.ToString(), z.Estado.ToString(), z.Version,
        z.CreadaEn, z.ActualizadaEn, z.InactivadaEn);
}

public interface IRepositorioZonas
{
    Task<Zona?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    Task<IReadOnlyList<Zona>> DeSedeAsync(Guid sedeId, CancellationToken cancelacion);

    Task<IReadOnlyList<Zona>> VariasAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion);

    Task<bool> ExisteNombreAsync(Guid sedeId, string nombreNormalizado, Guid? excluir, CancellationToken cancelacion);

    void Agregar(Zona zona);

    void EsperarVersion(Zona zona, uint version);
}

/// <summary>
/// HU-015 (ADR-019). Permiso y alcance de sede en todo; con <c>operacion</c> el listado devuelve solo
/// los puntos activos que admiten esa operación y que el usuario puede operar (HU-018).
/// </summary>
public sealed class ServicioZonas(
    IRepositorioZonas repositorio,
    IRepositorioSedes sedes,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string CodigoNombreDuplicado = "ZONA_NOMBRE_DUPLICADO";
    private const string TipoObjeto = "Zona";

    public async Task<IReadOnlyList<ZonaDto>> ListarAsync(Guid sedeId, ModoPuntoAcceso? operacion, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ZonasConsultar);
        await SedeEnAlcanceAsync(sedeId, cancelacion);
        var zonas = await repositorio.DeSedeAsync(sedeId, cancelacion);
        if (operacion is { } op)
        {
            zonas = zonas.Where(z => z.Admite(op) && contexto.PuedeOperarEnPunto(z.Id, z.SedeId)).ToList();
        }

        return zonas.Select(ZonaDto.Desde).ToList();
    }

    public async Task<ZonaDto> ObtenerAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ZonasConsultar);
        return ZonaDto.Desde(await CargarAsync(id, cancelacion));
    }

    public async Task<ZonaDto> CrearAsync(Guid sedeId, SolicitudCrearZona solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ZonasCrear);
        await SedeEnAlcanceAsync(sedeId, cancelacion);
        await ExigirNombreLibreAsync(sedeId, solicitud.Nombre!, null, cancelacion);

        var zona = Zona.Crear(contexto.OrganizacionId!.Value, sedeId, solicitud.Nombre!, solicitud.Descripcion, solicitud.EsPuntoAcceso, solicitud.Modo, reloj.UtcNow);
        repositorio.Agregar(zona);
        auditoria.Registrar(new EntradaAuditoria("zona.crear", TipoObjeto, zona.Id, ResultadoAuditoria.Exitoso, ValoresNuevos: Instantanea(zona)));

        await unidad.GuardarAsync(cancelacion);
        return ZonaDto.Desde(zona);
    }

    public async Task<ZonaDto> ModificarAsync(Guid id, SolicitudModificarZona solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ZonasModificar);
        var zona = await CargarAsync(id, cancelacion);
        await ExigirNombreLibreAsync(zona.SedeId, solicitud.Nombre!, id, cancelacion);

        var anterior = Instantanea(zona);
        repositorio.EsperarVersion(zona, solicitud.Version);
        zona.Modificar(solicitud.Nombre!, solicitud.Descripcion, solicitud.EsPuntoAcceso, solicitud.Modo, reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("zona.modificar", TipoObjeto, zona.Id, ResultadoAuditoria.Exitoso, anterior, Instantanea(zona)));

        await unidad.GuardarAsync(cancelacion);
        return ZonaDto.Desde(zona);
    }

    public async Task<ZonaDto> InactivarAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ZonasInactivar);
        var zona = await CargarAsync(id, cancelacion);
        zona.Inactivar(reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("zona.inactivar", TipoObjeto, zona.Id, ResultadoAuditoria.Exitoso,
            new { Estado = "Activa" }, new { Estado = "Inactiva", zona.InactivadaEn }));

        await unidad.GuardarAsync(cancelacion);
        return ZonaDto.Desde(zona);
    }

    private async Task<Zona> CargarAsync(Guid id, CancellationToken cancelacion)
    {
        var zona = await repositorio.ObtenerAsync(id, cancelacion) ?? throw new RecursoNoEncontradoException("la zona");
        AccesoDenegadoException.ExigirSede(contexto, zona.SedeId);
        return zona;
    }

    private async Task SedeEnAlcanceAsync(Guid sedeId, CancellationToken cancelacion)
    {
        _ = await sedes.ObtenerAsync(sedeId, cancelacion) ?? throw new RecursoNoEncontradoException("la sede");
        AccesoDenegadoException.ExigirSede(contexto, sedeId);
    }

    private async Task ExigirNombreLibreAsync(Guid sedeId, string nombre, Guid? excluir, CancellationToken cancelacion)
    {
        if (await repositorio.ExisteNombreAsync(sedeId, Zona.NormalizarNombre(nombre), excluir, cancelacion))
        {
            throw new ConflictoException(CodigoNombreDuplicado, "Ya existe una zona con ese nombre en la sede.");
        }
    }

    private static object Instantanea(Zona z) => new { z.Nombre, z.Descripcion, z.EsPuntoAcceso, Modo = z.Modo?.ToString(), Estado = z.Estado.ToString() };
}

internal static class ReglasZona
{
    public static void Aplicar<T>(AbstractValidator<T> v, Func<T, string?> nombre, Func<T, string?> descripcion, Func<T, bool> punto, Func<T, ModoPuntoAcceso?> modo)
    {
        v.RuleFor(x => nombre(x)).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio.")
            .MaximumLength(Zona.LargoNombre).OverridePropertyName("nombre");
        v.RuleFor(x => descripcion(x)).MaximumLength(Zona.LargoDescripcion).OverridePropertyName("descripcion");
        v.RuleFor(x => x).Must(x => punto(x) == modo(x).HasValue)
            .WithMessage("Un punto de acceso requiere modo (Entrada, Salida o Ambos) y una zona que no lo es no lo admite.")
            .OverridePropertyName("modo");
    }
}

internal sealed class ValidadorCrearZona : AbstractValidator<SolicitudCrearZona>
{
    public ValidadorCrearZona() => ReglasZona.Aplicar(this, s => s.Nombre, s => s.Descripcion, s => s.EsPuntoAcceso, s => s.Modo);
}

internal sealed class ValidadorModificarZona : AbstractValidator<SolicitudModificarZona>
{
    public ValidadorModificarZona() => ReglasZona.Aplicar(this, s => s.Nombre, s => s.Descripcion, s => s.EsPuntoAcceso, s => s.Modo);
}
