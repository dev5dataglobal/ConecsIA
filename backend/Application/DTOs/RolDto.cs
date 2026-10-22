namespace ConecsIA.Application.DTOs;

public sealed class RolDto
{
    public int IdRol { get; set; }
    public int? IdPlantillaRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}
