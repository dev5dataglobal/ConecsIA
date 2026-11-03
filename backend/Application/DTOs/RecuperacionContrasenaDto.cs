using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

public sealed class SolicitarRecuperacionDto
{
    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "El correo no es válido."), MaxLength(320)]
    public string Correo { get; set; } = string.Empty;
}

public sealed class RestablecerContrasenaDto
{
    [Required(ErrorMessage = "El enlace de recuperación no es válido."), MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria."), MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres."), MaxLength(128)]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}
