namespace ConecsIA.Application.DTOs;

public sealed class OrganizacionModuloDto
{
    public int IdModulo { get; set; }
    public bool Habilitado { get; set; }
    public DateTime CambiadoEn { get; set; }
    public int IdUsuarioCambia { get; set; }
}
