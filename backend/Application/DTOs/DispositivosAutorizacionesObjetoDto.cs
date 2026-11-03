using System.ComponentModel.DataAnnotations;

namespace ConecsIA.Application.DTOs;

/*
 * Contratos de dispositivos de personas y de autorizaciones de ingreso de vehículos y dispositivos.
 * Ninguna entrada lleva organización, responsable, solicitante ni aprobador: el servidor los toma del contexto
 * validado y de la identidad autenticada. Propiedades extra enviadas por el cliente se ignoran al deserializar.
 */

// ---------- Tipos de dispositivo ----------

public sealed class TipoDispositivoEntrada
{
    [Required(ErrorMessage = "El nombre es obligatorio."), MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }
}

public sealed class TipoDispositivoDto
{
    public int IdTipoDispositivo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

// ---------- Dispositivos ----------

public sealed class DispositivoEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de dispositivo.")]
    public int IdTipoDispositivo { get; set; }

    /// <summary>Identificador que permite reconocer el equipo (serial, código de inventario o similar).</summary>
    [Required(ErrorMessage = "El identificador es obligatorio."), MaxLength(60)]
    public string CodigoIdentificador { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? Serial { get; set; }

    [MaxLength(60)]
    public string? Marca { get; set; }

    [MaxLength(60)]
    public string? Modelo { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }

    [MaxLength(500)]
    public string? Descripcion { get; set; }
}

public sealed class DispositivoDto
{
    public int IdDispositivo { get; set; }
    public int IdPersona { get; set; }
    public int IdTipoDispositivo { get; set; }
    public string? TipoDispositivo { get; set; }
    public string CodigoIdentificador { get; set; } = string.Empty;
    public string? Serial { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Color { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
}

// ---------- Autorizaciones de objetos ----------

public sealed class AutorizacionObjetoEntrada
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la persona.")]
    public int IdPersona { get; set; }

    /// <summary>Vehiculo | Dispositivo.</summary>
    [Required(ErrorMessage = "Indique si se autoriza un vehículo o un dispositivo.")]
    public string TipoObjeto { get; set; } = string.Empty;

    public int? IdVehiculo { get; set; }
    public int? IdDispositivo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la sede.")]
    public int IdSede { get; set; }

    public int? IdArea { get; set; }

    /// <summary>Autorización de ingreso de la persona a la que se vincula (opcional, solo trazabilidad).</summary>
    public int? IdAutorizacion { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateOnly? FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    public DateOnly? FechaFin { get; set; }

    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }

    [MaxLength(500)]
    public string? Motivo { get; set; }
}

public sealed class DecisionAutorizacionEntrada
{
    /// <summary>Obligatorio para rechazar o revocar.</summary>
    [MaxLength(500)]
    public string? Motivo { get; set; }
}

public sealed class AutorizacionObjetoDto
{
    public int IdAutorizacionObjeto { get; set; }
    public string TipoObjeto { get; set; } = string.Empty;
    public int? IdVehiculo { get; set; }
    public int? IdDispositivo { get; set; }
    /// <summary>Placa o tipo + identificador del dispositivo.</summary>
    public string? Objeto { get; set; }
    public bool ObjetoActivo { get; set; }
    public int IdPersona { get; set; }
    public string? Persona { get; set; }
    public string? DocumentoPersona { get; set; }
    public int IdSede { get; set; }
    public string? Sede { get; set; }
    public int? IdArea { get; set; }
    public string? Area { get; set; }
    public int? IdAutorizacion { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public string? Motivo { get; set; }
    /// <summary>Estado persistido: Pendiente, Aprobada, Rechazada o Revocada.</summary>
    public string Estado { get; set; } = string.Empty;
    /// <summary>Evaluación calculada (no persistida): Vigente, Programada, Vencida o NoAplica.</summary>
    public string Vigencia { get; set; } = string.Empty;
    public string? MotivoDecision { get; set; }
    public string? SolicitadoPor { get; set; }
    public DateTime SolicitadoEn { get; set; }
    public string? DecididoPor { get; set; }
    public DateTime? DecididoEn { get; set; }
    public List<TransicionAutorizacionObjetoDto> Historial { get; set; } = [];
}

public sealed class TransicionAutorizacionObjetoDto
{
    public long IdTransicion { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public string? Actor { get; set; }
    public DateTime OcurridoEn { get; set; }
}

public sealed class OpcionesAutorizacionObjetoDto
{
    public List<OpcionCatalogoDto> Sedes { get; set; } = [];
    public List<OpcionAreaDto> Areas { get; set; } = [];
}

public sealed class OpcionAreaDto
{
    public int Id { get; set; }
    public int IdSede { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
