namespace ConecsIA.Application.DTOs;

public sealed class MovimientoAccesoDto
{
    public long IdMovimiento { get; set; }
    public int IdPersona { get; set; }
    public int IdSede { get; set; }
    public int IdZonaPuntoAcceso { get; set; }
    public int? IdVisita { get; set; }
    public int? IdAutorizacion { get; set; }
    public int? IdPermanencia { get; set; }
    public int IdUsuarioRegistra { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string? MotivoDenegacion { get; set; }
    public string Medio { get; set; } = string.Empty;
    public long? IdMovimientoCorrige { get; set; }
    public DateTime OcurridoEn { get; set; }
    public string? Observacion { get; set; }
}
