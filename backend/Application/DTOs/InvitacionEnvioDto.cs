namespace ConecsIA.Application.DTOs;

public sealed class InvitacionEnvioDto
{
    public int IdEnvio { get; set; }
    public int IdAutorizacion { get; set; }
    public int IdToken { get; set; }
    public int IdUsuarioEnvia { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateTime EnviadoEn { get; set; }
}
