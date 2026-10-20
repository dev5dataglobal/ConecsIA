namespace ConecsIA.Domain.Entities;

public class PersonaTelefono : OrganizacionEntity { public int IdTelefono { get; set; } public int IdPersona { get; set; } public string Numero { get; set; } = ""; public string? Tipo { get; set; } public bool Principal { get; set; } public bool Activo { get; set; } = true; }

