namespace ConecsIA.Domain.Entities;

public class ZonaEmpresarialOrganizacion : OrganizacionEntity
{
    public int IdZonaEmpresarial { get; set; }
    public bool EsAdministradora { get; set; }
    public bool Activa { get; set; } = true;
}
