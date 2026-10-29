using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

// ---------- Entradas ----------

/// <summary>
/// Solicitud de autorización de ingreso de una persona existente. Los campos que aplican dependen de la modalidad;
/// el servidor ignora los que no corresponden y valida la coherencia de fechas, horas y días.
/// </summary>
public sealed class AutorizacionAccesoEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la persona.")]
    public int IdPersona { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la sede.")]
    public int IdSede { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el área o destino.")]
    public int IdArea { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el responsable.")]
    public int IdMembresiaResponsable { get; set; }

    /// <summary>Unica | Diaria | Temporal | Recurrente | Permanente.</summary>
    [Required(ErrorMessage = "Seleccione el tipo de autorización.")]
    public string? Modalidad { get; set; }

    /// <summary>Día (Única, Diaria), inicio (Temporal) o inicio del periodo (Recurrente, Permanente).</summary>
    public DateOnly? FechaInicio { get; set; }

    /// <summary>Fin (Temporal) o fin opcional del periodo (Recurrente). No aplica a Única, Diaria ni Permanente.</summary>
    public DateOnly? FechaFin { get; set; }

    /// <summary>Hora de inicio: obligatoria en Temporal y Recurrente; franja opcional en Única y Diaria.</summary>
    public TimeOnly? HoraInicio { get; set; }

    public TimeOnly? HoraFin { get; set; }

    /// <summary>Días ISO (1 = lunes … 7 = domingo). Solo Recurrente.</summary>
    public List<int>? DiasSemana { get; set; }

    /// <summary>Zonas de la sede a las que se restringe el ingreso (vacío = sin restricción por zona).</summary>
    public List<int>? Zonas { get; set; }

    [Required(ErrorMessage = "Indique el motivo de la solicitud.")]
    [StringLength(500, ErrorMessage = "El motivo admite máximo 500 caracteres.")]
    public string? Motivo { get; set; }

    [StringLength(1000, ErrorMessage = "La observación admite máximo 1000 caracteres.")]
    public string? Observacion { get; set; }
}

// ---------- Salidas ----------

public sealed class AutorizacionAccesoDto
{
    public int IdAutorizacion { get; set; }
    public int IdPersona { get; set; }
    public string Persona { get; set; } = "";
    public string DocumentoPersona { get; set; } = "";
    public bool PersonaActiva { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public int IdArea { get; set; }
    public string Area { get; set; } = "";
    public int IdMembresiaResponsable { get; set; }
    public string? Responsable { get; set; }
    public string Modalidad { get; set; } = "";
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public int? CupoIngresos { get; set; }
    public int IngresosConsumidos { get; set; }
    public string? Motivo { get; set; }
    public string? Observacion { get; set; }
    /// <summary>Persistido: Pendiente | Aprobada | Rechazada | Cancelada | Revocada | Vencida | Agotada.</summary>
    public string Estado { get; set; } = "";
    /// <summary>Calculado para las aprobadas: Vigente | Programada | Vencida | Consumida; NoAplica en otros estados.</summary>
    public string Vigencia { get; set; } = "NoAplica";
    public string? SolicitadoPor { get; set; }
    public DateTime SolicitadoEn { get; set; }
    public string? DecididoPor { get; set; }
    public DateTime? DecididoEn { get; set; }
    public string? MotivoDecision { get; set; }
    public List<HorarioAutorizacionDto> Horarios { get; set; } = [];
    public List<OpcionCatalogoDto> Zonas { get; set; } = [];
    public List<TransicionAutorizacionObjetoDto> Historial { get; set; } = [];
    public string ZonaHoraria { get; set; } = "UTC";
}

public sealed class ZonaAutorizacionDto
{
    public int Id { get; set; }
    public int IdSede { get; set; }
    public string Nombre { get; set; } = "";
}

public sealed class OpcionesAutorizacionAccesoDto
{
    public List<OpcionCatalogoDto> Sedes { get; set; } = [];
    public List<OpcionAreaDto> Areas { get; set; } = [];
    public List<ResponsableVisitaDto> Responsables { get; set; } = [];
    public List<ZonaAutorizacionDto> Zonas { get; set; } = [];
    /// <summary>Política de la organización: si admite solicitar la modalidad Permanente (D-18).</summary>
    public bool PermitePermanente { get; set; }
    /// <summary>Política de la organización: toda autorización debe restringirse a una o más zonas.</summary>
    public bool ExigeZonas { get; set; }
    /// <summary>Membresía del usuario autenticado (responsable sugerido).</summary>
    public int? IdMembresiaPropia { get; set; }
}
