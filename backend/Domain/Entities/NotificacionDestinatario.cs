namespace ConecsIA.Domain.Entities;

public class NotificacionDestinatario : OrganizacionEntity { public int IdDestinatario { get; set; } public int IdNotificacion { get; set; } public int? IdUsuario { get; set; } public int? IdPersona { get; set; } public string Canal { get; set; } = ""; public string Direccion { get; set; } = ""; public string Estado { get; set; } = "Pendiente"; public int Intentos { get; set; } public string? UltimoError { get; set; } public DateTime? EnviadoEn { get; set; } public DateTime? LeidoEn { get; set; } }

