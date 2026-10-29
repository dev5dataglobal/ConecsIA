namespace ConecsIA.Domain.Entities;

/// <summary>Lugar donde se registran entradas y salidas (RF-05). Pertenece a una zona; id_sede se copia de la zona para que las llaves compuestas garanticen la coherencia sede-zona-punto.</summary>
public class PuntoAcceso : OrganizacionEntity { public int IdPuntoAcceso { get; set; } public int IdSede { get; set; } public int IdZona { get; set; } public string Nombre { get; set; } = ""; public string? Descripcion { get; set; } public string ModoAcceso { get; set; } = "Ambos"; public bool Activo { get; set; } = true; public DateTime CreadoEn { get; set; } public int? IdUsuarioCrea { get; set; } public DateTime? ActualizadoEn { get; set; } public int? IdUsuarioActualiza { get; set; } }
