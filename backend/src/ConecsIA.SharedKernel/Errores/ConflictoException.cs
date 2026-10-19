namespace ConecsIA.SharedKernel.Errores;

/// <summary>El comando choca con el estado actual (p. ej. nombre ya usado). Se traduce a 409 con código estable.</summary>
public class ConflictoException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
