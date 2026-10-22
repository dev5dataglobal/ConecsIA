namespace ConecsIA.Domain.Entities;

public class AlertaTransicion : OrganizacionEntity { public long IdTransicion { get; set; } public int IdAlerta { get; set; } public string EstadoAnterior { get; set; } = ""; public string EstadoNuevo { get; set; } = ""; public int IdUsuarioActor { get; set; } public string? Observacion { get; set; } public DateTime OcurridoEn { get; set; } }

