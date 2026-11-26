namespace ConecsIA.Domain.Entities;

/// <summary>
/// Responsable (anfitrión) designado para un destino, opcionalmente marcado como autorizador (RF-04, HU-014). Retirar una
/// designación la inactiva sin borrarla: las visitas y registros previos conservan su responsable y la auditoría guarda cada
/// asignación, cambio y retiro.
/// </summary>
public class AreaResponsable : OrganizacionEntity
{
    public int IdArea { get; set; }
    public int IdMembresia { get; set; }
    public bool EsAutorizador { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime AsignadoEn { get; set; }
    public int? IdUsuarioAsigna { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public int? IdUsuarioActualiza { get; set; }
}
