namespace ConecsIA.Domain.Entities;

public class TokenQr : OrganizacionEntity { public int IdToken { get; set; } public int IdAutorizacion { get; set; } public string HashSha256 { get; set; } = ""; public string Estado { get; set; } = "Activo"; public DateTime EmitidoEn { get; set; } public DateTime? ExpiraEn { get; set; } public DateTime? RevocadoEn { get; set; } }

