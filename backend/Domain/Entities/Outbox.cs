namespace ConecsIA.Domain.Entities;

public class Outbox : OrganizacionEntity { public long IdMensaje { get; set; } public string Tipo { get; set; } = ""; public string Carga { get; set; } = ""; public string Estado { get; set; } = "Pendiente"; public int Intentos { get; set; } public DateTime DisponibleEn { get; set; } public DateTime? ProcesadoEn { get; set; } }

