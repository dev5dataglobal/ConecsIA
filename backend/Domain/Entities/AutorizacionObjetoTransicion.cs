namespace ConecsIA.Domain.Entities;

/// <summary>Evento inmutable del ciclo de una <see cref="AutorizacionObjeto"/> (la base rechaza UPDATE y DELETE).</summary>
public class AutorizacionObjetoTransicion : OrganizacionEntity { public long IdTransicion { get; set; } public int IdAutorizacionObjeto { get; set; } public int IdSede { get; set; } public string? EstadoAnterior { get; set; } public string EstadoNuevo { get; set; } = ""; public string? Motivo { get; set; } public int IdUsuarioActor { get; set; } public DateTime OcurridoEn { get; set; } }
