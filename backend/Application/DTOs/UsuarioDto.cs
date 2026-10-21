namespace ConecsIA.Application.DTOs;

public sealed class UsuarioDto
{
    public int IdUsuario { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string CorreoNormalizado { get; set; } = string.Empty;
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreadoEn { get; set; }
}
