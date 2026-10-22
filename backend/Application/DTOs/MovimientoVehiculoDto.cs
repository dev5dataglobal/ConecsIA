namespace ConecsIA.Application.DTOs;

public sealed class MovimientoVehiculoDto
{
    public long IdMovimientoVehiculo { get; set; }
    public int IdVehiculo { get; set; }
    public long IdMovimiento { get; set; }
    public int IdVisita { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public DateTime OcurridoEn { get; set; }
}
