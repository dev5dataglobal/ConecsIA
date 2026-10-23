namespace ConecsIA.Domain.Entities;

/// <summary>Acceso de un rol (de una organización) a un módulo del catálogo. Sin fila ⇒ acceso denegado.</summary>
public class ModuloPermiso : OrganizacionEntity
{
    public int IdModuloPermiso { get; set; }
    public int IdRol { get; set; }
    public int IdAppModulo { get; set; }
    public DateTime AsignadoEn { get; set; }
    public int? IdUsuarioAsigna { get; set; }
}
