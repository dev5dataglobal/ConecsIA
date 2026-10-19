namespace ConecsIA.Infrastructure.Persistencia;

/// <summary>
/// Se intentó guardar una entidad de una organización distinta a la del contexto verificado (ADR-004).
/// Se responde como 404 para no revelar datos de otra organización.
/// </summary>
public sealed class EscrituraEntreOrganizacionesException(string entidad)
    : InvalidOperationException($"Escritura de {entidad} fuera de la organización del contexto.");
