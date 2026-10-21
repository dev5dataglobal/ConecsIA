namespace ConecsIA.Application.DTOs;

public sealed class PlantillaNotificacionDto
{
    public int IdPlantilla { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Canal { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
