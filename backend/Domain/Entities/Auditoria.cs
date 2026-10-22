namespace ConecsIA.Domain.Entities;

public class Auditoria { public long IdAuditoria { get; set; } public int? IdOrganizacion { get; set; } public string TipoActor { get; set; } = ""; public int? IdUsuario { get; set; } public string Accion { get; set; } = ""; public string Entidad { get; set; } = ""; public string IdRegistro { get; set; } = ""; public string Resultado { get; set; } = ""; public string? ValorAnterior { get; set; } public string? ValorNuevo { get; set; } public string? DireccionIp { get; set; } public string? IdCorrelacion { get; set; } public DateTime OcurridoEn { get; set; } }

