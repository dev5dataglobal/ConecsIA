namespace ConecsIA.Domain.Entities;

/// <summary>Instalación de una organización. Puede pertenecer a cero o una zona empresarial (ADR-028): la zona se resuelve siempre desde la sede.</summary>
public class Sede : OrganizacionEntity { public int IdSede { get; set; } public int IdCiudad { get; set; } public int? IdZonaEmpresarial { get; set; } public string Nombre { get; set; } = ""; public string? Direccion { get; set; } public string? Telefono { get; set; } public string? Correo { get; set; } public string ZonaHoraria { get; set; } = "UTC"; public int? IdMembresiaResponsable { get; set; } public bool Activo { get; set; } = true; }

