namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Operación sin usuario sobre una organización explícita (HU-007): procesos de fondo y flujos
/// anónimos verificados por otro medio (p. ej. token de invitación, HU-016). El filtro global y la
/// verificación al guardar siguen activos.
/// </summary>
public interface IContextoProceso
{
    void ComoProceso(Guid organizacionId, IEnumerable<string> permisos);
}
