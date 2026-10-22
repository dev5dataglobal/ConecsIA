namespace ConecsIA.Domain.Entities;

public class TipoDocumento { public int IdTipoDocumento { get; set; } public string Codigo { get; set; } = ""; public string Nombre { get; set; } = ""; public string? PaisEmisor { get; set; } public bool Activo { get; set; } = true; }

