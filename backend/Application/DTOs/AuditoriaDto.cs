namespace ConecsIA.Application.DTOs;

public sealed class AuditoriaDto
{
    public long IdAuditoria { get; set; }
    public int? IdOrganizacion { get; set; }
    public string TipoActor { get; set; } = string.Empty;
    public int? IdUsuario { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string IdRegistro { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? DireccionIp { get; set; }
    public string? IdCorrelacion { get; set; }
    public DateTime OcurridoEn { get; set; }
}
