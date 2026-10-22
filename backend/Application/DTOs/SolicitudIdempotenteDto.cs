namespace ConecsIA.Application.DTOs;

public sealed class SolicitudIdempotenteDto
{
    public string ClaveIdempotencia { get; set; } = string.Empty;
    public string Comando { get; set; } = string.Empty;
    public string HashSolicitud { get; set; } = string.Empty;
    public int CodigoResultado { get; set; }
    public string? Respuesta { get; set; }
    public DateTime CreadoEn { get; set; }
}
