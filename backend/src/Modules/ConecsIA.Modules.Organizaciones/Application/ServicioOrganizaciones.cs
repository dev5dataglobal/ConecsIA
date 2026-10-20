using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Organizaciones;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Organizaciones.Application;

public sealed record AdministradorInicial(string? Correo, string? Nombre);

public sealed record SolicitudCrearOrganizacion(
    string? Nombre,
    string? Tipo,
    string? IdentificacionLegal,
    string? ZonaHoraria,
    string? ContactoAdministrativo,
    AdministradorInicial? Administrador);

public sealed record SolicitudModificarOrganizacion(
    string? Nombre,
    string? Tipo,
    string? IdentificacionLegal,
    string? ZonaHoraria,
    string? ContactoAdministrativo,
    uint Version);

public sealed record SolicitudModificarMiOrganizacion(
    string? Nombre,
    string? IdentificacionLegal,
    string? ZonaHoraria,
    string? ContactoAdministrativo,
    uint Version);

public sealed record ConsultaOrganizacionesSaas(EstadoOrganizacion? Estado = null, int Pagina = 1, int Tamano = 20);

public sealed record OrganizacionDto(
    Guid Id,
    string Nombre,
    string Tipo,
    string? IdentificacionLegal,
    string? ZonaHoraria,
    string? ContactoAdministrativo,
    string Estado,
    uint Version,
    DateTimeOffset CreadaEn,
    DateTimeOffset ActualizadaEn,
    DateTimeOffset? InactivadaEn)
{
    public static OrganizacionDto Desde(Organizacion o) => new(
        o.Id, o.Nombre, o.Tipo, o.IdentificacionLegal, o.ZonaHoraria, o.ContactoAdministrativo,
        o.Estado.ToString(), o.Version, o.CreadaEn, o.ActualizadaEn, o.InactivadaEn);
}

public sealed record PaginaOrganizacionesDto(IReadOnlyList<OrganizacionDto> Elementos, int Total, int Pagina, int Tamano);

public interface IRepositorioOrganizaciones
{
    Task<Organizacion?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluir, CancellationToken cancelacion);

    Task<(IReadOnlyList<Organizacion> Elementos, int Total)> ListarAsync(
        EstadoOrganizacion? estado, int pagina, int tamano, CancellationToken cancelacion);

    void Agregar(Organizacion organizacion);

    void EsperarVersion(Organizacion organizacion, uint version);
}

