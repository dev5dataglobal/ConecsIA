namespace ConecsIA.Application.DTOs;

public sealed class VehiculoDto
{
    public int IdVehiculo { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string PlacaNormalizada { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Color { get; set; }
    public bool Activo { get; set; }
}
