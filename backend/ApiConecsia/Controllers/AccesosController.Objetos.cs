using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static ApiConecsia.Services.ReglasAcceso;

namespace ApiConecsia.Controllers;

/// <summary>
/// Entradas y salidas de vehículos y dispositivos en portería (RF-16, RF-17; ADR-030, Propuesta).
/// Entrada: el objeto debe tener su propia decisión Aprobada y vigente, el registro de acceso debe estar Aprobado, el objeto
/// activo y asociado a la persona, y la persona responsable dentro de la sede con este registro: el movimiento se vincula a
/// su episodio (visita) y a su movimiento de entrada. Un mismo vehículo o dispositivo no puede estar dentro dos veces.
/// Salida: cierra la última entrada abierta del objeto; no evalúa vigencia (una revocación posterior no la impide) y no exige
/// que la persona siga dentro. La salida de la persona no cierra sus objetos (D-10): solo se avisa al operador.
/// Permisos (Propuesta): entrada = Control de accesos·Añadir; salida = Control de accesos·Editar; además, acceso al módulo
/// del objeto (Vehículos o Dispositivos). Ambos exigen Idempotency-Key. Los intentos denegados se auditan sin crear movimiento.
/// </summary>
public sealed partial class AccesosController
{
    private const string Vehiculo = "Vehiculo";

