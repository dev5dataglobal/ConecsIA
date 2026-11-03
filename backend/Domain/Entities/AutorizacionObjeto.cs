namespace ConecsIA.Domain.Entities;

/// <summary>
/// Autorización de ingreso de un vehículo o dispositivo. Ciclo propio, separado de la autorización de la persona:
/// aprobarla no autoriza el ingreso de la persona. <see cref="IdAutorizacion"/> la vincula, si se indica, a la
/// autorización de ingreso de la persona para trazabilidad.
/// </summary>
public class AutorizacionObjeto : OrganizacionEntity { public int IdAutorizacionObjeto { get; set; } public string TipoObjeto { get; set; } = ""; public int? IdVehiculo { get; set; } public int? IdDispositivo { get; set; } public int IdPersona { get; set; } public int IdSede { get; set; } public int? IdArea { get; set; } public int? IdAutorizacion { get; set; } public DateOnly FechaInicio { get; set; } public DateOnly FechaFin { get; set; } public TimeOnly? HoraInicio { get; set; } public TimeOnly? HoraFin { get; set; } public string? Motivo { get; set; } public string Estado { get; set; } = "Pendiente"; public string? MotivoDecision { get; set; } public int IdUsuarioSolicita { get; set; } public DateTime SolicitadoEn { get; set; } public int? IdUsuarioDecide { get; set; } public DateTime? DecididoEn { get; set; } public uint Version { get; set; } }
