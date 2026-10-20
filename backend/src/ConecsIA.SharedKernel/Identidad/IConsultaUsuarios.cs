namespace ConecsIA.SharedKernel.Identidad;

/// <summary>
/// Contrato del módulo Identidad (ADR-001) para validar usuarios de la organización del contexto
/// sin referenciar el módulo: clave ausente = no es miembro (404); valor false = inactivo (422).
/// </summary>
public interface IConsultaUsuarios
{
    Task<IReadOnlyDictionary<Guid, bool>> ActivosEnOrganizacionAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion);
}
