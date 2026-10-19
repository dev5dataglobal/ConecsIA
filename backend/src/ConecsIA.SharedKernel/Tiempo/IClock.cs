namespace ConecsIA.SharedKernel.Tiempo;

/// <summary>
/// Única fuente de la hora actual para el código de negocio. Permite probar reglas de vigencia
/// sin depender del reloj del sistema (HU-001). Los instantes se manejan en UTC (ADR-008, propuesto).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
