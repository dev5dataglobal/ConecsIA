namespace ConecsIA.Application.DTOs;

public sealed class AutorizacionHorarioDto
{
    public int IdAutorizacionHorario { get; set; }
    public int IdAutorizacion { get; set; }
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}
