namespace ConecsIA.Application.DTOs;

public sealed class DispositivoDto
{
    public int IdDispositivo { get; set; }
    public int IdPersonaResponsable { get; set; }
    public int IdTipoDispositivo { get; set; }
    public string CodigoIdentificador { get; set; } = string.Empty;
    public string? Serial { get; set; }
    public string? SerialNormalizado { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Color { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}
