using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

// Administración de zonas y puntos de acceso (RF-05). La organización nunca viaja en el cuerpo: es la activa.

public sealed class ZonaEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la sede.")]
    public int IdSede { get; set; }

    [Required(ErrorMessage = "Indique el nombre."), StringLength(150, ErrorMessage = "El nombre admite máximo 150 caracteres.")]
    public string Nombre { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción admite máximo 500 caracteres.")]
    public string? Descripcion { get; set; }
}

public sealed class PuntoAccesoEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la zona.")]
    public int IdZona { get; set; }

    [Required(ErrorMessage = "Indique el nombre."), StringLength(150, ErrorMessage = "El nombre admite máximo 150 caracteres.")]
    public string Nombre { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción admite máximo 500 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>Entrada | Salida | Ambos.</summary>
    public string ModoAcceso { get; set; } = "Ambos";
}

public sealed class ZonaAdminDto
{
    public int IdZona { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public bool SedeActiva { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public int PuntosAcceso { get; set; }
    /// <summary>Tiene autorizaciones, movimientos o puntos: no se elimina, solo se inactiva.</summary>
    public bool TieneHistorial { get; set; }
    public DateTime CreadoEn { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public string? ActualizadoPor { get; set; }
}

public sealed class PuntoAccesoAdminDto
{
    public int IdPuntoAcceso { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public int IdZona { get; set; }
    public string Zona { get; set; } = "";
    public bool ZonaActiva { get; set; }
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

public sealed class ZonasAutorizacionEntrada
{
    public List<int> Zonas { get; set; } = [];
}
