namespace ConecsIA.Domain.Entities;

public class SolicitudIdempotente : OrganizacionEntity { public string ClaveIdempotencia { get; set; } = ""; public string Comando { get; set; } = ""; public string HashSolicitud { get; set; } = ""; public int CodigoResultado { get; set; } public string? Respuesta { get; set; } public DateTime CreadoEn { get; set; } }

