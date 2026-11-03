namespace ConecsIA.Domain.Entities;

/// <summary>Categoría de acceso sugerida por tipo de organización (plano SaaS). Se copia a <see cref="CategoriaAcceso"/> al crear la organización.</summary>
public class PlantillaCategoriaAcceso { public int IdPlantillaCategoriaAcceso { get; set; } public int IdTipoOrganizacion { get; set; } public string Codigo { get; set; } = ""; public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public string FlujoAprobacion { get; set; } = "Seguridad"; public string[] ModalidadesPermitidas { get; set; } = []; public bool ExigeAnfitrion { get; set; } public bool PermiteVehiculos { get; set; } = true; public bool PermiteDispositivos { get; set; } = true; public int Orden { get; set; } public bool Activo { get; set; } = true; }
