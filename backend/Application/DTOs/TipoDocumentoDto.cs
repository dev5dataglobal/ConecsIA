namespace ConecsIA.Application.DTOs;

public sealed class TipoDocumentoDto
{
    public int IdTipoDocumento { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? PaisEmisor { get; set; }
    public bool Activo { get; set; }
}
