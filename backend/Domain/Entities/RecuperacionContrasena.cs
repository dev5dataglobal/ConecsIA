namespace ConecsIA.Domain.Entities;

/// <summary>Solicitud de restablecimiento de contraseña: token de un solo uso (se guarda su hash) con vencimiento.</summary>
public class RecuperacionContrasena
{
    public long IdRecuperacionContrasena { get; set; }
    public int IdUsuario { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime CreadoEn { get; set; }
    public DateTime ExpiraEn { get; set; }
    public DateTime? UsadoEn { get; set; }
}
