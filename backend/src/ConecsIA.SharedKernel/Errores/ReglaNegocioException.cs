namespace ConecsIA.SharedKernel.Errores;

/// <summary>
/// Un comando viola una regla de negocio. Se traduce a 422 con un código de motivo estable (ADR-020).
/// No se usa para denegaciones de entrada: esas son un resultado de negocio con 200.
/// </summary>
public class ReglaNegocioException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
