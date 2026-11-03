namespace ConecsIA.Domain.Entities;

/// <summary>Agrupación física de sedes (parque, zona franca…). Catálogo de plataforma sin vínculo con organizaciones: pertenecen las sedes (ADR-028).</summary>
public class ZonaEmpresarial
{
    public int IdZonaEmpresarial { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadaEn { get; set; }
}
