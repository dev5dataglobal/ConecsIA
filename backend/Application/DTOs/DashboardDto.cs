namespace ConecsIA.Application.DTOs;

public sealed class DashboardDto
{
    public DashboardOrganizationDto Organizacion { get; set; } = new();
    public IReadOnlyList<DashboardSedeDto> Sedes { get; set; } = [];
    public DashboardResumenDto Resumen { get; set; } = new();
    public IReadOnlyList<DashboardSerieDto> VisitasPorDia { get; set; } = [];
    public IReadOnlyList<DashboardDistribucionDto> DistribucionPorTipo { get; set; } = [];
    public IReadOnlyList<DashboardPersonaDentroDto> PersonasDentro { get; set; } = [];
    public IReadOnlyList<DashboardActividadDto> ActividadReciente { get; set; } = [];
}

public sealed class DashboardOrganizationDto { public int IdOrganizacion { get; set; } public string Nombre { get; set; } = string.Empty; }
public sealed class DashboardSedeDto { public int IdSede { get; set; } public string Nombre { get; set; } = string.Empty; }
public sealed class DashboardResumenDto { public int PersonasDentro { get; set; } public int Visitas { get; set; } public int AutorizacionesActivas { get; set; } public int AlertasPendientes { get; set; } }
public sealed class DashboardSerieDto { public DateOnly Fecha { get; set; } public int Total { get; set; } }
public sealed class DashboardDistribucionDto { public string Tipo { get; set; } = string.Empty; public int Total { get; set; } }
public sealed class DashboardPersonaDentroDto
{
    public int IdPersona { get; set; }
    public string Persona { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime EntradaEn { get; set; }
    public string? ZonaDestino { get; set; }
    public string Estado { get; set; } = string.Empty;
}
public sealed class DashboardActividadDto { public string Tipo { get; set; } = string.Empty; public string Titulo { get; set; } = string.Empty; public string? Detalle { get; set; } public DateTime OcurrioEn { get; set; } }
