namespace ConecsIA.Application.DTOs;

public sealed class SedeDto
{
    public int IdSede { get; set; }
    public int IdCiudad { get; set; }
    public int? IdZonaEmpresarial { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;
    public int? IdMembresiaResponsable { get; set; }
    public bool Activo { get; set; }
}
