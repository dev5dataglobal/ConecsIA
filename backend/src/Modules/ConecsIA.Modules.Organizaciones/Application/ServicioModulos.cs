using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Modulos;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Organizaciones.Application;

public sealed record SolicitudModulos(IReadOnlyList<string>? Habilitados);

public sealed record ModuloOrganizacionDto(string Modulo, string Nombre, bool Habilitado, DateTimeOffset? CambiadoEn, Guid? CambiadoPor);

public interface IRepositorioModulos
{
    /// <summary>Filas de la organización del contexto (filtro global).</summary>
    Task<IReadOnlyList<ModuloOrganizacion>> DeLaOrganizacionAsync(CancellationToken cancelacion);

    void Agregar(ModuloOrganizacion modulo);
}

/// <summary>
/// HU-011: habilitación de módulos desde el plano SaaS. El plan (contenido y precios) queda
/// pendiente de D-16; aquí solo se registran las banderas de módulo con fecha y actor.
/// </summary>
public sealed class ServicioModulos(
    IRepositorioOrganizaciones organizaciones,
    IRepositorioModulos repositorio,
    IContextoSolicitud contexto,
    IContextoPlanoSaas planoSaas,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public IReadOnlyList<ModuloOpcional> Catalogo()
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesConsultar);
        return ModulosOpcionales.Todos;
    }

    public async Task<IReadOnlyList<ModuloOrganizacionDto>> ObtenerAsync(Guid organizacionId, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesConsultar);
        await ExigirOrganizacionAsync(organizacionId, cancelacion);
        planoSaas.OperarEnOrganizacion(organizacionId);
        return Proyectar(await repositorio.DeLaOrganizacionAsync(cancelacion));
    }

    public async Task<IReadOnlyList<ModuloOrganizacionDto>> CambiarAsync(Guid organizacionId, SolicitudModulos solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermisoSaas(contexto, PermisosSaas.OrganizacionesAdministrar);
        await ExigirOrganizacionAsync(organizacionId, cancelacion);
        planoSaas.OperarEnOrganizacion(organizacionId);

        var actuales = await repositorio.DeLaOrganizacionAsync(cancelacion);
        var anterior = actuales.Where(m => m.Habilitado).Select(m => m.Modulo).Order().ToList();
        var pedidos = solicitud.Habilitados!.ToHashSet(StringComparer.Ordinal);
        var ahora = reloj.UtcNow;
        foreach (var modulo in ModulosOpcionales.Todos)
        {
            var fila = actuales.SingleOrDefault(m => m.Modulo == modulo.Clave);
            var habilitar = pedidos.Contains(modulo.Clave);
            if (fila is null)
            {
                if (habilitar) repositorio.Agregar(ModuloOrganizacion.Crear(organizacionId, modulo.Clave, true, contexto.UsuarioId, ahora));
            }
            else
            {
                fila.Cambiar(habilitar, contexto.UsuarioId, ahora);
            }
        }

        auditoria.Registrar(new EntradaAuditoria("organizacion.modulos", "Organizacion", organizacionId, ResultadoAuditoria.Exitoso,
            new { Habilitados = anterior }, new { Habilitados = pedidos.Order().ToList() }));
        await unidad.GuardarAsync(cancelacion);
        return Proyectar(await repositorio.DeLaOrganizacionAsync(cancelacion));
    }

    private async Task ExigirOrganizacionAsync(Guid id, CancellationToken cancelacion)
    {
        if (await organizaciones.ObtenerAsync(id, cancelacion) is null) throw new RecursoNoEncontradoException("la organización");
    }

    private static List<ModuloOrganizacionDto> Proyectar(IReadOnlyList<ModuloOrganizacion> filas) =>
        ModulosOpcionales.Todos.Select(m =>
        {
            var fila = filas.SingleOrDefault(f => f.Modulo == m.Clave);
            return new ModuloOrganizacionDto(m.Clave, m.Nombre, fila?.Habilitado ?? false, fila?.CambiadoEn, fila?.CambiadoPor);
        }).ToList();
}

/// <summary>Guarda de módulo para la organización del contexto (lectura por petición, sin caché).</summary>
public sealed class ModulosHabilitados(IRepositorioModulos repositorio) : IModulosHabilitados
{
    public async Task<IReadOnlyList<string>> HabilitadosAsync(CancellationToken cancelacion) =>
        (await repositorio.DeLaOrganizacionAsync(cancelacion)).Where(m => m.Habilitado).Select(m => m.Modulo).Order().ToList();
}

internal sealed class ValidadorSolicitudModulos : AbstractValidator<SolicitudModulos>
{
    public ValidadorSolicitudModulos()
    {
        RuleFor(s => s.Habilitados).NotNull().WithMessage("Indica los módulos habilitados (puede ser una lista vacía).")
            .OverridePropertyName("habilitados");
        RuleForEach(s => s.Habilitados).Must(ModulosOpcionales.Existe).WithMessage("El módulo {PropertyValue} no existe en el catálogo.")
            .OverridePropertyName("habilitados");
    }
}
