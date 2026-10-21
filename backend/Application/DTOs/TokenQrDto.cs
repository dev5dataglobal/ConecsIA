namespace ConecsIA.Application.DTOs;

public sealed class TokenQrDto
{
    public int IdToken { get; set; }
    public int IdAutorizacion { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime EmitidoEn { get; set; }
    public DateTime? ExpiraEn { get; set; }
    public DateTime? RevocadoEn { get; set; }
}
