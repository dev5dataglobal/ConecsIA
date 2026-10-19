namespace ConecsIA.SharedKernel.Auditoria;

public enum ResultadoAuditoria
{
    Exitoso,
    Denegado,
}

/// <summary>Evento de negocio a auditar. Actor, organización, instante y traza los completa el registro.</summary>
public sealed record EntradaAuditoria(
    string Accion,
    string TipoObjeto,
    Guid? ObjetoId,
    ResultadoAuditoria Resultado,
    object? ValoresAnteriores = null,
    object? ValoresNuevos = null);

/// <summary>
/// Auditoría append-only (ADR-014). Registrar no guarda por sí mismo: el evento se persiste en la
/// misma transacción que el cambio auditado, al guardar la unidad de trabajo.
/// </summary>
public interface IRegistroAuditoria
{
    void Registrar(EntradaAuditoria entrada);
}
