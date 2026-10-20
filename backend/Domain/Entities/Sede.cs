namespace ConecsIA.Domain.Entities;

public class Sede : OrganizacionEntity { public int IdSede { get; set; } public int IdCiudad { get; set; } public string Nombre { get; set; } = ""; public string? Direccion { get; set; } public string? Telefono { get; set; } public string? Correo { get; set; } public string ZonaHoraria { get; set; } = "UTC"; public int? IdMembresiaResponsable { get; set; } public bool Activo { get; set; } = true; }

