using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

// ---------- Entradas ----------

public sealed class VisitaEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la persona.")]
    public int IdPersona { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la sede.")]
    public int IdSede { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el área o destino.")]
    public int IdArea { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el responsable que recibe la visita.")]
    public int IdMembresiaResponsable { get; set; }

    [Required(ErrorMessage = "Indique el motivo de la visita.")]
    [StringLength(500, ErrorMessage = "El motivo admite máximo 500 caracteres.")]
    public string? Motivo { get; set; }

    public DateOnly? FechaEsperada { get; set; }
    public TimeOnly? HoraEsperadaInicio { get; set; }
    public TimeOnly? HoraEsperadaFin { get; set; }

    /// <summary>Autorización de ingreso de la persona que habilita la visita (opcional según la política).</summary>
    public int? IdAutorizacion { get; set; }

    [StringLength(500, ErrorMessage = "La observación admite máximo 500 caracteres.")]
    public string? Observacion { get; set; }
}

public sealed class VincularAutorizacionEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la autorización.")]
    public int IdAutorizacion { get; set; }
}

public sealed class CancelarVisitaEntrada
{
    [Required(ErrorMessage = "Indique el motivo de la cancelación.")]
    [StringLength(500, ErrorMessage = "El motivo admite máximo 500 caracteres.")]
    public string? Motivo { get; set; }
}

public sealed class MovimientoVisitaEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el punto de acceso.")]
    public int IdPuntoAcceso { get; set; }

    [StringLength(500, ErrorMessage = "La observación admite máximo 500 caracteres.")]
    public string? Observacion { get; set; }
}

// ---------- Salidas ----------

