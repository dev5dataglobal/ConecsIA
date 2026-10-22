namespace ConecsIA.Domain.Entities;

public class Notificacion : OrganizacionEntity { public int IdNotificacion { get; set; } public int? IdPlantilla { get; set; } public string Evento { get; set; } = ""; public string Titulo { get; set; } = ""; public string Mensaje { get; set; } = ""; public DateTime CreadoEn { get; set; } }

