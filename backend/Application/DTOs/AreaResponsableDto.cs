namespace ConecsIA.Application.DTOs;

public sealed class AreaResponsableDto
{
    public int IdArea { get; set; }
    public int IdMembresia { get; set; }
    public bool EsAutorizador { get; set; }
    public bool Activo { get; set; }
}
