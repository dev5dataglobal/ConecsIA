namespace ConecsIA.Application.DTOs;

public sealed class PersonaVehiculoDto
{
    public int IdPersona { get; set; }
    public int IdVehiculo { get; set; }
    public string Relacion { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
