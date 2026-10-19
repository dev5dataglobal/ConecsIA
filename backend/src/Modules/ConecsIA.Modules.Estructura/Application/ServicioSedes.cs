using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Estructura.Application;

public sealed record SolicitudCrearSede(string? Nombre, string? Direccion, string? Ciudad, string? Pais, string? ZonaHoraria);

public sealed record SolicitudModificarSede(
    string? Nombre,
    string? Direccion,
    string? Ciudad,
    string? Pais,
    string? ZonaHoraria,
    uint Version);

public sealed record ConsultaSedes(EstadoSede? Estado = null, int Pagina = 1, int Tamano = 20);

public sealed record SedeDto(
    Guid Id,
    string Nombre,
    string? Direccion,
    string? Ciudad,
    string? Pais,
    string ZonaHoraria,
    string Estado,
    uint Version,
    DateTimeOffset CreadaEn,
    DateTimeOffset ActualizadaEn,
    DateTimeOffset? InactivadaEn)
{
    public static SedeDto Desde(Sede s) => new(
        s.Id, s.Nombre, s.Direccion, s.Ciudad, s.Pais, s.ZonaHoraria, s.Estado.ToString(),
        s.Version, s.CreadaEn, s.ActualizadaEn, s.InactivadaEn);
}

public sealed record PaginaDto<T>(IReadOnlyList<T> Elementos, int Total, int Pagina, int Tamano);

public interface IRepositorioSedes
{
    /// <summary>Solo dentro de la organización del contexto (filtro global): otra organización = null.</summary>
    Task<Sede?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluir, CancellationToken cancelacion);

    Task<(IReadOnlyList<Sede> Elementos, int Total)> ListarAsync(
        EstadoSede? estado,
        IReadOnlySet<Guid>? soloSedes,
        int pagina,
        int tamano,
        CancellationToken cancelacion);

    void Agregar(Sede sede);

    /// <summary>Fija la versión que el cliente leyó para que el guardado detecte cambios concurrentes.</summary>
    void EsperarVersion(Sede sede, uint version);
}

