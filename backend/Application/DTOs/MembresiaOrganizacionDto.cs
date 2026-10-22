namespace ConecsIA.Application.DTOs;

public sealed class MembresiaOrganizacionDto
{
    public int IdMembresia { get; set; }
    public int IdUsuario { get; set; }
    public int? IdPersona { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; }
}
