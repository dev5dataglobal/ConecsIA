namespace ConecsIA.SharedKernel.Persistencia;

/// <summary>Confirma en una sola transacción los cambios del caso de uso y su auditoría.</summary>
public interface IUnidadDeTrabajo
{
    Task GuardarAsync(CancellationToken cancelacion);
}
