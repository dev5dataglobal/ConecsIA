namespace ConecsIA.Domain.Entities;

public class AreaResponsable : OrganizacionEntity { public int IdArea { get; set; } public int IdMembresia { get; set; } public bool EsAutorizador { get; set; } public bool Activo { get; set; } = true; }

