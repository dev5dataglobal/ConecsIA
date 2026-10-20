namespace ConecsIA.Domain.Entities;

public class Archivo { public Guid IdArchivo { get; set; } public int? IdOrganizacion { get; set; } public string ClaveAlmacenamiento { get; set; } = ""; public string TipoContenido { get; set; } = ""; public long TamanoBytes { get; set; } public string HashSha256 { get; set; } = ""; public int IdUsuarioCarga { get; set; } public DateTime CreadoEn { get; set; } }

