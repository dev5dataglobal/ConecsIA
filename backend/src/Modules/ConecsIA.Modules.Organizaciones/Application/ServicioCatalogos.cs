using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Organizaciones.Application;

public sealed record SolicitudElemento(string? Clave, string? Nombre);

public sealed record SolicitudRenombrar(string? Nombre);

public sealed record ElementoCatalogoDto(Guid Id, string Catalogo, string Clave, string Nombre, string Origen, string Estado)
{
    public static ElementoCatalogoDto Desde(ElementoCatalogo e) =>
        new(e.Id, e.Catalogo, e.Clave, e.Nombre, e.Origen, e.Activo ? "Activo" : "Inactivo");
}

public sealed record ResultadoPlantilla(string Tipo, int Version, int Agregados);

public interface IRepositorioCatalogos
{
    /// <summary>Elementos de la organización del contexto (filtro global).</summary>
    Task<IReadOnlyList<ElementoCatalogo>> DeCatalogoAsync(string catalogo, CancellationToken cancelacion);

    Task<ElementoCatalogo?> ObtenerAsync(Guid id, CancellationToken cancelacion);

    void Agregar(ElementoCatalogo elemento);
}

/// <summary>
/// HU-010: aplica la plantilla del tipo de la organización agregando solo lo que falta (clave
/// natural catálogo + clave). No toca elementos existentes, inactivos ni roles. Dos aplicaciones
/// simultáneas no duplican: el índice único lo impide y la perdedora responde 409.
/// </summary>
public sealed class AplicadorPlantillas(IRepositorioCatalogos repositorio, IRegistroAuditoria auditoria, IClock reloj)
{
    public async Task<ResultadoPlantilla> AplicarAsync(Guid organizacionId, string tipo, CancellationToken cancelacion)
    {
        var agregados = 0;
        foreach (var catalogo in PlantillasOrganizacion.Catalogos)
        {
            var existentes = (await repositorio.DeCatalogoAsync(catalogo, cancelacion)).Select(e => e.Clave).ToHashSet();
            foreach (var elemento in PlantillasOrganizacion.Elementos(tipo).Where(e => e.Catalogo == catalogo && !existentes.Contains(e.Clave)))
            {
                repositorio.Agregar(ElementoCatalogo.Crear(organizacionId, catalogo, elemento.Clave, elemento.Nombre, PlantillasOrganizacion.Origen(tipo), reloj.UtcNow));
                agregados++;
            }
        }

        auditoria.Registrar(new EntradaAuditoria("organizacion.aplicar_plantilla", "Organizacion", organizacionId, ResultadoAuditoria.Exitoso,
            ValoresNuevos: new { Tipo = tipo, PlantillasOrganizacion.Version, Agregados = agregados }));
        return new ResultadoPlantilla(tipo, PlantillasOrganizacion.Version, agregados);
    }
}

public sealed class ServicioPlantillasSaas(
    IRepositorioOrganizaciones organizaciones,
    AplicadorPlantillas aplicador,
    IContextoSolicitud contexto,
    IContextoPlanoSaas planoSaas,
    IUnidadDeTrabajo unidad)
{
    public async Task<ResultadoPlantilla> AplicarAsync(Guid organizacionId, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesAdministrar);
        var organizacion = await organizaciones.ObtenerAsync(organizacionId, cancelacion) ?? throw new RecursoNoEncontradoException("la organización");
        planoSaas.OperarEnOrganizacion(organizacionId);
        var resultado = await aplicador.AplicarAsync(organizacionId, organizacion.Tipo, cancelacion);
        await unidad.GuardarAsync(cancelacion);
        return resultado;
    }
}

