namespace ConecsIA.Application.DTOs;

public sealed class VisitaDto
{
    public int IdVisita { get; set; }
    public int IdPersona { get; set; }
    public int? IdAutorizacion { get; set; }
    public int IdSede { get; set; }
    public int IdArea { get; set; }
    public int IdMembresiaResponsable { get; set; }
    public int IdUsuarioRegistra { get; set; }
    public string? PoliticaAplicada { get; set; }
    public string? Motivo { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTime CreadoEn { get; set; }
}
