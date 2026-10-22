namespace ConecsIA.Domain.Entities;

public class PersonaVehiculo : OrganizacionEntity { public int IdPersona { get; set; } public int IdVehiculo { get; set; } public string Relacion { get; set; } = "Responsable"; public bool Activo { get; set; } = true; }

