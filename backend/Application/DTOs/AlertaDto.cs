namespace ConecsIA.Application.DTOs;

public sealed class AlertaDto
{
    public int IdAlerta { get; set; }
    public int IdSede { get; set; }
    public string TipoAlerta { get; set; } = string.Empty;
    public int? IdPersona { get; set; }
    public int? IdVisita { get; set; }
    public int? IdAutorizacion { get; set; }
    public long? IdMovimiento { get; set; }
    public string ClaveDeduplicacion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime GeneradaEn { get; set; }
}
