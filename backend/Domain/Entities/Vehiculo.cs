namespace ConecsIA.Domain.Entities;

public class Vehiculo : OrganizacionEntity { public int IdVehiculo { get; set; } public string Placa { get; set; } = ""; public string PlacaNormalizada { get; set; } = ""; public string? Tipo { get; set; } public string? Marca { get; set; } public string? Modelo { get; set; } public string? Color { get; set; } public bool Activo { get; set; } = true; }

