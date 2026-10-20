namespace ConecsIA.Domain.Entities;

public class Rol : OrganizacionEntity { public int IdRol { get; set; } public int? IdPlantillaRol { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public bool Activo { get; set; } = true; }

