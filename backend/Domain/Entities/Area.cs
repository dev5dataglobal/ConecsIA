namespace ConecsIA.Domain.Entities;

public class Area : OrganizacionEntity { public int IdArea { get; set; } public int IdSede { get; set; } public int? IdAreaPadre { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public bool Activo { get; set; } = true; }

