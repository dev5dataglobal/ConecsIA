using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

public sealed class UsuarioPerfilDto
{
    public int IdUsuario { get; set; }
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreadoEn { get; set; }
}

public sealed class ActualizarPerfilDto
{
    [Required, EmailAddress, MaxLength(320)]
    public string Correo { get; set; } = string.Empty;
}

public sealed class CambiarContrasenaDto
{
    [Required]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required]
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}
