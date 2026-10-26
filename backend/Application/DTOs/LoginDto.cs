namespace ConecsIA.Application.DTOs;

public sealed class LoginDto
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>Mantiene la sesión al cerrar el navegador (cookie persistente) hasta el vencimiento configurado.</summary>
    public bool Recordarme { get; set; }
}
