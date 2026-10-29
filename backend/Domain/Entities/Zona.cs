namespace ConecsIA.Domain.Entities;

/// <summary>Sector físico de una sede que puede restringirse en una autorización (RF-05). Los puntos de acceso pertenecen a una zona.</summary>
public class Zona : OrganizacionEntity { public int IdZona { get; set; } public int IdSede { get; set; } public int? IdZonaPadre { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public bool Activo { get; set; } = true; public DateTime CreadoEn { get; set; } public int? IdUsuarioCrea { get; set; } public DateTime? ActualizadoEn { get; set; } public int? IdUsuarioActualiza { get; set; } }
