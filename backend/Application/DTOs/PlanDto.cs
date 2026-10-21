namespace ConecsIA.Application.DTOs;

public sealed class PlanDto
{
    public int IdPlan { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}
