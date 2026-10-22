namespace ConecsIA.Application.DTOs;

public sealed class ParametroOrganizacionDto
{
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public DateTime ActualizadoEn { get; set; }
}
