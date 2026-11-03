using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

// Administración de puntos de acceso por sede (RF-05, ADR-028). La organización nunca viaja en el cuerpo: es la activa.

public sealed class PuntoAccesoEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la sede.")]
    public int IdSede { get; set; }

    [Required(ErrorMessage = "Indique el nombre."), StringLength(150, ErrorMessage = "El nombre admite máximo 150 caracteres.")]
    public string Nombre { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción admite máximo 500 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>Entrada | Salida | Ambos.</summary>
    public string ModoAcceso { get; set; } = "Ambos";

    /// <summary>Punto común de la zona empresarial de la sede; solo si la sede pertenece a una zona.</summary>
    public bool EsComunZonaEmpresarial { get; set; }
}

public sealed class PuntoAccesoAdminDto
{
    public int IdPuntoAcceso { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public bool SedeActiva { get; set; }
    /// <summary>Zona empresarial de la sede (resuelta desde la sede), si tiene.</summary>
    public string? ZonaEmpresarial { get; set; }
    public bool EsComunZonaEmpresarial { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string ModoAcceso { get; set; } = "Ambos";
    public bool Activo { get; set; }
    /// <summary>Tiene movimientos u operadores asignados: no se elimina, solo se inactiva.</summary>
    public bool TieneHistorial { get; set; }
    public DateTime CreadoEn { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public string? ActualizadoPor { get; set; }
}
