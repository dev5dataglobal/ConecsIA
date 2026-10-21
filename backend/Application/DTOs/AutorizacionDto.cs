namespace ConecsIA.Application.DTOs;

public sealed class AutorizacionDto
{
    public int IdAutorizacion { get; set; }
    public int IdPersona { get; set; }
    public int IdSede { get; set; }
    public int IdArea { get; set; }
    public int IdMembresiaResponsable { get; set; }
    public int IdUsuarioCrea { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public int? CupoIngresos { get; set; }
    public int IngresosConsumidos { get; set; }
    public string? Motivo { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTime CreadoEn { get; set; }
}
