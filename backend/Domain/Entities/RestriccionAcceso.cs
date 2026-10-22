namespace ConecsIA.Domain.Entities;

public class RestriccionAcceso : OrganizacionEntity { public int IdRestriccion { get; set; } public int IdPersona { get; set; } public int? IdSede { get; set; } public int? IdZona { get; set; } public string TipoRestriccion { get; set; } = ""; public string? Motivo { get; set; } public DateTime FechaInicio { get; set; } public DateTime? FechaFin { get; set; } public string Estado { get; set; } = "Activa"; public int IdUsuarioRegistra { get; set; } public DateTime CreadoEn { get; set; } public int? IdUsuarioLevanta { get; set; } public DateTime? LevantadaEn { get; set; } }

