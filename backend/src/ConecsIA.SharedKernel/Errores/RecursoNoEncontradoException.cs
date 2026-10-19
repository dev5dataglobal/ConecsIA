namespace ConecsIA.SharedKernel.Errores;

/// <summary>
/// El recurso no existe o pertenece a otra organización. Ambos casos responden 404 para no
/// revelar la existencia de datos de otro tenant (ADR-020, CA-01).
/// </summary>
public class RecursoNoEncontradoException(string recurso) : Exception($"No se encontró {recurso}.")
{
    public string Recurso { get; } = recurso;
}