public sealed class VisitaListaDto
{
    public int IdVisita { get; set; }
    public int IdPersona { get; set; }
    public string Persona { get; set; } = "";
    public string DocumentoPersona { get; set; } = "";
    public bool PersonaActiva { get; set; }
    /// <summary>Histórico: visita registrada como excepción al tipo de permiso de la persona (campo retirado el 2026-10-28).</summary>
    public bool ExcepcionTipoAcceso { get; set; }
    public string? MotivoExcepcion { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public int IdArea { get; set; }
    public string Area { get; set; } = "";
    public int IdMembresiaResponsable { get; set; }
    public string? Responsable { get; set; }
    public string? Motivo { get; set; }
    public DateOnly? FechaEsperada { get; set; }
    public TimeOnly? HoraEsperadaInicio { get; set; }
    public TimeOnly? HoraEsperadaFin { get; set; }
    public int? IdAutorizacion { get; set; }
    public string? PoliticaAplicada { get; set; }
    /// <summary>Estado persistido de la visita.</summary>
    public string Estado { get; set; } = "";
    public DateTime CreadoEn { get; set; }
    public DateTime? EntradaEn { get; set; }
    public DateTime? SalidaEn { get; set; }
    public string ZonaHoraria { get; set; } = "UTC";
}

public sealed class VisitaDetalleDto
{
    public VisitaListaDto Visita { get; set; } = null!;
    public string? RegistradaPor { get; set; }
    public string? Observacion { get; set; }
    public string? MotivoCierre { get; set; }
    public string? ActualizadaPor { get; set; }
    public DateTime? ActualizadaEn { get; set; }
    public AutorizacionVisitaDto? Autorizacion { get; set; }
    public PermanenciaVisitaDto? Permanencia { get; set; }
    public List<MovimientoVisitaDto> Movimientos { get; set; } = [];
    /// <summary>Eventos de ciclo de vida (registro, vínculo, cancelación…) leídos de la auditoría; nunca se editan.</summary>
    public List<EventoVisitaDto> Historial { get; set; } = [];
    /// <summary>Evaluación del servidor en este instante (no registra nada).</summary>
    public EvaluacionIngresoDto Evaluacion { get; set; } = null!;
}

public sealed class AutorizacionVisitaDto
{
    public int IdAutorizacion { get; set; }
    public string Estado { get; set; } = "";
    public string Modalidad { get; set; } = "";
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public int? CupoIngresos { get; set; }
    public int IngresosConsumidos { get; set; }
    public int IdSede { get; set; }
    public int IdArea { get; set; }
    public List<HorarioAutorizacionDto> Horarios { get; set; } = [];
}

public sealed class HorarioAutorizacionDto
{
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public sealed class PermanenciaVisitaDto
{
    public int IdPermanencia { get; set; }
    public string Estado { get; set; } = "";
    public DateTime AbiertaEn { get; set; }
    public DateTime? CerradaEn { get; set; }
    public long? DuracionSegundos { get; set; }
}

public sealed class MovimientoVisitaDto
{
    public long IdMovimiento { get; set; }
    public string TipoMovimiento { get; set; } = "";
    public string Resultado { get; set; } = "";
    public string PuntoAcceso { get; set; } = "";
    /// <summary>Zona de acceso retirada en la que se registró (solo movimientos anteriores a ADR-028).</summary>
    public string? ZonaHistorica { get; set; }
    public string? Operador { get; set; }
    public DateTime OcurridoEn { get; set; }
    public string? Observacion { get; set; }
}

public sealed class EventoVisitaDto
{
    public long IdEvento { get; set; }
    public string Accion { get; set; } = "";
    public string? Actor { get; set; }
    public DateTime OcurridoEn { get; set; }
    public string? Motivo { get; set; }
}

public sealed class EvaluacionIngresoDto
{
    public bool PuedeIngresar { get; set; }
    public bool PuedeSalir { get; set; }
    /// <summary>Motivos concretos por los que no puede ingresar (vacío si puede).</summary>
    public List<string> Motivos { get; set; } = [];
}

public sealed class IndicadoresVisitasDto
{
    public int Pendientes { get; set; }
    public int Autorizadas { get; set; }
    public int Dentro { get; set; }
    public int Finalizadas { get; set; }
}

public sealed class PaginaVisitasDto
{
    public List<VisitaListaDto> Registros { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
    public IndicadoresVisitasDto Indicadores { get; set; } = new();
}

public sealed class PresenciaVisitaDto
{
    public int IdVisita { get; set; }
    public int IdPermanencia { get; set; }
    public int IdPersona { get; set; }
    public string Persona { get; set; } = "";
    public string DocumentoPersona { get; set; } = "";
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public string Area { get; set; } = "";
    public string? Responsable { get; set; }
    public string PuntoEntrada { get; set; } = "";
    public DateTime EntradaEn { get; set; }
    public long SegundosDentro { get; set; }
    public string ZonaHoraria { get; set; } = "UTC";
}

public sealed class OpcionesVisitaDto
{
    public List<OpcionCatalogoDto> Sedes { get; set; } = [];
    public List<OpcionAreaDto> Areas { get; set; } = [];
    public List<ResponsableVisitaDto> Responsables { get; set; } = [];
    public List<PuntoAccesoVisitaDto> PuntosAcceso { get; set; } = [];
    /// <summary>La organización permite registrar visitas sin autorización previa (parámetro, deshabilitado por defecto).</summary>
    public bool PermiteSinAutorizacion { get; set; }
}

public sealed class ResponsableVisitaDto
{
    public int IdMembresia { get; set; }
    public string Nombre { get; set; } = "";
    /// <summary>Áreas donde está designado como responsable; vacío = sin designación (aplica en áreas sin responsables).</summary>
    public List<int> Areas { get; set; } = [];
}

public sealed class PuntoAccesoVisitaDto
{
    public int Id { get; set; }
    public int IdSede { get; set; }
    public string Nombre { get; set; } = "";
    public string ModoAcceso { get; set; } = "Ambos";
}

public sealed class AutorizacionDisponibleDto
{
    public int IdAutorizacion { get; set; }
    public string Estado { get; set; } = "";
    public string Modalidad { get; set; } = "";
    public int IdSede { get; set; }
    public int IdArea { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
}
