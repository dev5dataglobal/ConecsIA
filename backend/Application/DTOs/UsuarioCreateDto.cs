namespace ConecsIA.Application.DTOs;

public sealed class UsuarioCreateDto
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
