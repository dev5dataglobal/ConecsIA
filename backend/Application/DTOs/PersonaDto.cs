namespace ConecsIA.Application.DTOs;

public sealed class PersonaDto
{
    public int IdPersona { get; set; }
    public int IdTipoPersona { get; set; }
    public int IdTipoDocumento { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NumeroDocumentoNormalizado { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? EmpresaProcedencia { get; set; }
    public Guid? IdArchivoFoto { get; set; }
    public string? Observacion { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
    public int IdUsuarioCrea { get; set; }
}
