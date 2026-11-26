namespace ConecsIA.Application.DTOs;

/// <summary>Responsable o autorizador designado para un destino.</summary>
public sealed class AreaResponsableDto
{
    public int IdArea { get; set; }
    public int IdMembresia { get; set; }
    public string Usuario { get; set; } = "";
    public bool UsuarioActivo { get; set; }
    public bool EsAutorizador { get; set; }
    public bool Activo { get; set; }
    public DateTime AsignadoEn { get; set; }
    public string? AsignadoPor { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public string? ActualizadoPor { get; set; }
}

/// <summary>Asignar (o reactivar) un responsable de destino; <see cref="EsAutorizador"/> lo marca además como autorizador.</summary>
public sealed class AsignarResponsableAreaEntrada
{
    public int IdMembresia { get; set; }
    public bool EsAutorizador { get; set; }
}

/// <summary>Usuario de la organización que puede designarse como responsable.</summary>
public sealed class MiembroOrganizacionDto
{
    public int IdMembresia { get; set; }
    public string Usuario { get; set; } = "";
}
