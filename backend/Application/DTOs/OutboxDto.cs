namespace ConecsIA.Application.DTOs;

public sealed class OutboxDto
{
    public long IdMensaje { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Carga { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int Intentos { get; set; }
    public DateTime DisponibleEn { get; set; }
    public DateTime? ProcesadoEn { get; set; }
}
