namespace ConecsIA.Domain.Entities;

/// <summary>
/// Sesión renovable de un usuario. El token viaja solo en una cookie HttpOnly; aquí se guarda su hash (SHA-256).
/// Cada renovación revoca la fila y crea otra (rotación) conservando el vencimiento absoluto del inicio de sesión.
/// </summary>
public class SesionRefresco
{
    public long IdSesionRefresco { get; set; }
    public int IdUsuario { get; set; }
    public string TokenHash { get; set; } = "";
    /// <summary>true = «Recordarme»: la cookie persiste al cerrar el navegador.</summary>
    public bool Recordar { get; set; }
    /// <summary>Sello de seguridad del usuario al emitirla: si cambia (contraseña, restablecimiento) la sesión deja de renovarse.</summary>
    public string SelloSeguridad { get; set; } = "";
    public DateTime CreadoEn { get; set; }
    public DateTime ExpiraEn { get; set; }
    public DateTime? RevocadoEn { get; set; }
    public long? IdReemplazo { get; set; }
}
