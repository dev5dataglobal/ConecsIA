namespace ConecsIA.Application.DTOs;

public sealed class ZonaDto
{
    public int IdZona { get; set; }
    public int IdSede { get; set; }
    public int? IdZonaPadre { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsPuntoAcceso { get; set; }
    public string? ModoAcceso { get; set; }
    public bool Activo { get; set; }
}