/// <summary>
/// HU-009 en el plano SaaS: solo con permisos <c>saas.organizaciones.*</c>, que no dan acceso a datos
/// de clientes. El alta aprovisiona roles y primer administrador en la misma transacción.
/// </summary>
public sealed class ServicioOrganizacionesSaas(
    IRepositorioOrganizaciones repositorio,
    IProvisionOrganizacion provision,
    AplicadorPlantillas plantillas,
    IContextoSolicitud contexto,
    IContextoPlanoSaas planoSaas,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string CodigoNombreDuplicado = "ORGANIZACION_NOMBRE_DUPLICADO";
    private const string TipoObjeto = "Organizacion";

    public IReadOnlyList<TipoOrganizacion> Tipos()
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesConsultar);
        return TiposOrganizacion.Todos;
    }

    public async Task<PaginaOrganizacionesDto> ListarAsync(ConsultaOrganizacionesSaas consulta, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesConsultar);
        var (elementos, total) = await repositorio.ListarAsync(consulta.Estado, consulta.Pagina, consulta.Tamano, cancelacion);
        return new PaginaOrganizacionesDto(elementos.Select(OrganizacionDto.Desde).ToList(), total, consulta.Pagina, consulta.Tamano);
    }

    public async Task<OrganizacionDto> ObtenerAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesConsultar);
        return OrganizacionDto.Desde(await CargarAsync(id, cancelacion));
    }

    public async Task<OrganizacionDto> CrearAsync(SolicitudCrearOrganizacion solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesAdministrar);
        await ExigirNombreLibreAsync(solicitud.Nombre!, excluir: null, cancelacion);

        var organizacion = Organizacion.Crear(
            solicitud.Nombre!, solicitud.Tipo!, solicitud.IdentificacionLegal, solicitud.ZonaHoraria, solicitud.ContactoAdministrativo, reloj.UtcNow);
        repositorio.Agregar(organizacion);

        // A partir de aquí las escrituras (roles, membresía, auditoría) pertenecen a la organización nueva.
        planoSaas.OperarEnOrganizacion(organizacion.Id);
        var administrador = await provision.ProvisionarAsync(
            organizacion.Id, solicitud.Administrador!.Correo!, solicitud.Administrador.Nombre!, cancelacion);
        // HU-010: la organización nueva recibe la plantilla básica de su tipo.
        await plantillas.AplicarAsync(organizacion.Id, organizacion.Tipo, cancelacion);
        auditoria.Registrar(new EntradaAuditoria("organizacion.crear", TipoObjeto, organizacion.Id, ResultadoAuditoria.Exitoso,
            ValoresNuevos: new { Organizacion = Instantanea(organizacion), PrimerAdministrador = administrador }));

        await unidad.GuardarAsync(cancelacion);
        return OrganizacionDto.Desde(organizacion);
    }

    public async Task<OrganizacionDto> ModificarAsync(Guid id, SolicitudModificarOrganizacion solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesAdministrar);
        var organizacion = await CargarAsync(id, cancelacion);
        await ExigirNombreLibreAsync(solicitud.Nombre!, excluir: id, cancelacion);

        var anterior = Instantanea(organizacion);
        repositorio.EsperarVersion(organizacion, solicitud.Version);
        organizacion.Modificar(solicitud.Nombre!, solicitud.Tipo!, solicitud.IdentificacionLegal, solicitud.ZonaHoraria, solicitud.ContactoAdministrativo, reloj.UtcNow);
        planoSaas.OperarEnOrganizacion(organizacion.Id);
        auditoria.Registrar(new EntradaAuditoria("organizacion.modificar", TipoObjeto, organizacion.Id, ResultadoAuditoria.Exitoso, anterior, Instantanea(organizacion)));

        await unidad.GuardarAsync(cancelacion);
        return OrganizacionDto.Desde(organizacion);
    }

    public Task<OrganizacionDto> InactivarAsync(Guid id, CancellationToken cancelacion) =>
        CambiarEstadoAsync(id, "organizacion.inactivar", o => o.Inactivar(reloj.UtcNow), cancelacion);

    public Task<OrganizacionDto> ReactivarAsync(Guid id, CancellationToken cancelacion) =>
        CambiarEstadoAsync(id, "organizacion.reactivar", o => o.Reactivar(reloj.UtcNow), cancelacion);

    private async Task<OrganizacionDto> CambiarEstadoAsync(Guid id, string accion, Action<Organizacion> cambio, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesAdministrar);
        var organizacion = await CargarAsync(id, cancelacion);

        var anterior = new { Estado = organizacion.Estado.ToString() };
        cambio(organizacion);
        planoSaas.OperarEnOrganizacion(organizacion.Id);
        auditoria.Registrar(new EntradaAuditoria(accion, TipoObjeto, organizacion.Id, ResultadoAuditoria.Exitoso, anterior,
            new { Estado = organizacion.Estado.ToString(), organizacion.InactivadaEn }));

        await unidad.GuardarAsync(cancelacion);
        return OrganizacionDto.Desde(organizacion);
    }

    private async Task<Organizacion> CargarAsync(Guid id, CancellationToken cancelacion) =>
        await repositorio.ObtenerAsync(id, cancelacion) ?? throw new RecursoNoEncontradoException("la organización");

    private async Task ExigirNombreLibreAsync(string nombre, Guid? excluir, CancellationToken cancelacion)
    {
        if (await repositorio.ExisteNombreAsync(Organizacion.NormalizarNombre(nombre), excluir, cancelacion))
        {
            throw new ConflictoException(CodigoNombreDuplicado, "Ya existe una organización con ese nombre.");
        }
    }

    internal static object Instantanea(Organizacion o) =>
        new { o.Nombre, o.Tipo, o.IdentificacionLegal, o.ZonaHoraria, o.ContactoAdministrativo, Estado = o.Estado.ToString() };
}

