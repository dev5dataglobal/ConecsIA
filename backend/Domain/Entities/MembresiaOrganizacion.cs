namespace ConecsIA.Domain.Entities;

public class MembresiaOrganizacion : OrganizacionEntity { public int IdMembresia { get; set; } public int IdUsuario { get; set; } public int? IdPersona { get; set; } public string Estado { get; set; } = "Invitada"; public DateTime CreadoEn { get; set; } }

