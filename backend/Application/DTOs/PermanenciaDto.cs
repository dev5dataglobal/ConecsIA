namespace ConecsIA.Application.DTOs;

public sealed class PermanenciaDto
{
    public int IdPermanencia { get; set; }
    public int IdPersona { get; set; }
    public int IdSede { get; set; }
    public int IdVisita { get; set; }
    public long IdMovimientoEntrada { get; set; }
    public long? IdMovimientoSalida { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime AbiertaEn { get; set; }
    public DateTime? CerradaEn { get; set; }
}
