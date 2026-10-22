namespace ConecsIA.Application.DTOs;

public sealed class ModuloDto
{
    public int IdModulo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
