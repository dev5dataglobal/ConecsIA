namespace ConecsIA.Application.DTOs;

public sealed class MovimientoDispositivoDto
{
    public long IdMovimientoDispositivo { get; set; }
    public int IdDispositivo { get; set; }
    public int IdSede { get; set; }
    public int IdZonaPuntoAcceso { get; set; }
    public int IdPersonaResponsable { get; set; }
    public int IdVisita { get; set; }
    public int IdUsuarioRegistra { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public DateTime OcurridoEn { get; set; }
    public string? Observacion { get; set; }
}
