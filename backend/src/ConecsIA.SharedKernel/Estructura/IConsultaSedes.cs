namespace ConecsIA.SharedKernel.Estructura;

/// <summary>
/// Contrato del módulo Estructura para que otros módulos validen sedes sin referenciarlo (ADR-001).
/// Solo ve las sedes de la organización del contexto (filtro global).
/// </summary>
public interface IConsultaSedes
{
    /// <summary>De los identificadores dados, los que existen en la organización del contexto.</summary>
    Task<IReadOnlySet<Guid>> ExistentesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion);
}
