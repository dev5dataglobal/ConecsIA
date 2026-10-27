namespace ConecsIA.Application.DTOs;

public sealed class CiudadDto
{
    public int IdCiudad { get; set; }
    public string? CodigoDane { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Departamento { get; set; }
    public string Pais { get; set; } = string.Empty;
    public string ZonaHoraria { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
