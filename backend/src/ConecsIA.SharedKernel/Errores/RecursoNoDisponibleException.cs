namespace ConecsIA.SharedKernel.Errores;

/// <summary>
/// El recurso existió pero ya no se puede usar (p. ej. invitación vencida o aceptada, HU-016).
/// Se traduce a 410 con código estable y sin detalles que permitan enumerar tokens.
/// </summary>
public class RecursoNoDisponibleException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
