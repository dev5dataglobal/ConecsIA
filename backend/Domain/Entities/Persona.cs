namespace ConecsIA.Domain.Entities;

public class Persona : OrganizacionEntity { public int IdPersona { get; set; } public int IdTipoPersona { get; set; } public int IdTipoDocumento { get; set; } public string NumeroDocumento { get; set; } = ""; public string NumeroDocumentoNormalizado { get; set; } = ""; public string Nombres { get; set; } = ""; public string Apellidos { get; set; } = ""; public string? Correo { get; set; } public string? EmpresaProcedencia { get; set; } public Guid? IdArchivoFoto { get; set; } public string? Observacion { get; set; } public bool Activo { get; set; } = true; public DateTime CreadoEn { get; set; } public int IdUsuarioCrea { get; set; } }