/// <summary>
/// HU-009: el administrador de una organización lee y edita la información general de la suya.
/// Cualquier otro identificador responde 404 (CA-01), aunque exista.
/// </summary>
public sealed class ServicioMiOrganizacion(
    IRepositorioOrganizaciones repositorio,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public async Task<OrganizacionDto> ObtenerAsync(Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.OrganizacionConsultar);
        return OrganizacionDto.Desde(await CargarPropiaAsync(id, cancelacion));
    }

    public async Task<OrganizacionDto> ModificarAsync(Guid id, SolicitudModificarMiOrganizacion solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.OrganizacionModificar);
        var organizacion = await CargarPropiaAsync(id, cancelacion);
        if (await repositorio.ExisteNombreAsync(Organizacion.NormalizarNombre(solicitud.Nombre!), id, cancelacion))
        {
            throw new ConflictoException(ServicioOrganizacionesSaas.CodigoNombreDuplicado, "Ya existe una organización con ese nombre.");
        }

        var anterior = ServicioOrganizacionesSaas.Instantanea(organizacion);
        repositorio.EsperarVersion(organizacion, solicitud.Version);
        organizacion.ModificarInformacionGeneral(solicitud.Nombre!, solicitud.IdentificacionLegal, solicitud.ZonaHoraria, solicitud.ContactoAdministrativo, reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("organizacion.modificar", "Organizacion", organizacion.Id, ResultadoAuditoria.Exitoso,
            anterior, ServicioOrganizacionesSaas.Instantanea(organizacion)));

        await unidad.GuardarAsync(cancelacion);
        return OrganizacionDto.Desde(organizacion);
    }

    private async Task<Organizacion> CargarPropiaAsync(Guid id, CancellationToken cancelacion)
    {
        if (id != contexto.OrganizacionId) throw new RecursoNoEncontradoException("la organización");
        return await repositorio.ObtenerAsync(id, cancelacion) ?? throw new RecursoNoEncontradoException("la organización");
    }
}

internal static class ReglasOrganizacion
{
    public static void Aplicar<T>(
        AbstractValidator<T> v, Func<T, string?> nombre, Func<T, string?> identificacion, Func<T, string?> zona, Func<T, string?> contacto)
    {
        v.RuleFor(x => nombre(x)).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio.")
            .MaximumLength(Organizacion.LargoNombre).OverridePropertyName("nombre");
        v.RuleFor(x => identificacion(x)).MaximumLength(Organizacion.LargoIdentificacion).OverridePropertyName("identificacionLegal");
        v.RuleFor(x => contacto(x)).MaximumLength(Organizacion.LargoContacto).OverridePropertyName("contactoAdministrativo");
        v.RuleFor(x => zona(x)).Must(z => string.IsNullOrWhiteSpace(z) || ZonaHorariaIana.EsValida(z))
            .WithMessage("La zona horaria no es un identificador IANA válido.").OverridePropertyName("zonaHoraria");
    }

    public static void AplicarTipo<T>(AbstractValidator<T> v, Func<T, string?> tipo) =>
        v.RuleFor(x => tipo(x)).Must(TiposOrganizacion.Existe).WithMessage("El tipo de organización no existe.").OverridePropertyName("tipo");
}

internal sealed class ValidadorCrearOrganizacion : AbstractValidator<SolicitudCrearOrganizacion>
{
    public ValidadorCrearOrganizacion()
    {
        ReglasOrganizacion.Aplicar(this, s => s.Nombre, s => s.IdentificacionLegal, s => s.ZonaHoraria, s => s.ContactoAdministrativo);
        ReglasOrganizacion.AplicarTipo(this, s => s.Tipo);
        RuleFor(s => s.Administrador).NotNull().WithMessage("Indica el primer administrador.").OverridePropertyName("administrador");
        RuleFor(s => s.Administrador!.Correo).EmailAddress().NotEmpty().When(s => s.Administrador is not null)
            .WithMessage("El correo del administrador no es válido.").OverridePropertyName("administrador.correo");
        RuleFor(s => s.Administrador!.Nombre).NotEmpty().MaximumLength(200).When(s => s.Administrador is not null)
            .WithMessage("El nombre del administrador es obligatorio.").OverridePropertyName("administrador.nombre");
    }
}

internal sealed class ValidadorModificarOrganizacion : AbstractValidator<SolicitudModificarOrganizacion>
{
    public ValidadorModificarOrganizacion()
    {
        ReglasOrganizacion.Aplicar(this, s => s.Nombre, s => s.IdentificacionLegal, s => s.ZonaHoraria, s => s.ContactoAdministrativo);
        ReglasOrganizacion.AplicarTipo(this, s => s.Tipo);
    }
}

internal sealed class ValidadorModificarMiOrganizacion : AbstractValidator<SolicitudModificarMiOrganizacion>
{
    public ValidadorModificarMiOrganizacion() =>
        ReglasOrganizacion.Aplicar(this, s => s.Nombre, s => s.IdentificacionLegal, s => s.ZonaHoraria, s => s.ContactoAdministrativo);
}

internal sealed class ValidadorConsultaOrganizaciones : AbstractValidator<ConsultaOrganizacionesSaas>
{
    public ValidadorConsultaOrganizaciones()
    {
        RuleFor(c => c.Pagina).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(c => c.Tamano).InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}
