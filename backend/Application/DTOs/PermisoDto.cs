namespace ConecsIA.Application.DTOs;

public sealed class PermisoDto
{
    public int IdPermiso { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}
