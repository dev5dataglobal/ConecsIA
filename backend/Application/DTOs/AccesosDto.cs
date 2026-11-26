namespace ConecsIA.Application.DTOs;

// ---------- Registro de acceso unificado (ADR-029) ----------

/// <summary>Franja de ingreso: día ISO (1 = lunes … 7 = domingo) y horas locales de la sede.</summary>
public sealed class FranjaHorariaDto
{
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

/// <summary>Vehículo o dispositivo de la persona que se pide autorizar con el registro.</summary>
public sealed class ObjetoAccesoEntrada
{
    /// <summary>Vehiculo | Dispositivo.</summary>
    public string? TipoObjeto { get; set; }
    public int? IdObjeto { get; set; }
}

/// <summary>
/// Creación del registro de acceso. La persona ya existe (se busca o se crea en Personas desde la misma pantalla).
/// Modalidad: Unica | Diaria | Temporal | Recurrente | Permanente; la categoría limita las permitidas.
/// Horario: <see cref="Franjas"/> propias, o <see cref="UsaHorarioBase"/> para heredar el de la sede u organización.
/// </summary>
public sealed class RegistroAccesoEntrada
{
    public int IdPersona { get; set; }
    public int IdCategoriaAcceso { get; set; }
    public int IdSede { get; set; }
    public int IdArea { get; set; }
    /// <summary>Anfitrión: membresía responsable del destino.</summary>
    public int IdMembresiaResponsable { get; set; }
    public string? Motivo { get; set; }
    public string? Observacion { get; set; }
    public string? Modalidad { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    /// <summary>Temporal: hora de inicio y fin de los instantes; Única/Diaria: franja opcional del día.</summary>
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public List<FranjaHorariaDto> Franjas { get; set; } = [];
    public bool UsaHorarioBase { get; set; }
    public List<ObjetoAccesoEntrada> Objetos { get; set; } = [];
}

/// <summary>Aprobar (motivo opcional), rechazar, revocar o cancelar (motivo obligatorio).</summary>
public sealed class DecisionAccesoEntrada
{
    public string? Motivo { get; set; }
}

public sealed class HorarioAccesoEntrada
{
    public List<FranjaHorariaDto> Franjas { get; set; } = [];
    public bool UsaHorarioBase { get; set; }
    public string? Motivo { get; set; }
}

public sealed class MovimientoAccesoEntrada
{
    public int IdPuntoAcceso { get; set; }
    public string? Observacion { get; set; }
}

/// <summary>Resultado de una entrada o salida confirmada (también la réplica idempotente).</summary>
public sealed class MovimientoConfirmadoDto
{
    public int IdAutorizacion { get; set; }
    public int IdVisita { get; set; }
    public long IdMovimiento { get; set; }
    public int IdPermanencia { get; set; }
    public string TipoMovimiento { get; set; } = "";
    public DateTime OcurridoEn { get; set; }
    public long? DuracionSegundos { get; set; }
    /// <summary>Motivos de horario cuando la organización alerta en lugar de bloquear.</summary>
    public List<string> Alertas { get; set; } = [];
    /// <summary>En la salida de la persona: vehículos y dispositivos de su episodio que siguen dentro (aviso, no bloquea; D-10).</summary>
    public List<string> ObjetosDentro { get; set; } = [];
}

/// <summary>Resultado de una entrada o salida confirmada de un vehículo o dispositivo (también la réplica idempotente).</summary>
public sealed class MovimientoObjetoConfirmadoDto
{
    public int IdAutorizacion { get; set; }
    public int IdAutorizacionObjeto { get; set; }
    public string TipoObjeto { get; set; } = "";
    public long IdMovimientoObjeto { get; set; }
    public int IdVisita { get; set; }
    public string TipoMovimiento { get; set; } = "";
    public DateTime OcurridoEn { get; set; }
    public long? DuracionSegundos { get; set; }
    /// <summary>En la salida: la persona responsable ya no está dentro de la sede.</summary>
    public bool PersonaFuera { get; set; }
}

/// <summary>Vehículo o dispositivo con entrada sin salida.</summary>
public sealed class ObjetoDentroDto
{
    public long IdMovimientoEntrada { get; set; }
    public int IdAutorizacion { get; set; }
    public int IdAutorizacionObjeto { get; set; }
    public string TipoObjeto { get; set; } = "";
    public string Objeto { get; set; } = "";
    public int IdPersona { get; set; }
    public string Persona { get; set; } = "";
    public string DocumentoPersona { get; set; } = "";
    /// <summary>La persona responsable sigue dentro de la sede (si no, el objeto quedó sin su responsable).</summary>
    public bool PersonaDentro { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public string PuntoEntrada { get; set; } = "";
    public DateTime EntradaEn { get; set; }
    public long SegundosDentro { get; set; }
    public string ZonaHoraria { get; set; } = "UTC";
}

public sealed class ObjetoAccesoDto
{
    public int IdAutorizacionObjeto { get; set; }
    public string TipoObjeto { get; set; } = "";
    public int IdObjeto { get; set; }
    public string? Objeto { get; set; }
    public string Estado { get; set; } = "";
    public string? MotivoDecision { get; set; }
    public string? DecididoPor { get; set; }
    public DateTime? DecididoEn { get; set; }
    /// <summary>Tiene una entrada sin salida (en cualquier registro de la organización).</summary>
    public bool Dentro { get; set; }
    public DateTime? EntradaEn { get; set; }
}

public sealed class TransicionAccesoDto
{
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = "";
    public string? Actor { get; set; }
    public string? Observacion { get; set; }
    public DateTime OcurridoEn { get; set; }
}

public sealed class RegistroAccesoDto
{
    public int IdAutorizacion { get; set; }
    public int? IdCategoriaAcceso { get; set; }
    public string? Categoria { get; set; }
    public string? FlujoAprobacion { get; set; }
    public int IdPersona { get; set; }
    public string Persona { get; set; } = "";
    public string DocumentoPersona { get; set; } = "";
    public bool PersonaActiva { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public int IdArea { get; set; }
    public string Area { get; set; } = "";
    public int IdMembresiaResponsable { get; set; }
    public string? Anfitrion { get; set; }
    public string? Motivo { get; set; }
    public string? Observacion { get; set; }
    public string Modalidad { get; set; } = "";
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public bool UsaHorarioBase { get; set; }
    public int? CupoIngresos { get; set; }
    public int IngresosConsumidos { get; set; }
    public string Estado { get; set; } = "";
    /// <summary>Derivada: NoAplica | Programada | Vigente | FueraDeHorario | Vencida | Consumida.</summary>
    public string Vigencia { get; set; } = "";
    public string Origen { get; set; } = "";
    public string? CreadoPor { get; set; }
    public DateTime CreadoEn { get; set; }
    public string? DecididoPor { get; set; }
    public DateTime? DecididoEn { get; set; }
    public string? MotivoDecision { get; set; }
    public bool Dentro { get; set; }
    public string ZonaHoraria { get; set; } = "UTC";
    public List<FranjaHorariaDto> Horario { get; set; } = [];
    public List<ObjetoAccesoDto> Objetos { get; set; } = [];
    /// <summary>Solo en el detalle.</summary>
    public List<TransicionAccesoDto>? Historial { get; set; }
    public List<MovimientoHistorialDto>? Movimientos { get; set; }
    /// <summary>Solo en el detalle: motivos por los que no podría ingresar ahora (vacío = habilitado).</summary>
    public List<string>? MotivosIngreso { get; set; }
}

public sealed class PaginaRegistrosAccesoDto
{
    public List<RegistroAccesoDto> Registros { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
}

public sealed class PresenciaAccesoDto
{
    public int IdPermanencia { get; set; }
    public int IdAutorizacion { get; set; }
    public int IdPersona { get; set; }
    public string Persona { get; set; } = "";
    public string DocumentoPersona { get; set; } = "";
    public string? Categoria { get; set; }
    public int IdSede { get; set; }
    public string Sede { get; set; } = "";
    public string Area { get; set; } = "";
    public string? Anfitrion { get; set; }
    public string PuntoEntrada { get; set; } = "";
    public DateTime EntradaEn { get; set; }
    public long SegundosDentro { get; set; }
    public string ZonaHoraria { get; set; } = "UTC";
    /// <summary>Vehículos y dispositivos de este episodio que siguen dentro.</summary>
    public List<string> ObjetosDentro { get; set; } = [];
}

/// <summary>Movimiento del historial: entrada, salida o intento denegado.</summary>
public sealed class MovimientoHistorialDto
{
    public long IdMovimiento { get; set; }
    public int? IdAutorizacion { get; set; }
    public int IdPersona { get; set; }
    public string Persona { get; set; } = "";
    public string DocumentoPersona { get; set; } = "";
    public string Sede { get; set; } = "";
    public string? Area { get; set; }
    public string? Anfitrion { get; set; }
    public string PuntoAcceso { get; set; } = "";
    public string TipoMovimiento { get; set; } = "";
    public string Resultado { get; set; } = "";
    public string? Motivo { get; set; }
    public string? Operador { get; set; }
    public DateTime OcurridoEn { get; set; }
}

public sealed class PaginaHistorialDto
{
    public List<MovimientoHistorialDto> Registros { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int Tamano { get; set; }
}

/// <summary>Evento de la bitácora de un registro de acceso (auditoría inmutable).</summary>
public sealed class EventoBitacoraDto
{
    public long IdAuditoria { get; set; }
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string IdRegistro { get; set; } = "";
    public string Resultado { get; set; } = "";
    public string? Usuario { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? DireccionIp { get; set; }
    public string? AgenteUsuario { get; set; }
    public DateTime OcurridoEn { get; set; }
}

// ---------- Configuración ----------

public sealed class CategoriaAccesoDto
{
    public int IdCategoriaAcceso { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public string FlujoAprobacion { get; set; } = "";
    public string[] ModalidadesPermitidas { get; set; } = [];
    public bool ExigeAnfitrion { get; set; }
    public bool ExigeMotivo { get; set; }
    public bool PermiteVehiculos { get; set; }
    public bool PermiteDispositivos { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
}

public sealed class CategoriaAccesoEntrada
{
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? FlujoAprobacion { get; set; }
    public string[] ModalidadesPermitidas { get; set; } = [];
    public bool ExigeAnfitrion { get; set; }
    public bool ExigeMotivo { get; set; } = true;
    public bool PermiteVehiculos { get; set; } = true;
    public bool PermiteDispositivos { get; set; } = true;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class HorarioBaseEntrada
{
    /// <summary>Nulo = horario de la organización.</summary>
    public int? IdSede { get; set; }
    public List<FranjaHorariaDto> Franjas { get; set; } = [];
}

public sealed class HorarioBaseDto
{
    public int? IdSede { get; set; }
    public string ZonaHoraria { get; set; } = "";
    public List<FranjaHorariaDto> Franjas { get; set; } = [];
}

/// <summary>Parámetros del módulo por organización. Campos nulos = sin cambio.</summary>
public sealed class ConfiguracionAccesoEntrada
{
    /// <summary>Identificador IANA o de Windows (p. ej. America/Bogota).</summary>
    public string? ZonaHoraria { get; set; }
    /// <summary>Bloquear | Alertar (D-51, Propuesta).</summary>
    public string? FueraDeHorario { get; set; }
    public bool? PermitePermanente { get; set; }
}

public sealed class ConfiguracionAccesoDto
{
    public string ZonaHoraria { get; set; } = "";
    public string FueraDeHorario { get; set; } = "Bloquear";
    public bool PermitePermanente { get; set; }
}

public sealed class OpcionesAccesoDto
{
    public List<CategoriaAccesoDto> Categorias { get; set; } = [];
    public List<OpcionSimpleDto> Sedes { get; set; } = [];
    public List<OpcionAreaAccesoDto> Areas { get; set; } = [];
    public List<OpcionSimpleDto> Anfitriones { get; set; } = [];
    public List<OpcionPuntoAccesoDto> PuntosAcceso { get; set; } = [];
    public bool PermitePermanente { get; set; }
    public string FueraDeHorario { get; set; } = "Bloquear";
}

public sealed class OpcionSimpleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public sealed class OpcionAreaAccesoDto
{
    public int Id { get; set; }
    public int IdSede { get; set; }
    public string Nombre { get; set; } = "";
    /// <summary>Membresías designadas como responsables activas; vacío = cualquier usuario activo puede ser anfitrión.</summary>
    public List<int> Responsables { get; set; } = [];
}

public sealed class OpcionPuntoAccesoDto
{
    public int Id { get; set; }
    public int IdSede { get; set; }
    public string Nombre { get; set; } = "";
    public string ModoAcceso { get; set; } = "";
}
