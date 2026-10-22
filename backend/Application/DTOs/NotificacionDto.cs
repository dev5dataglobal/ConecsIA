namespace ConecsIA.Application.DTOs;

public sealed class NotificacionDto
{
    public int IdNotificacion { get; set; }
    public int? IdPlantilla { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; }
}
