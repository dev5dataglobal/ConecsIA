namespace ConecsIA.Application.DTOs;

public sealed class ArchivoDto
{
    public Guid IdArchivo { get; set; }
    public int? IdOrganizacion { get; set; }
    public string ClaveAlmacenamiento { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public int IdUsuarioCarga { get; set; }
    public DateTime CreadoEn { get; set; }
}
