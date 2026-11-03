namespace ConecsIA.Domain.Entities;

/// <summary>
/// Categoría de acceso configurable por organización (ADR-029): el término visible (Visitante, Invitado, Contratista…),
/// el flujo de aprobación, las modalidades permitidas y qué datos exige el registro de acceso.
/// </summary>
public class CategoriaAcceso : OrganizacionEntity { public int IdCategoriaAcceso { get; set; } public string Codigo { get; set; } = ""; public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public string FlujoAprobacion { get; set; } = "Seguridad"; public string[] ModalidadesPermitidas { get; set; } = []; public bool ExigeAnfitrion { get; set; } public bool ExigeMotivo { get; set; } = true; public bool PermiteVehiculos { get; set; } = true; public bool PermiteDispositivos { get; set; } = true; public int Orden { get; set; } public bool Activo { get; set; } = true; public DateTime CreadoEn { get; set; } }
