namespace ConecsIA.Domain.Entities;

public class PlantillaNotificacion : OrganizacionEntity { public int IdPlantilla { get; set; } public string Evento { get; set; } = ""; public string Canal { get; set; } = ""; public string Asunto { get; set; } = ""; public string Cuerpo { get; set; } = ""; public bool Activo { get; set; } = true; }

