namespace ConecsIA.Application.DTOs;

public sealed class ZonaEmpresarialDto
{
    public int IdZonaEmpresarial { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activa { get; set; }
    public DateTime CreadaEn { get; set; }
    public int TotalSedes { get; set; }
}

public sealed class ZonaEmpresarialEntrada
{
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activa { get; set; } = true;
    public int? IdSedeInicial { get; set; }
}
