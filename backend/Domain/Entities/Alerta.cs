namespace ConecsIA.Domain.Entities;

public class Alerta : OrganizacionEntity { public int IdAlerta { get; set; } public int IdSede { get; set; } public string TipoAlerta { get; set; } = ""; public int? IdPersona { get; set; } public int? IdVisita { get; set; } public int? IdAutorizacion { get; set; } public long? IdMovimiento { get; set; } public string ClaveDeduplicacion { get; set; } = ""; public string Descripcion { get; set; } = ""; public string Estado { get; set; } = "Nueva"; public DateTime GeneradaEn { get; set; } }

