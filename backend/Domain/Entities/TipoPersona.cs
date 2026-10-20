namespace ConecsIA.Domain.Entities;

public class TipoPersona : OrganizacionEntity { public int IdTipoPersona { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public bool Activo { get; set; } = true; }

