namespace ConecsIA.Domain.Entities;

public class Zona : OrganizacionEntity { public int IdZona { get; set; } public int IdSede { get; set; } public int? IdZonaPadre { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public bool EsPuntoAcceso { get; set; } public string? ModoAcceso { get; set; } public bool Activo { get; set; } = true; }

