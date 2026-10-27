namespace ConecsIA.Domain.Entities;

public class Organizacion { public int IdOrganizacion { get; set; } public int IdTipoOrganizacion { get; set; } public int IdPlan { get; set; } public Guid? IdArchivoLogo { get; set; } public string Nombre { get; set; } = ""; public string? Identificacion { get; set; } public string? Direccion { get; set; } public string? Telefono { get; set; } public string? Correo { get; set; } public bool TieneSedes { get; set; } public string Estado { get; set; } = "Activa"; public DateTime CreadoEn { get; set; }
}

