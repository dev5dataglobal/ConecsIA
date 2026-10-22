namespace ConecsIA.Domain.Entities;

public class MovimientoVehiculo : OrganizacionEntity { public long IdMovimientoVehiculo { get; set; } public int IdVehiculo { get; set; } public long IdMovimiento { get; set; } public int IdVisita { get; set; } public string TipoMovimiento { get; set; } = "Entrada"; public DateTime OcurridoEn { get; set; } }

