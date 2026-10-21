namespace ConecsIA.Application.DTOs;

public sealed class OrganizacionDto
{
    public int IdOrganizacion { get; set; }
    public int IdTipoOrganizacion { get; set; }
    public int IdPlan { get; set; }
    public Guid? IdArchivoLogo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Identificacion { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; }
}
