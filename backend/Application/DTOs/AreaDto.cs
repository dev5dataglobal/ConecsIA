namespace ConecsIA.Application.DTOs;

public sealed class AreaDto
{
    public int IdArea { get; set; }
    public int IdSede { get; set; }
    public int? IdAreaPadre { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsComunZonaEmpresarial { get; set; }
    public bool Activo { get; set; }
}
