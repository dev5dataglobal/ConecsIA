namespace ConecsIA.Domain.Entities;

/// <summary>
/// Entrada o salida de un vehículo o dispositivo por un punto de acceso (RF-16, RF-17; ADR-030). Solo inserción: una salida
/// referencia la entrada que cierra (<see cref="IdMovimientoEntrada"/>); el objeto está «dentro» mientras su última entrada no
/// tenga salida. Se vincula al registro de acceso, a la decisión propia del objeto, al episodio (visita) de la persona
/// responsable y, en la entrada, al movimiento de entrada de esa persona.
/// </summary>
public class MovimientoObjeto : OrganizacionEntity
{
    public long IdMovimientoObjeto { get; set; }
    public string TipoObjeto { get; set; } = "";
    public int? IdVehiculo { get; set; }
    public int? IdDispositivo { get; set; }
    public int IdAutorizacionObjeto { get; set; }
    public int IdAutorizacion { get; set; }
    public int IdVisita { get; set; }
    public int IdPersonaResponsable { get; set; }
    public int IdSede { get; set; }
    public int IdPuntoAcceso { get; set; }
    /// <summary>Movimiento de la persona al que acompaña (su entrada a la sede); nulo en las salidas.</summary>
    public long? IdMovimientoAcceso { get; set; }
    /// <summary>Entrada que cierra esta salida; nulo en las entradas.</summary>
    public long? IdMovimientoEntrada { get; set; }
    public int IdUsuarioRegistra { get; set; }
    public string TipoMovimiento { get; set; } = "Entrada";
    public DateTime OcurridoEn { get; set; }
    public string? Observacion { get; set; }
}
