namespace ConecsIA.Domain.Entities;

public class Ciudad { public int IdCiudad { get; set; } public string? CodigoDane { get; set; } public string Nombre { get; set; } = ""; public string? Departamento { get; set; } public string Pais { get; set; } = ""; public string ZonaHoraria { get; set; } = "America/Bogota"; public bool Activo { get; set; } = true; }