/// <summary>HU-010: el administrador consulta y ajusta los catálogos de su organización.</summary>
public sealed class ServicioCatalogos(
    IRepositorioCatalogos repositorio,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string CodigoClaveDuplicada = "CATALOGO_CLAVE_DUPLICADA";

    public async Task<IReadOnlyList<ElementoCatalogoDto>> ListarAsync(string catalogo, CancellationToken cancelacion)
    {
        ExigirCatalogo(catalogo);
        return (await repositorio.DeCatalogoAsync(catalogo, cancelacion)).OrderBy(e => e.Nombre).Select(ElementoCatalogoDto.Desde).ToList();
    }

    public async Task<ElementoCatalogoDto> AgregarAsync(string catalogo, SolicitudElemento solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ConfiguracionModificar);
        ExigirCatalogo(catalogo);
        var clave = solicitud.Clave!.Trim().ToLowerInvariant();
        if ((await repositorio.DeCatalogoAsync(catalogo, cancelacion)).Any(e => e.Clave == clave))
        {
            throw new ConflictoException(CodigoClaveDuplicada, "Ya existe un elemento con esa clave en el catálogo.");
        }

        var elemento = ElementoCatalogo.Crear(contexto.OrganizacionId!.Value, catalogo, clave, solicitud.Nombre!, ElementoCatalogo.OrigenPersonalizado, reloj.UtcNow);
        repositorio.Agregar(elemento);
        auditoria.Registrar(new EntradaAuditoria("catalogo.agregar", "ElementoCatalogo", elemento.Id, ResultadoAuditoria.Exitoso,
            ValoresNuevos: new { catalogo, elemento.Clave, elemento.Nombre }));
        await unidad.GuardarAsync(cancelacion);
        return ElementoCatalogoDto.Desde(elemento);
    }

    public async Task<ElementoCatalogoDto> RenombrarAsync(string catalogo, Guid id, SolicitudRenombrar solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ConfiguracionModificar);
        var elemento = await CargarAsync(catalogo, id, cancelacion);
        var anterior = new { elemento.Nombre };
        elemento.Renombrar(solicitud.Nombre!);
        auditoria.Registrar(new EntradaAuditoria("catalogo.renombrar", "ElementoCatalogo", elemento.Id, ResultadoAuditoria.Exitoso, anterior, new { elemento.Nombre }));
        await unidad.GuardarAsync(cancelacion);
        return ElementoCatalogoDto.Desde(elemento);
    }

    public async Task<ElementoCatalogoDto> InactivarAsync(string catalogo, Guid id, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ConfiguracionModificar);
        var elemento = await CargarAsync(catalogo, id, cancelacion);
        elemento.Inactivar();
        auditoria.Registrar(new EntradaAuditoria("catalogo.inactivar", "ElementoCatalogo", elemento.Id, ResultadoAuditoria.Exitoso,
            new { Estado = "Activo" }, new { Estado = "Inactivo" }));
        await unidad.GuardarAsync(cancelacion);
        return ElementoCatalogoDto.Desde(elemento);
    }

    private async Task<ElementoCatalogo> CargarAsync(string catalogo, Guid id, CancellationToken cancelacion)
    {
        ExigirCatalogo(catalogo);
        var elemento = await repositorio.ObtenerAsync(id, cancelacion);
        return elemento is { } e && e.Catalogo == catalogo ? e : throw new RecursoNoEncontradoException("el elemento");
    }

    private static void ExigirCatalogo(string catalogo)
    {
        if (!PlantillasOrganizacion.Catalogos.Contains(catalogo)) throw new RecursoNoEncontradoException("el catálogo");
    }
}

internal sealed class ValidadorElemento : AbstractValidator<SolicitudElemento>
{
    public ValidadorElemento()
    {
        RuleFor(s => s.Clave).NotEmpty().MaximumLength(ElementoCatalogo.LargoClave).Matches("^[a-zA-Z0-9-]+$")
            .WithMessage("La clave admite letras, números y guiones.").OverridePropertyName("clave");
        RuleFor(s => s.Nombre).NotEmpty().MaximumLength(ElementoCatalogo.LargoNombre).WithMessage("El nombre es obligatorio.").OverridePropertyName("nombre");
    }
}

internal sealed class ValidadorRenombrar : AbstractValidator<SolicitudRenombrar>
{
    public ValidadorRenombrar() =>
        RuleFor(s => s.Nombre).NotEmpty().MaximumLength(ElementoCatalogo.LargoNombre).WithMessage("El nombre es obligatorio.").OverridePropertyName("nombre");
}
