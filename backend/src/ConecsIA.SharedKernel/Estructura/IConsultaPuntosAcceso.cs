namespace ConecsIA.SharedKernel.Estructura;

public sealed record PuntoAccesoResumen(Guid Id, Guid SedeId, bool EsPuntoAcceso, bool Activa);

/// <summary>
/// Contrato del módulo Estructura (ADR-001, ADR-019) para validar zonas que actúan como puntos de
/// acceso. Solo ve las de la organización del contexto: una ausente es de otra organización (404).
/// </summary>
public interface IConsultaPuntosAcceso
{
    Task<IReadOnlyDictionary<Guid, PuntoAccesoResumen>> ObtenerAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion);
}
