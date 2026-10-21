namespace ConecsIA.Application.DTOs;

public sealed class NotificacionDestinatarioDto
{
    public int IdDestinatario { get; set; }
    public int IdNotificacion { get; set; }
    public int? IdUsuario { get; set; }
    public int? IdPersona { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int Intentos { get; set; }
    public string? UltimoError { get; set; }
    public DateTime? EnviadoEn { get; set; }
    public DateTime? LeidoEn { get; set; }
}
