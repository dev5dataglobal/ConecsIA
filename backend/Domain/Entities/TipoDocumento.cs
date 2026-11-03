namespace ConecsIA.Domain.Entities;

/// <summary>Catálogo global de tipos de documento. <see cref="PatronValidacion"/>: expresión regular sobre el número normalizado.</summary>
public class TipoDocumento { public int IdTipoDocumento { get; set; } public string Codigo { get; set; } = ""; public string Nombre { get; set; } = ""; public string? PaisEmisor { get; set; } public string? PatronValidacion { get; set; } public bool Activo { get; set; } = true; }
