namespace ConecsIA.Domain.Entities;

/// <summary>Lugar donde se registran entradas y salidas (RF-05). Pertenece siempre a una sede (y a su organización). Si la sede está en una zona empresarial puede marcarse común de la zona (ADR-028).</summary>
public class PuntoAcceso : OrganizacionEntity { public int IdPuntoAcceso { get; set; } public int IdSede { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public string ModoAcceso { get; set; } = "Ambos"; public bool EsComunZonaEmpresarial { get; set; } public bool Activo { get; set; } = true; public DateTime CreadoEn { get; set; } public int? IdUsuarioCrea { get; set; } public DateTime? ActualizadoEn { get; set; } public int? IdUsuarioActualiza { get; set; } }
