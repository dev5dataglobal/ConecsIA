namespace ConecsIA.Domain.Entities;

public class ZonaEmpresarial
{
    public int IdZonaEmpresarial { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; }
}