    /// <summary>Registra la entrada del vehículo o dispositivo del registro por un punto de acceso de su sede.</summary>
    /// <response code="200">Entrada confirmada (o réplica idempotente).</response>
    /// <response code="409">Entrada denegada: el cuerpo incluye <c>motivos</c>; el intento queda auditado.</response>
    [HttpPost("{id:int}/objetos/{idObjeto:int}/entrada")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Anadir)]
    public Task<IActionResult> RegistrarEntradaObjeto(int id, int idObjeto, [FromBody] MovimientoAccesoEntrada request, [FromHeader(Name = CabeceraIdempotencia)] string? clave, CancellationToken ct) =>
        MovimientoObjetoAsync(id, idObjeto, "Entrada", request, clave, ct);

    /// <summary>Registra la salida del vehículo o dispositivo; cierra su entrada abierta.</summary>
    /// <response code="409">Sin entrada abierta o salida repetida: no se modifica el historial.</response>
    [HttpPost("{id:int}/objetos/{idObjeto:int}/salida")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Editar)]
    public Task<IActionResult> RegistrarSalidaObjeto(int id, int idObjeto, [FromBody] MovimientoAccesoEntrada request, [FromHeader(Name = CabeceraIdempotencia)] string? clave, CancellationToken ct) =>
        MovimientoObjetoAsync(id, idObjeto, "Salida", request, clave, ct);

    /// <summary>Vehículos y dispositivos dentro (entrada sin salida), con su responsable y si este sigue dentro.</summary>
    /// <param name="tipo">Vehiculo | Dispositivo.</param>
    [HttpGet("objetos-dentro")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<ObjetoDentroDto>>> ObjetosDentro([FromQuery] int? idSede = null, [FromQuery] string? tipo = null, CancellationToken ct = default)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var tipos = await TiposObjetoPermitidosAsync(ct);
        if (Limpiar(tipo) is { } t) tipos = tipos.Where(x => x == t).ToList();
        var filas = await (
            from e in EntradasObjetoAbiertas()
            join p in Db.Personas on e.IdPersonaResponsable equals p.IdPersona
            join s in Db.Sedes on e.IdSede equals s.IdSede
            join z in Db.PuntosAcceso on e.IdPuntoAcceso equals z.IdPuntoAcceso
            where tipos.Contains(e.TipoObjeto) && (alcance == null || alcance.Contains(e.IdSede)) && (idSede == null || e.IdSede == idSede)
            orderby e.OcurridoEn
            select new ObjetoDentroDto
            {
                IdMovimientoEntrada = e.IdMovimientoObjeto, IdAutorizacion = e.IdAutorizacion, IdAutorizacionObjeto = e.IdAutorizacionObjeto, TipoObjeto = e.TipoObjeto,
                IdPersona = p.IdPersona, Persona = p.Nombres + " " + p.Apellidos, DocumentoPersona = p.NumeroDocumento,
                PersonaDentro = Db.Permanencias.Any(pe => pe.IdPersona == e.IdPersonaResponsable && pe.IdSede == e.IdSede && pe.Estado == "Abierta"),
                IdSede = s.IdSede, Sede = s.Nombre, PuntoEntrada = z.Nombre, EntradaEn = e.OcurridoEn, ZonaHoraria = s.ZonaHoraria,
            }).ToListAsync(ct);
        var descripciones = await DescribirObjetosAsync(filas.Select(x => x.IdAutorizacionObjeto), ct);
        var ahora = DateTime.UtcNow;
        foreach (var fila in filas)
        {
            fila.Objeto = descripciones.GetValueOrDefault(fila.IdAutorizacionObjeto, "");
            fila.SegundosDentro = Math.Max(0, (long)(ahora - fila.EntradaEn).TotalSeconds);
        }
        return Ok(filas);
    }

    private async Task<IActionResult> MovimientoObjetoAsync(int id, int idObjeto, string tipo, MovimientoAccesoEntrada request, string? clave, CancellationToken ct)
    {
        if (ClaveInvalida(ref clave) is { } faltante) return faltante;
        var observacion = Limpiar(request.Observacion);
        var comando = tipo == "Entrada" ? "objeto.entrada" : "objeto.salida";
        var hash = Hash(comando, idObjeto, request.IdPuntoAcceso, observacion);
        var alcance = await SedesAlcanceAsync(ct);
        var puntosPropios = await PuntosAlcanceAsync(ct);
        var tiposPermitidos = await TiposObjetoPermitidosAsync(ct);

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        await Db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM public.autorizacion_objeto WHERE id_organizacion = {OrganizacionActiva} AND id_autorizacion_objeto = {idObjeto} FOR UPDATE", ct);
        var objeto = await Db.AutorizacionesObjeto.AsNoTracking().SingleOrDefaultAsync(x => x.IdAutorizacionObjeto == idObjeto && x.IdAutorizacion == id, ct);
        if (objeto is null || !EnAlcance(alcance, objeto.IdSede)) return NoEncontrado("El vehículo o dispositivo no pertenece a este registro o no está en tu alcance.");
        if (await ReplicaAsync(clave!, comando, hash, ct) is { } replica) return replica;
        if (!tiposPermitidos.Contains(objeto.TipoObjeto))
            return Prohibido(objeto.TipoObjeto == Vehiculo ? "Tu perfil no tiene acceso al módulo de vehículos." : "Tu perfil no tiene acceso al módulo de dispositivos.");
        // Serializa los movimientos del mismo vehículo o dispositivo aunque esté en varios registros de acceso.
        if (objeto.TipoObjeto == Vehiculo)
            await Db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM public.vehiculo WHERE id_organizacion = {OrganizacionActiva} AND id_vehiculo = {objeto.IdVehiculo} FOR UPDATE", ct);
        else
            await Db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM public.dispositivo WHERE id_organizacion = {OrganizacionActiva} AND id_dispositivo = {objeto.IdDispositivo} FOR UPDATE", ct);

        var punto = await Db.PuntosAcceso.AsNoTracking().SingleOrDefaultAsync(x => x.IdPuntoAcceso == request.IdPuntoAcceso && x.IdSede == objeto.IdSede, ct);
        if (punto is null || !punto.Activo) return Invalido("El punto de acceso no existe en la sede del registro o está inactivo.");
        if (tipo == "Entrada" && punto.ModoAcceso == "Salida") return Invalido("El punto de acceso solo registra salidas.");
        if (tipo == "Salida" && punto.ModoAcceso == "Entrada") return Invalido("El punto de acceso solo registra entradas.");
        if (puntosPropios is not null && !puntosPropios.Contains(punto.IdPuntoAcceso))
            return await DenegarObjetoAsync(transaccion, objeto, punto, tipo, StatusCodes.Status403Forbidden, ["No estás asignado a este punto de acceso."], ct);

        var nombre = objeto.TipoObjeto == Vehiculo ? "El vehículo" : "El dispositivo";
        var abierta = await EntradasObjetoAbiertas()
            .Where(x => objeto.TipoObjeto == Vehiculo ? x.IdVehiculo == objeto.IdVehiculo : x.IdDispositivo == objeto.IdDispositivo)
            .OrderByDescending(x => x.OcurridoEn).FirstOrDefaultAsync(ct);
        var ahora = DateTime.UtcNow;
        MovimientoObjeto movimiento;
        long? duracion = null;
        if (tipo == "Entrada")
        {
            var registro = await Db.Autorizaciones.AsNoTracking().SingleAsync(x => x.IdAutorizacion == id, ct);
            var zona = await Db.Sedes.Where(x => x.IdSede == objeto.IdSede).Select(x => x.ZonaHoraria).SingleAsync(ct);
            var hoy = DateOnly.FromDateTime(ReglasAutorizacion.HoraLocal(ahora, zona));
            var motivos = new List<string>();
            if (abierta is not null) motivos.Add($"{nombre} ya está dentro: registra antes su salida.");
            if (registro.Estado != Aprobada) motivos.Add($"El registro de acceso está {registro.Estado.ToLowerInvariant()}.");
            if (objeto.Estado != Aprobada)
                motivos.Add(objeto.Estado == Pendiente ? $"{nombre} está pendiente de aprobación." : $"{nombre} está {objeto.Estado.ToLowerInvariant()}: no habilita el ingreso.");
            else if (hoy < objeto.FechaInicio || hoy > objeto.FechaFin) motivos.Add($"La autorización de {nombre.ToLowerInvariant()} no está vigente hoy.");
            if (await ValidarObjetoAsync(objeto.TipoObjeto, objeto.IdPersona, objeto.IdVehiculo ?? objeto.IdDispositivo!.Value, ct) is { } invalido) motivos.Add(invalido);
            var permanencia = await (from pe in Db.Permanencias.AsNoTracking()
                                     join v in Db.Visitas on pe.IdVisita equals v.IdVisita
                                     where pe.Estado == "Abierta" && v.IdAutorizacion == id && pe.IdPersona == objeto.IdPersona && pe.IdSede == objeto.IdSede
                                     select pe).FirstOrDefaultAsync(ct);
            if (permanencia is null) motivos.Add("La persona responsable no está dentro con este registro: registra primero su entrada.");
            if (motivos.Count > 0) return await DenegarObjetoAsync(transaccion, objeto, punto, tipo, StatusCodes.Status409Conflict, motivos, ct);
            movimiento = NuevoMovimientoObjeto(objeto, punto, tipo, permanencia!.IdVisita, ahora, observacion);
            movimiento.IdMovimientoAcceso = permanencia.IdMovimientoEntrada;
        }
        else
        {
            if (abierta is null)
                return await DenegarObjetoAsync(transaccion, objeto, punto, tipo, StatusCodes.Status409Conflict,
                    [$"{nombre} no tiene una entrada abierta: no se registra la salida."], ct);
            if (abierta.IdAutorizacionObjeto != objeto.IdAutorizacionObjeto)
                return await DenegarObjetoAsync(transaccion, objeto, punto, tipo, StatusCodes.Status409Conflict,
                    [$"{nombre} ingresó con el registro de acceso #{abierta.IdAutorizacion}: registra su salida desde ese registro."], ct);
            duracion = (long)Math.Floor((ahora - abierta.OcurridoEn).TotalSeconds);
            if (duracion < 0)
                return await DenegarObjetoAsync(transaccion, objeto, punto, tipo, StatusCodes.Status409Conflict,
                    ["Inconsistencia: la entrada registrada es posterior al instante de salida. No se registró la salida."], ct);
            movimiento = NuevoMovimientoObjeto(objeto, punto, tipo, abierta.IdVisita, ahora, observacion);
            movimiento.IdMovimientoEntrada = abierta.IdMovimientoObjeto;
        }

        Db.MovimientosObjeto.Add(movimiento);
        // Índice único: una entrada admite una sola salida.
        if (!await GuardarAsync(ct))
            return await DenegarObjetoAsync(transaccion, objeto, punto, tipo, StatusCodes.Status409Conflict,
                ["Otro registro simultáneo cambió el estado de este objeto. Consulta el estado actual."], ct);
        var personaFuera = tipo == "Salida" &&
            !await Db.Permanencias.AnyAsync(x => x.IdPersona == objeto.IdPersona && x.IdSede == objeto.IdSede && x.Estado == "Abierta", ct);
        Auditar(OrganizacionActiva, tipo == "Entrada" ? "RegistrarEntradaObjeto" : "RegistrarSalidaObjeto", EntidadObjeto, idObjeto.ToString(), null,
            new { objeto.TipoObjeto, objeto.IdVehiculo, objeto.IdDispositivo, movimiento.IdMovimientoObjeto, movimiento.IdVisita, punto.IdPuntoAcceso, DuracionSegundos = duracion, OcurridoEn = ahora });
        return await ConfirmarAsync(transaccion, new MovimientoObjetoConfirmadoDto
        {
            IdAutorizacion = id, IdAutorizacionObjeto = idObjeto, TipoObjeto = objeto.TipoObjeto, IdMovimientoObjeto = movimiento.IdMovimientoObjeto,
            IdVisita = movimiento.IdVisita, TipoMovimiento = tipo, OcurridoEn = ahora, DuracionSegundos = duracion, PersonaFuera = personaFuera,
        }, comando, clave!, hash, ct);
    }

    private MovimientoObjeto NuevoMovimientoObjeto(AutorizacionObjeto objeto, PuntoAcceso punto, string tipo, int idVisita, DateTime ahora, string? observacion) => new()
    {
        IdOrganizacion = OrganizacionActiva, TipoObjeto = objeto.TipoObjeto, IdVehiculo = objeto.IdVehiculo, IdDispositivo = objeto.IdDispositivo,
        IdAutorizacionObjeto = objeto.IdAutorizacionObjeto, IdAutorizacion = objeto.IdAutorizacion!.Value, IdVisita = idVisita,
        IdPersonaResponsable = objeto.IdPersona, IdSede = objeto.IdSede, IdPuntoAcceso = punto.IdPuntoAcceso, IdUsuarioRegistra = User.IdUsuario()!.Value,
        TipoMovimiento = tipo, OcurridoEn = ahora, Observacion = observacion,
    };

    /// <summary>Deshace la operación y audita el intento denegado con sus motivos (sin crear movimiento). Responde problem+json con <c>motivos</c>.</summary>
    private async Task<IActionResult> DenegarObjetoAsync(IDbContextTransaction transaccion, AutorizacionObjeto objeto, PuntoAcceso punto, string tipo, int codigo, List<string> motivos, CancellationToken ct)
    {
        await transaccion.RollbackAsync(ct);
        Db.ChangeTracker.Clear();
        motivos = motivos.Distinct().ToList();
        Auditar(OrganizacionActiva, tipo == "Entrada" ? "RegistrarEntradaObjeto" : "RegistrarSalidaObjeto", EntidadObjeto, objeto.IdAutorizacionObjeto.ToString(), null,
            new { Motivos = motivos, punto.IdPuntoAcceso, objeto.TipoObjeto }, "Denegado");
        await Db.SaveChangesAsync(ct);
        var problema = new ProblemDetails
        {
            Status = codigo,
            Title = codigo == StatusCodes.Status403Forbidden ? "Acceso denegado" : tipo == "Entrada" ? "Ingreso denegado" : "Salida no registrada",
            Detail = string.Join(" ", motivos),
        };
        problema.Extensions["motivos"] = motivos;
        return new ObjectResult(problema) { StatusCode = codigo, ContentTypes = { "application/problem+json" } };
    }

    /// <summary>Entradas de objetos sin salida (filtro global por organización).</summary>
    private IQueryable<MovimientoObjeto> EntradasObjetoAbiertas() =>
        Db.MovimientosObjeto.AsNoTracking().Where(e => e.TipoMovimiento == "Entrada" && !Db.MovimientosObjeto.Any(s => s.IdMovimientoEntrada == e.IdMovimientoObjeto));

    /// <summary>Tipos de objeto cuyos módulos están al alcance del usuario (cualquier acción).</summary>
    private async Task<List<string>> TiposObjetoPermitidosAsync(CancellationToken ct)
    {
        var propios = await AccesosPropiosAsync(ct);
        var tipos = new List<string>();
        if (propios.TryGetValue(CodigosModulo.Vehiculos, out var v) && v.Alguna) tipos.Add(Vehiculo);
        if (propios.TryGetValue(CodigosModulo.Dispositivos, out var d) && d.Alguna) tipos.Add("Dispositivo");
        return tipos;
    }

    /// <summary>Descripción legible de cada objeto autorizado: «placa · marca · color» o «tipo · identificador».</summary>
    private async Task<Dictionary<int, string>> DescribirObjetosAsync(IEnumerable<int> idsAutorizacionObjeto, CancellationToken ct)
    {
        var ids = idsAutorizacionObjeto.Distinct().ToList();
        if (ids.Count == 0) return [];
        return await Db.AutorizacionesObjeto.AsNoTracking().Where(o => ids.Contains(o.IdAutorizacionObjeto))
            .Select(o => new
            {
                o.IdAutorizacionObjeto,
                Texto = o.TipoObjeto == Vehiculo
                    ? Db.Vehiculos.Where(v => v.IdVehiculo == o.IdVehiculo).Select(v => "Vehículo " + v.Placa + (v.Marca != null ? " · " + v.Marca : "") + (v.Color != null ? " · " + v.Color : "")).FirstOrDefault()
                    : Db.Dispositivos.Where(d => d.IdDispositivo == o.IdDispositivo)
                        .Select(d => Db.TiposDispositivo.Where(t => t.IdTipoDispositivo == d.IdTipoDispositivo).Select(t => t.Nombre).FirstOrDefault() + " · " + d.CodigoIdentificador).FirstOrDefault(),
            }).ToDictionaryAsync(x => x.IdAutorizacionObjeto, x => x.Texto ?? "", ct);
    }

    /// <summary>Objetos con entrada abierta en los episodios indicados, descritos, por visita.</summary>
    private async Task<ILookup<int, string>> ObjetosDentroPorVisitaAsync(List<int> idsVisita, CancellationToken ct)
    {
        if (idsVisita.Count == 0) return Array.Empty<int>().ToLookup(x => x, x => "");
        var abiertas = await EntradasObjetoAbiertas().Where(e => idsVisita.Contains(e.IdVisita)).Select(e => new { e.IdVisita, e.IdAutorizacionObjeto }).ToListAsync(ct);
        var descripciones = await DescribirObjetosAsync(abiertas.Select(x => x.IdAutorizacionObjeto), ct);
        return abiertas.ToLookup(x => x.IdVisita, x => descripciones.GetValueOrDefault(x.IdAutorizacionObjeto, ""));
    }
}
