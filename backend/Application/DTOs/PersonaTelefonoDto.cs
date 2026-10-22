namespace ConecsIA.Application.DTOs;

public sealed class PersonaTelefonoDto
{
    public int IdTelefono { get; set; }
    public int IdPersona { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public bool Principal { get; set; }
    public bool Activo { get; set; }
}
