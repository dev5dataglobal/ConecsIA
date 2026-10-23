namespace ConecsIA.Domain.Entities;

/// <summary>
/// Módulo funcional de la aplicación (catálogo global del sistema, no por organización).
/// <see cref="Codigo"/> es el identificador estable usado para autorizar; <see cref="Nombre"/> es solo presentación.
/// </summary>
public class AppModulo
{
    public int IdAppModulo { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Ruta { get; set; }
    public string? Icono { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; }
}
