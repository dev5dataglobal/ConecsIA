namespace ConecsIA.Application.DTOs;

public sealed class AlertaTransicionDto
{
    public long IdTransicion { get; set; }
    public int IdAlerta { get; set; }
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public int IdUsuarioActor { get; set; }
    public string? Observacion { get; set; }
    public DateTime OcurridoEn { get; set; }
}
