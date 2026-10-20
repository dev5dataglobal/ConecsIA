using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Organizaciones.Application;

public sealed record SolicitudVocabulario(IReadOnlyDictionary<string, Termino>? Terminos, uint Version);

public sealed record VocabularioDto(IReadOnlyList<string> Claves, IReadOnlyDictionary<string, Termino> Terminos, uint Version);

public interface IRepositorioVocabulario
{
    /// <summary>El de la organización del contexto (filtro global), o null si nunca se personalizó.</summary>
    Task<VocabularioOrganizacion?> ObtenerAsync(CancellationToken cancelacion);

    void Agregar(VocabularioOrganizacion vocabulario);

    void EsperarVersion(VocabularioOrganizacion vocabulario, uint version);
}

/// <summary>HU-012: todo miembro lee el vocabulario de su organización; modificarlo exige permiso.</summary>
public sealed class ServicioVocabulario(
    IRepositorioVocabulario repositorio,
    IContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public async Task<VocabularioDto> ObtenerAsync(CancellationToken cancelacion) =>
        Dto(await repositorio.ObtenerAsync(cancelacion));

    public async Task<VocabularioDto> ReemplazarAsync(SolicitudVocabulario solicitud, CancellationToken cancelacion)
    {
        AccesoDenegadoException.ExigirPermiso(contexto, Permisos.ConfiguracionModificar);
        var vocabulario = await repositorio.ObtenerAsync(cancelacion);
        if (vocabulario is null)
        {
            // La versión 0 indica que el cliente leyó el vocabulario vacío.
            if (solicitud.Version != 0) throw new SharedKernel.Errores.ConflictoException("CONFLICTO_CONCURRENCIA", "El vocabulario cambió desde que fue leído.");
            vocabulario = VocabularioOrganizacion.Crear(contexto.OrganizacionId!.Value, reloj.UtcNow);
            repositorio.Agregar(vocabulario);
        }
        else
        {
            repositorio.EsperarVersion(vocabulario, solicitud.Version);
        }

        var anterior = new { Terminos = vocabulario.Terminos.ToDictionary() };
        vocabulario.Reemplazar(solicitud.Terminos!, reloj.UtcNow);
        auditoria.Registrar(new EntradaAuditoria("vocabulario.modificar", "Vocabulario", vocabulario.Id, ResultadoAuditoria.Exitoso,
            anterior, new { vocabulario.Terminos }));

        await unidad.GuardarAsync(cancelacion);
        return Dto(vocabulario);
    }

    private static VocabularioDto Dto(VocabularioOrganizacion? v) =>
        new(VocabularioOrganizacion.Claves, v?.Terminos ?? new Dictionary<string, Termino>(), v?.Version ?? 0);
}

internal sealed class ValidadorVocabulario : AbstractValidator<SolicitudVocabulario>
{
    public ValidadorVocabulario()
    {
        RuleFor(s => s.Terminos).NotNull().WithMessage("Indica los términos (puede ser un objeto vacío).").OverridePropertyName("terminos");
        RuleForEach(s => s.Terminos)
            .Must(t => VocabularioOrganizacion.Claves.Contains(t.Key))
            .WithMessage("La clave de vocabulario no existe en el catálogo.")
            .Must(t => t.Value is not null
                       && VocabularioOrganizacion.EsEtiquetaValida(t.Value.Singular)
                       && VocabularioOrganizacion.EsEtiquetaValida(t.Value.Plural))
            .WithMessage($"Cada término necesita singular y plural de texto plano, de hasta {VocabularioOrganizacion.LargoMaximo} caracteres.")
            .OverridePropertyName("terminos");
    }
}