/// <summary>
/// Casos de uso de HU-013. Cada operación exige permiso + alcance en el servidor (ADR-005) y
/// audita en la misma transacción (ADR-014). La organización siempre sale del contexto verificado.
/// </summary>
public sealed class ServicioSedes(
    IRepositorioSedes repositorio,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string CodigoNombreDuplicado = "SEDE_NOMBRE_DUPLICADO";
    private const string TipoObjeto = "Sede";

    public async Task<PaginaDto<SedeDto>> ListarAsync(ConsultaSedes consulta, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.SedesConsultar);

        var alcance = contexto.AlcanceTotalSedes ? null : contexto.SedesAsignadas;
        var (elementos, total) = await repositorio.ListarAsync(consulta.Estado, alcance, consulta.Pagina, consulta.Tamano, cancelacion);
        return new PaginaDto<SedeDto>(elementos.Select(SedeDto.Desde).ToList(), total, consulta.Pagina, consulta.Tamano);
    }

    public async Task<SedeDto> ObtenerAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.SedesConsultar);
        return SedeDto.Desde(await CargarEnAlcanceAsync(id, cancelacion));
    }

    public async Task<SedeDto> CrearAsync(SolicitudCrearSede solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.SedesCrear);
        if (!contexto.AlcanceTotalSedes)
        {
            throw new AccesoDenegadoException(AccesoDenegadoException.CodigoFueraDeAlcance, "Crear sedes requiere alcance sobre toda la organización.");
        }

        await ExigirNombreLibreAsync(solicitud.Nombre!, excluir: null, cancelacion);

        var sede = Sede.Crear(
            contexto.OrganizacionId!.Value,
            solicitud.Nombre!,
            solicitud.Direccion,
            solicitud.Ciudad,
            solicitud.Pais,
            solicitud.ZonaHoraria!,
            reloj.UtcNow);
        repositorio.Agregar(sede);
        auditoria.Registrar(new EntradaAuditoria("sede.crear", TipoObjeto, sede.Id, ResultadoAuditoria.Exitoso, ValoresNuevos: Instantanea(sede)));

        // El índice único respalda la verificación previa ante altas concurrentes (409).
        await unidad.GuardarAsync(cancelacion);
        return SedeDto.Desde(sede);
    }

    public async Task<SedeDto> ModificarAsync(Guid id, SolicitudModificarSede solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.SedesModificar);
        var sede = await CargarEnAlcanceAsync(id, cancelacion);
        await ExigirNombreLibreAsync(solicitud.Nombre!, excluir: id, cancelacion);

        var anterior = Instantanea(sede);
        repositorio.EsperarVersion(sede, solicitud.Version);
        sede.Modificar(solicitud.Nombre!, solicitud.Direccion, solicitud.Ciudad, solicitud.Pais, solicitud.ZonaHoraria!, reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("sede.modificar", TipoObjeto, sede.Id, ResultadoAuditoria.Exitoso, anterior, Instantanea(sede)));

        await unidad.GuardarAsync(cancelacion);
        return SedeDto.Desde(sede);
    }

    public async Task<SedeDto> InactivarAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.SedesInactivar);
        var sede = await CargarEnAlcanceAsync(id, cancelacion);

        var anterior = new { Estado = sede.Estado.ToString() };
        sede.Inactivar(reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria(
            "sede.inactivar", TipoObjeto, sede.Id, ResultadoAuditoria.Exitoso, anterior, new { Estado = sede.Estado.ToString(), sede.InactivadaEn }));

        await unidad.GuardarAsync(cancelacion);
        return SedeDto.Desde(sede);
    }

    private async Task<Sede> CargarEnAlcanceAsync(Guid id, CancellationToken cancelacion)
    {
        // Primero existencia (otra organización = 404, CA-01) y luego alcance (403, CA-02).
        var sede = await repositorio.ObtenerAsync(id, cancelacion) ?? throw new RecursoNoEncontradoException("la sede");
        AccesoDenegadoException.ExigirSede(contexto, sede.Id);
        return sede;
    }

    private async Task ExigirNombreLibreAsync(string nombre, Guid? excluir, CancellationToken cancelacion)
    {
        if (await repositorio.ExisteNombreAsync(Sede.NormalizarNombre(nombre), excluir, cancelacion))
        {
            throw new ConflictoException(CodigoNombreDuplicado, "Ya existe una sede con ese nombre en la organización.");
        }
    }

    private static object Instantanea(Sede s) => new { s.Nombre, s.Direccion, s.Ciudad, s.Pais, s.ZonaHoraria, Estado = s.Estado.ToString() };
}

internal static class ReglasSede
{
    public static void Aplicar<T>(
        AbstractValidator<T> v,
        Func<T, string?> nombre,
        Func<T, string?> direccion,
        Func<T, string?> ciudad,
        Func<T, string?> pais,
        Func<T, string?> zona)
    {
        v.RuleFor(x => nombre(x)).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio.")
            .MaximumLength(Sede.LargoNombre).WithMessage($"El nombre admite hasta {Sede.LargoNombre} caracteres.")
            .OverridePropertyName("nombre");
        v.RuleFor(x => direccion(x)).MaximumLength(Sede.LargoDireccion).OverridePropertyName("direccion");
        v.RuleFor(x => ciudad(x)).MaximumLength(Sede.LargoCiudadPais).OverridePropertyName("ciudad");
        v.RuleFor(x => pais(x)).MaximumLength(Sede.LargoCiudadPais).OverridePropertyName("pais");
        v.RuleFor(x => zona(x)).Must(ZonaHoraria.EsIanaValida)
            .WithMessage("La zona horaria no es un identificador IANA válido.")
            .OverridePropertyName("zonaHoraria");
    }
}

internal sealed class ValidadorCrearSede : AbstractValidator<SolicitudCrearSede>
{
    public ValidadorCrearSede() =>
        ReglasSede.Aplicar(this, s => s.Nombre, s => s.Direccion, s => s.Ciudad, s => s.Pais, s => s.ZonaHoraria);
}

internal sealed class ValidadorModificarSede : AbstractValidator<SolicitudModificarSede>
{
    public ValidadorModificarSede() =>
        ReglasSede.Aplicar(this, s => s.Nombre, s => s.Direccion, s => s.Ciudad, s => s.Pais, s => s.ZonaHoraria);
}

internal sealed class ValidadorConsultaSedes : AbstractValidator<ConsultaSedes>
{
    public ValidadorConsultaSedes()
    {
        RuleFor(c => c.Pagina).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(c => c.Tamano).InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}
