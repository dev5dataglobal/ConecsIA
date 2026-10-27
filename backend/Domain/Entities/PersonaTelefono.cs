namespace ConecsIA.Domain.Entities;

/// <summary>Teléfono de una persona (1:N). <see cref="Numero"/> se guarda solo con dígitos; el indicativo va aparte.</summary>
public class PersonaTelefono : OrganizacionEntity { public int IdTelefono { get; set; } public int IdPersona { get; set; } public string CodigoPais { get; set; } = "+57"; public string Numero { get; set; } = ""; public string? Tipo { get; set; } public bool Principal { get; set; } public bool Activo { get; set; } = true; }
