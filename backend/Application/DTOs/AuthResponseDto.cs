namespace ConecsIA.Application.DTOs;

public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public UsuarioDto Usuario { get; init; } = new();
}
