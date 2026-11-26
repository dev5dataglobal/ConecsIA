using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
/// Entradas, salidas, presencia, historial y bitácora del registro de acceso (ADR-029).
/// Entrada: bloquea la fila del registro, evalúa vigencia y horario en la hora local de la sede y, si habilita, crea en
/// una transacción el episodio (visita), el movimiento, la permanencia, el consumo de cupo y la auditoría. Todo intento
/// denegado queda como movimiento con resultado Denegado y motivos, más auditoría, sin abrir permanencia. Fuera de
/// horario con «accesos.fuera_horario = Alertar» ingresa y queda auditado como alerta (D-51, Propuesta).
/// Entrada y salida exigen Idempotency-Key: un reintento devuelve la respuesta guardada.
/// </summary>
public sealed partial class AccesosController
{
    private const string CabeceraIdempotencia = "Idempotency-Key";

    /// <summary>Registra la entrada por un punto de acceso de la sede del registro.</summary>
    /// <response code="200">Entrada confirmada (o réplica idempotente).</response>
    /// <response code="409">Ingreso denegado: el cuerpo incluye <c>motivos</c>; el intento queda registrado.</response>
    [HttpPost("{id:int}/entrada")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Anadir)]
    public async Task<IActionResult> RegistrarEntrada(int id, [FromBody] MovimientoAccesoEntrada request, [FromHeader(Name = CabeceraIdempotencia)] string? clave, CancellationToken ct)
    {
        if (ClaveInvalida(ref clave) is { } faltante) return faltante;
        var observacion = Limpiar(request.Observacion);
        var hash = Hash("acceso.entrada", id, request.IdPuntoAcceso, observacion);
        var alcance = await SedesAlcanceAsync(ct);
        var puntosPropios = await PuntosAlcanceAsync(ct);

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var registro = await BloquearRegistroAsync(id, ct);
        if (registro is null || !EnAlcance(alcance, registro.IdSede)) return NoEncontrado("El registro de acceso no existe o no está en tu alcance.");
        if (await ReplicaAsync(clave!, "acceso.entrada", hash, ct) is { } replica) return replica;

        var sede = await Db.Sedes.AsNoTracking().SingleAsync(x => x.IdSede == registro.IdSede, ct);
        var punto = await Db.PuntosAcceso.AsNoTracking().SingleOrDefaultAsync(x => x.IdPuntoAcceso == request.IdPuntoAcceso && x.IdSede == registro.IdSede, ct);
        if (punto is null || !punto.Activo) return Invalido("El punto de acceso no existe en la sede del registro o está inactivo.");
        if (punto.ModoAcceso == "Salida") return Invalido("El punto de acceso solo registra salidas.");
        if (puntosPropios is not null && !puntosPropios.Contains(punto.IdPuntoAcceso))
            return await DenegarAsync(transaccion, registro, punto, "Entrada", StatusCodes.Status403Forbidden, ["No estás asignado a este punto de acceso."], ct);

        var ahora = DateTime.UtcNow;
        var local = ReglasAutorizacion.HoraLocal(ahora, sede.ZonaHoraria);
        var horario = HorarioEfectivo(registro,
            (await Db.AutorizacionesHorarios.AsNoTracking().Where(x => x.IdAutorizacion == id).ToListAsync(ct)).Select(x => new Franja(x.DiaSemana, x.HoraInicio, x.HoraFin)),
            await Db.HorariosBase.AsNoTracking().Where(x => x.IdSede == null || x.IdSede == registro.IdSede).ToListAsync(ct));
        var evaluacion = Evaluar(registro, horario, local);
        var bloqueos = evaluacion.Bloqueos;
        if (!await Db.Personas.AnyAsync(x => x.IdPersona == registro.IdPersona && x.Activo, ct)) bloqueos.Add("La persona está inactiva.");
        if (!sede.Activo) bloqueos.Add("La sede está inactiva.");
        if (!await Db.Areas.AnyAsync(x => x.IdArea == registro.IdArea && x.Activo, ct)) bloqueos.Add("El destino está inactivo.");
        if (await Db.Permanencias.AnyAsync(x => x.IdPersona == registro.IdPersona && x.IdSede == registro.IdSede && x.Estado == "Abierta", ct))
            bloqueos.Add("La persona ya está dentro de esta sede; registra antes su salida.");
        var alertar = evaluacion.Horario.Count > 0 && await ModoFueraHorarioAsync(ct) == "Alertar";
        if (bloqueos.Count > 0 || (evaluacion.Horario.Count > 0 && !alertar))
            return await DenegarAsync(transaccion, registro, punto, "Entrada", StatusCodes.Status409Conflict, [.. bloqueos, .. evaluacion.Horario], ct);

        var idUsuario = User.IdUsuario()!.Value;
        // Episodio operativo de este ingreso (D-49): siempre vinculado al registro de acceso.
        var visita = new Visita
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = registro.IdPersona, IdAutorizacion = registro.IdAutorizacion, IdSede = registro.IdSede,
            IdArea = registro.IdArea, IdMembresiaResponsable = registro.IdMembresiaResponsable, IdUsuarioRegistra = idUsuario, Motivo = registro.Motivo,
            FechaEsperada = DateOnly.FromDateTime(local), Estado = ReglasVisita.Ingreso, CreadoEn = ahora, Version = 1,
        };
        Db.Visitas.Add(visita);
        await Db.SaveChangesAsync(ct);
        var movimiento = new MovimientoAcceso
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = registro.IdPersona, IdSede = registro.IdSede, IdPuntoAcceso = punto.IdPuntoAcceso,
            IdVisita = visita.IdVisita, IdAutorizacion = registro.IdAutorizacion, IdUsuarioRegistra = idUsuario, TipoMovimiento = "Entrada",
            Resultado = "Confirmado", Medio = "Manual", OcurridoEn = ahora,
            Observacion = alertar ? $"[Fuera de horario] {string.Join(" ", evaluacion.Horario)} {observacion}".Trim() : observacion,
        };
        Db.MovimientosAcceso.Add(movimiento);
        if (!await GuardarAsync(ct)) return await ConflictoConcurrenciaAsync(transaccion, registro, punto, "Entrada", ct);
        var permanencia = new Permanencia
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = registro.IdPersona, IdSede = registro.IdSede, IdVisita = visita.IdVisita,
            IdMovimientoEntrada = movimiento.IdMovimiento, Estado = "Abierta", AbiertaEn = ahora,
        };
        Db.Permanencias.Add(permanencia);
        // Índices únicos: una permanencia por visita y una abierta por persona y sede.
        if (!await GuardarAsync(ct)) return await ConflictoConcurrenciaAsync(transaccion, registro, punto, "Entrada", ct);
        movimiento.IdPermanencia = permanencia.IdPermanencia;
        if (ReglasVisita.CupoEfectivo(registro) is int cupo)
        {
            // Consumo atómico: con la fila bloqueada solo un ingreso gana el último cupo.
            var consumidas = await Db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE public.autorizacion SET ingresos_consumidos = ingresos_consumidos + 1 WHERE id_organizacion = {OrganizacionActiva} AND id_autorizacion = {id} AND ingresos_consumidos < {cupo}", ct);
            if (consumidas == 0)
                return await DenegarAsync(transaccion, registro, punto, "Entrada", StatusCodes.Status409Conflict,
                    [cupo == 1 ? "El acceso de ingreso único ya fue utilizado." : "El acceso agotó su cupo de ingresos."], ct);
        }
        Auditar(OrganizacionActiva, "RegistrarEntrada", Entidad, id.ToString(), null,
            new { visita.IdVisita, movimiento.IdMovimiento, permanencia.IdPermanencia, punto.IdPuntoAcceso, OcurridoEn = ahora });
        if (alertar)
            Auditar(OrganizacionActiva, "IngresoFueraDeHorario", Entidad, id.ToString(), null, new { Motivos = evaluacion.Horario, movimiento.IdMovimiento }, "Alerta");
        return await ConfirmarAsync(transaccion, new MovimientoConfirmadoDto
        {
            IdAutorizacion = id, IdVisita = visita.IdVisita, IdMovimiento = movimiento.IdMovimiento, IdPermanencia = permanencia.IdPermanencia,
            TipoMovimiento = "Entrada", OcurridoEn = ahora, Alertas = alertar ? evaluacion.Horario : [],
        }, "acceso.entrada", clave!, hash, ct);
    }

    /// <summary>Registra la salida que cierra la permanencia abierta. No evalúa la vigencia (una revocación posterior no la impide).</summary>
    /// <response code="409">Salida repetida o inconsistente: no se modifica el historial.</response>
    [HttpPost("permanencias/{idPermanencia:int}/salida")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Editar)]
    public async Task<IActionResult> RegistrarSalida(int idPermanencia, [FromBody] MovimientoAccesoEntrada request, [FromHeader(Name = CabeceraIdempotencia)] string? clave, CancellationToken ct)
    {
        if (ClaveInvalida(ref clave) is { } faltante) return faltante;
        var observacion = Limpiar(request.Observacion);
        var hash = Hash("acceso.salida", idPermanencia, request.IdPuntoAcceso, observacion);
        var alcance = await SedesAlcanceAsync(ct);
        var puntosPropios = await PuntosAlcanceAsync(ct);

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        await Db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM public.permanencia WHERE id_organizacion = {OrganizacionActiva} AND id_permanencia = {idPermanencia} FOR UPDATE", ct);
        var permanencia = await Db.Permanencias.SingleOrDefaultAsync(x => x.IdPermanencia == idPermanencia, ct);
        if (permanencia is null || !EnAlcance(alcance, permanencia.IdSede)) return NoEncontrado("La permanencia no existe o no está en tu alcance.");
        if (await ReplicaAsync(clave!, "acceso.salida", hash, ct) is { } replica) return replica;
        var visita = await Db.Visitas.SingleAsync(x => x.IdVisita == permanencia.IdVisita, ct);
        var registro = visita.IdAutorizacion is int ia ? await Db.Autorizaciones.AsNoTracking().SingleOrDefaultAsync(x => x.IdAutorizacion == ia, ct) : null;
        var punto = await Db.PuntosAcceso.AsNoTracking().SingleOrDefaultAsync(x => x.IdPuntoAcceso == request.IdPuntoAcceso && x.IdSede == permanencia.IdSede, ct);
        if (punto is null || !punto.Activo) return Invalido("El punto de acceso no existe en la sede o está inactivo.");
        if (punto.ModoAcceso == "Entrada") return Invalido("El punto de acceso solo registra entradas.");
        if (registro is null) return Conflicto("Inconsistencia: la permanencia no tiene registro de acceso. Repórtala para su revisión.");
        if (puntosPropios is not null && !puntosPropios.Contains(punto.IdPuntoAcceso))
            return await DenegarAsync(transaccion, registro, punto, "Salida", StatusCodes.Status403Forbidden, ["No estás asignado a este punto de acceso."], ct);
        if (permanencia.Estado != "Abierta")
            return await DenegarAsync(transaccion, registro, punto, "Salida", StatusCodes.Status409Conflict, ["La salida de esta permanencia ya fue registrada."], ct);
        var ahora = DateTime.UtcNow;
        var duracion = (long)Math.Floor((ahora - permanencia.AbiertaEn).TotalSeconds);
        if (duracion < 0)
            return await DenegarAsync(transaccion, registro, punto, "Salida", StatusCodes.Status409Conflict,
                ["Inconsistencia: la entrada registrada es posterior al instante de salida. No se registró la salida."], ct);

        var movimiento = new MovimientoAcceso
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = permanencia.IdPersona, IdSede = permanencia.IdSede, IdPuntoAcceso = punto.IdPuntoAcceso,
            IdVisita = visita.IdVisita, IdAutorizacion = registro.IdAutorizacion, IdPermanencia = permanencia.IdPermanencia,
            IdUsuarioRegistra = User.IdUsuario()!.Value, TipoMovimiento = "Salida", Resultado = "Confirmado", Medio = "Manual", OcurridoEn = ahora, Observacion = observacion,
        };
        Db.MovimientosAcceso.Add(movimiento);
        if (!await GuardarAsync(ct)) return await ConflictoConcurrenciaAsync(transaccion, registro, punto, "Salida", ct);
        permanencia.Estado = "Cerrada";
        permanencia.CerradaEn = ahora;
        permanencia.IdMovimientoSalida = movimiento.IdMovimiento;
        permanencia.DuracionSegundos = duracion;
        visita.Estado = ReglasVisita.Finalizada;
        visita.Version++;
        visita.IdUsuarioActualiza = User.IdUsuario();
        visita.ActualizadoEn = ahora;
        // Finalizar el episodio no cierra el registro de acceso (un recurrente sigue vigente).
        // La salida de la persona no cierra sus vehículos ni dispositivos (D-10): se informan para que el operador los revise.
        var objetosDentro = (await ObjetosDentroPorVisitaAsync([visita.IdVisita], ct))[visita.IdVisita].ToList();
        Auditar(OrganizacionActiva, "RegistrarSalida", Entidad, registro.IdAutorizacion.ToString(), null,
            new { visita.IdVisita, movimiento.IdMovimiento, permanencia.IdPermanencia, punto.IdPuntoAcceso, DuracionSegundos = duracion, OcurridoEn = ahora, ObjetosDentro = objetosDentro });
        return await ConfirmarAsync(transaccion, new MovimientoConfirmadoDto
        {
            IdAutorizacion = registro.IdAutorizacion, IdVisita = visita.IdVisita, IdMovimiento = movimiento.IdMovimiento, IdPermanencia = permanencia.IdPermanencia,
            TipoMovimiento = "Salida", OcurridoEn = ahora, DuracionSegundos = duracion, ObjetosDentro = objetosDentro,
        }, "acceso.salida", clave!, hash, ct);
    }

    /// <summary>Personas dentro de la instalación (permanencias abiertas). La pantalla la vuelve a pedir periódicamente.</summary>
    [HttpGet("presencia")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<PresenciaAccesoDto>>> Presencia([FromQuery] int? idSede = null, CancellationToken ct = default)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var filas = await (
            from pe in Db.Permanencias.AsNoTracking()
            join v in Db.Visitas on pe.IdVisita equals v.IdVisita
            join p in Db.Personas on pe.IdPersona equals p.IdPersona
            join s in Db.Sedes on pe.IdSede equals s.IdSede
            join ar in Db.Areas on v.IdArea equals ar.IdArea
            join m in Db.MovimientosAcceso on pe.IdMovimientoEntrada equals m.IdMovimiento
            join z in Db.PuntosAcceso on m.IdPuntoAcceso equals z.IdPuntoAcceso
            where pe.Estado == "Abierta" && (alcance == null || alcance.Contains(pe.IdSede)) && (idSede == null || pe.IdSede == idSede)
            orderby pe.AbiertaEn
            select new PresenciaAccesoDto
            {
                IdPermanencia = pe.IdPermanencia, IdAutorizacion = v.IdAutorizacion ?? 0, IdPersona = p.IdPersona, Persona = p.Nombres + " " + p.Apellidos,
                DocumentoPersona = p.NumeroDocumento, IdSede = s.IdSede, Sede = s.Nombre, Area = ar.Nombre, PuntoEntrada = z.Nombre, EntradaEn = pe.AbiertaEn, ZonaHoraria = s.ZonaHoraria,
                Categoria = Db.Autorizaciones.Where(a => a.IdAutorizacion == v.IdAutorizacion)
                    .Select(a => Db.CategoriasAcceso.Where(c => c.IdCategoriaAcceso == a.IdCategoriaAcceso).Select(c => c.Nombre).FirstOrDefault()).FirstOrDefault(),
                Anfitrion = Db.Usuarios.Where(u => Db.MembresiasOrganizacion.Any(mo => mo.IdMembresia == v.IdMembresiaResponsable && mo.IdUsuario == u.IdUsuario)).Select(u => u.Correo).FirstOrDefault(),
            }).ToListAsync(ct);
        var idsPermanencia = filas.Select(x => x.IdPermanencia).ToList();
        var visitas = await Db.Permanencias.AsNoTracking().Where(x => idsPermanencia.Contains(x.IdPermanencia)).ToDictionaryAsync(x => x.IdPermanencia, x => x.IdVisita, ct);
        var objetos = await ObjetosDentroPorVisitaAsync(visitas.Values.ToList(), ct);
        var ahora = DateTime.UtcNow;
        foreach (var fila in filas)
        {
            fila.SegundosDentro = Math.Max(0, (long)(ahora - fila.EntradaEn).TotalSeconds);
            fila.ObjetosDentro = objetos[visitas[fila.IdPermanencia]].ToList();
        }
        return Ok(filas);
    }

    /// <summary>Historial de movimientos (entradas, salidas e intentos denegados) por persona, anfitrión, sede, vehículo y fechas.</summary>
    /// <param name="placa">Movimientos de registros que incluyen ese vehículo.</param>
    /// <param name="desde">Fecha UTC inicial (inclusive).</param>
    /// <param name="hasta">Fecha UTC final (inclusive).</param>
    [HttpGet("historial")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<PaginaHistorialDto>> Historial([FromQuery] FiltroHistorial filtro, [FromQuery] int pagina = 1, [FromQuery] int tamano = 50, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 200);
        var query = await FiltrarHistorialAsync(filtro, ct);
        var total = await query.CountAsync(ct);
        var filas = await ProyectarMovimientos(query).OrderByDescending(x => x.OcurridoEn).ThenByDescending(x => x.IdMovimiento)
            .Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return Ok(new PaginaHistorialDto { Registros = filas, Total = total, Pagina = pagina, Tamano = tamano });
    }

    /// <summary>Exporta el historial filtrado a CSV (UTF-8 con BOM, abre en Excel). Máximo 50 000 filas; la exportación se audita.</summary>
    [HttpGet("historial/exportar")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportarHistorial([FromQuery] FiltroHistorial filtro, CancellationToken ct)
    {
        const int Maximo = 50_000;
        var filas = await ProyectarMovimientos(await FiltrarHistorialAsync(filtro, ct)).OrderByDescending(x => x.OcurridoEn).Take(Maximo + 1).ToListAsync(ct);
        if (filas.Count > Maximo) return Invalido($"El resultado supera {Maximo:N0} filas: acota el rango de fechas o los filtros.");
        var csv = new StringBuilder();
        csv.AppendLine("Fecha UTC,Tipo,Resultado,Documento,Persona,Sede,Destino,Anfitrión,Punto de acceso,Operador,Registro de acceso,Motivo");
        foreach (var f in filas)
            csv.AppendLine(string.Join(",", new[]
            {
                f.OcurridoEn.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), f.TipoMovimiento, f.Resultado, f.DocumentoPersona, f.Persona,
                f.Sede, f.Area, f.Anfitrion, f.PuntoAcceso, f.Operador, f.IdAutorizacion?.ToString(CultureInfo.InvariantCulture), f.Motivo,
            }.Select(Csv)));
        Auditar(OrganizacionActiva, "ExportarHistorial", "MovimientoAcceso", "historial", null, new { Filtro = filtro, Filas = filas.Count });
        await Db.SaveChangesAsync(ct);
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8",
            $"historial-accesos-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    /// <summary>Bitácora inmutable del registro: eventos del registro, de sus vehículos/dispositivos y de sus episodios.</summary>
    [HttpGet("{id:int}/bitacora")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<EventoBitacoraDto>>> Bitacora(int id, CancellationToken ct)
    {
        if (!await (await EnAlcanceAsync(ct)).AnyAsync(x => x.IdAutorizacion == id, ct)) return NoEncontrado("El registro de acceso no existe o no está en tu alcance.");
        var objetos = await Db.AutorizacionesObjeto.AsNoTracking().Where(x => x.IdAutorizacion == id).Select(x => x.IdAutorizacionObjeto.ToString()).ToListAsync(ct);
        var visitas = await Db.Visitas.AsNoTracking().Where(x => x.IdAutorizacion == id).Select(x => x.IdVisita.ToString()).ToListAsync(ct);
        var idTexto = id.ToString();
        var organizacion = OrganizacionActiva;
        // auditoria no tiene filtro global (id_organizacion admite nulo): se filtra explícitamente por la organización activa.
        return Ok(await Db.Auditorias.AsNoTracking()
            .Where(x => x.IdOrganizacion == organizacion &&
                ((x.Entidad == Entidad && x.IdRegistro == idTexto) || (x.Entidad == EntidadObjeto && objetos.Contains(x.IdRegistro)) || (x.Entidad == "Visita" && visitas.Contains(x.IdRegistro))))
            .OrderBy(x => x.OcurridoEn).ThenBy(x => x.IdAuditoria)
            .Select(x => new EventoBitacoraDto
            {
                IdAuditoria = x.IdAuditoria, Accion = x.Accion, Entidad = x.Entidad, IdRegistro = x.IdRegistro, Resultado = x.Resultado,
                Usuario = Db.Usuarios.Where(u => u.IdUsuario == x.IdUsuario).Select(u => u.Correo).FirstOrDefault(),
                ValorAnterior = x.ValorAnterior, ValorNuevo = x.ValorNuevo, DireccionIp = x.DireccionIp, AgenteUsuario = x.AgenteUsuario, OcurridoEn = x.OcurridoEn,
            }).ToListAsync(ct));
    }

    // ---------- Apoyo ----------

    /// <summary>Filtros del historial.</summary>
    public sealed class FiltroHistorial
    {
        public int? IdPersona { get; set; }
        public string? Buscar { get; set; }
        public int? IdAnfitrion { get; set; }
        public int? IdSede { get; set; }
        public int? IdArea { get; set; }
        public string? Placa { get; set; }
        /// <summary>Entrada | Salida.</summary>
        public string? Tipo { get; set; }
        /// <summary>Confirmado | Denegado.</summary>
        public string? Resultado { get; set; }
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
    }

    private async Task<IQueryable<MovimientoAcceso>> FiltrarHistorialAsync(FiltroHistorial f, CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var q = Db.MovimientosAcceso.AsNoTracking().Where(m => alcance == null || alcance.Contains(m.IdSede));
        if (f.IdPersona is int p) q = q.Where(m => m.IdPersona == p);
        if (Limpiar(f.Buscar) is { } texto)
        {
            var patron = $"%{texto.ToUpperInvariant()}%";
            q = q.Where(m => Db.Personas.Any(x => x.IdPersona == m.IdPersona && (EF.Functions.ILike(x.NumeroDocumentoNormalizado, patron) || EF.Functions.ILike(x.Nombres + " " + x.Apellidos, patron))));
        }
        if (f.IdAnfitrion is int an) q = q.Where(m => Db.Autorizaciones.Any(a => a.IdAutorizacion == m.IdAutorizacion && a.IdMembresiaResponsable == an));
        if (f.IdSede is int s) q = q.Where(m => m.IdSede == s);
        if (f.IdArea is int ar) q = q.Where(m => Db.Autorizaciones.Any(a => a.IdAutorizacion == m.IdAutorizacion && a.IdArea == ar));
        if (Limpiar(f.Placa) is { } placa)
        {
            var normalizada = ReglasPersona.NormalizarPlaca(placa);
            q = q.Where(m => Db.AutorizacionesObjeto.Any(o => o.IdAutorizacion == m.IdAutorizacion && o.TipoObjeto == "Vehiculo"
                && Db.Vehiculos.Any(v => v.IdVehiculo == o.IdVehiculo && v.PlacaNormalizada == normalizada)));
        }
        if (Limpiar(f.Tipo) is { } tipo) q = q.Where(m => m.TipoMovimiento == tipo);
        if (Limpiar(f.Resultado) is { } resultado) q = q.Where(m => m.Resultado == resultado);
        if (f.Desde is DateOnly d) { var desde = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); q = q.Where(m => m.OcurridoEn >= desde); }
        if (f.Hasta is DateOnly h) { var hasta = h.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc); q = q.Where(m => m.OcurridoEn < hasta); }
        return q;
    }

    private IQueryable<MovimientoHistorialDto> ProyectarMovimientos(IQueryable<MovimientoAcceso> origen) =>
        from m in origen
        join p in Db.Personas on m.IdPersona equals p.IdPersona
        join s in Db.Sedes on m.IdSede equals s.IdSede
        join z in Db.PuntosAcceso on m.IdPuntoAcceso equals z.IdPuntoAcceso
        select new MovimientoHistorialDto
        {
            IdMovimiento = m.IdMovimiento, IdAutorizacion = m.IdAutorizacion, IdPersona = p.IdPersona, Persona = p.Nombres + " " + p.Apellidos,
            DocumentoPersona = p.NumeroDocumento, Sede = s.Nombre, PuntoAcceso = z.Nombre, TipoMovimiento = m.TipoMovimiento, Resultado = m.Resultado,
            Motivo = m.MotivoDenegacion ?? m.Observacion, OcurridoEn = m.OcurridoEn,
            Area = Db.Autorizaciones.Where(a => a.IdAutorizacion == m.IdAutorizacion).Select(a => Db.Areas.Where(x => x.IdArea == a.IdArea).Select(x => x.Nombre).FirstOrDefault()).FirstOrDefault(),
            Anfitrion = Db.Autorizaciones.Where(a => a.IdAutorizacion == m.IdAutorizacion)
                .Select(a => Db.Usuarios.Where(u => Db.MembresiasOrganizacion.Any(mo => mo.IdMembresia == a.IdMembresiaResponsable && mo.IdUsuario == u.IdUsuario)).Select(u => u.Correo).FirstOrDefault()).FirstOrDefault(),
            Operador = Db.Usuarios.Where(u => u.IdUsuario == m.IdUsuarioRegistra).Select(u => u.Correo).FirstOrDefault(),
        };

    private static string Csv(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return "";
        // Neutraliza fórmulas (inyección CSV) y escapa comillas.
        if (valor[0] is '=' or '+' or '-' or '@' or '\t' or '\r') valor = "'" + valor;
        return valor.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;
    }

    private ObjectResult? ClaveInvalida(ref string? clave)
    {
        clave = clave?.Trim();
        return string.IsNullOrEmpty(clave) || clave.Length > 100
            ? Invalido($"Falta la cabecera {CabeceraIdempotencia} (máximo 100 caracteres): evita registrar dos veces el mismo movimiento.")
            : null;
    }

    private static string Hash(string comando, int id, int punto, string? observacion) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{comando}|{id}|{punto}|{observacion}")));

    private async Task<IActionResult?> ReplicaAsync(string clave, string comando, string hash, CancellationToken ct)
    {
        var previa = await Db.SolicitudesIdempotentes.AsNoTracking().SingleOrDefaultAsync(x => x.ClaveIdempotencia == clave, ct);
        if (previa is null) return null;
        if (previa.Comando != comando || previa.HashSolicitud != hash) return Conflicto($"La clave {CabeceraIdempotencia} ya se usó para otra solicitud.");
        Response.Headers["Idempotent-Replayed"] = "true";
        return new ContentResult { StatusCode = previa.CodigoResultado, Content = previa.Respuesta, ContentType = "application/json" };
    }

    /// <summary>
    /// Guarda, registra la respuesta idempotente y confirma. La respuesta es compacta (cabe en solicitud_idempotente y
    /// sirve a lectores automáticos); el detalle completo se consulta en GET /api/accesos/{id}.
    /// </summary>
    private async Task<IActionResult> ConfirmarAsync<T>(IDbContextTransaction transaccion, T respuesta, string comando, string clave, string hash, CancellationToken ct)
    {
        await Db.SaveChangesAsync(ct);
        Db.SolicitudesIdempotentes.Add(new SolicitudIdempotente
        {
            IdOrganizacion = OrganizacionActiva, ClaveIdempotencia = clave, Comando = comando, HashSolicitud = hash,
            CodigoResultado = StatusCodes.Status200OK, Respuesta = JsonSerializer.Serialize(respuesta, Json), CreadoEn = DateTime.UtcNow,
        });
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Ok(respuesta);
    }

    /// <summary>
    /// Deshace la operación y deja constancia del intento: movimiento con resultado Denegado (sin permanencia) y auditoría
    /// con los motivos. Responde problem+json con <c>motivos</c>.
    /// </summary>
    private async Task<IActionResult> DenegarAsync(IDbContextTransaction transaccion, Autorizacion registro, PuntoAcceso punto, string tipo, int codigo, List<string> motivos, CancellationToken ct)
    {
        await transaccion.RollbackAsync(ct);
        Db.ChangeTracker.Clear();
        motivos = motivos.Distinct().ToList();
        Db.MovimientosAcceso.Add(new MovimientoAcceso
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = registro.IdPersona, IdSede = registro.IdSede, IdPuntoAcceso = punto.IdPuntoAcceso,
            IdAutorizacion = registro.IdAutorizacion, IdUsuarioRegistra = User.IdUsuario()!.Value, TipoMovimiento = tipo, Resultado = "Denegado",
            MotivoDenegacion = string.Join(" ", motivos), Medio = "Manual", OcurridoEn = DateTime.UtcNow,
        });
        Auditar(OrganizacionActiva, tipo == "Entrada" ? "RegistrarEntrada" : "RegistrarSalida", Entidad, registro.IdAutorizacion.ToString(), null,
            new { Motivos = motivos, punto.IdPuntoAcceso }, "Denegado");
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

    private Task<IActionResult> ConflictoConcurrenciaAsync(IDbContextTransaction transaccion, Autorizacion registro, PuntoAcceso punto, string tipo, CancellationToken ct) =>
        DenegarAsync(transaccion, registro, punto, tipo, StatusCodes.Status409Conflict,
            ["Otro registro simultáneo cambió la permanencia de esta persona en la sede. Consulta el estado actual."], ct);

    /// <summary>Puntos de acceso asignados a la membresía; null = sin restricción por punto.</summary>
    private async Task<List<int>?> PuntosAlcanceAsync(CancellationToken ct)
    {
        var membresia = await MembresiaPropiaAsync(ct);
        if (membresia is null) return null;
        var puntos = await Db.MembresiasPuntosAcceso.AsNoTracking().Where(x => x.IdMembresia == membresia).Select(x => x.IdPuntoAcceso).ToListAsync(ct);
        return puntos.Count == 0 ? null : puntos;
    }
}
