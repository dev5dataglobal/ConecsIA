using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ApiConecsia.Services.ReglasVisita;

namespace ApiConecsia.Controllers;

/// <summary>
/// Visitas de personas externas y su ciclo operativo (RF-11/RF-12, HU-028, HU-032..HU-034, HU-037).
/// Visita ≠ persona ≠ autorización ≠ permanencia: la visita es el episodio; la entrada abre una permanencia y la
/// salida la cierra. Organización: siempre la activa validada en servidor. Alcance por sede: las sedes asignadas a la
/// membresía (membresia_sede); sin asignaciones, todas las de la organización (Propuesta, D-34). Puntos de acceso:
/// los asignados (membresia_punto_acceso) o, sin asignaciones, todos los de las sedes del alcance.
/// Permisos (D-35, Propuesta): consultar = Visitas.Consultar; registrar = Visitas.Añadir; vincular autorización y
/// cancelar = Visitas.Editar; entrada = ControlAccesos.Añadir; salida = ControlAccesos.Editar; presencia e historial de
/// movimientos = ControlAccesos.Consultar. Decidir la autorización corresponde al módulo de autorizaciones (EP-07).
/// Entrada y salida exigen Idempotency-Key, bloquean la fila de la visita y guardan movimiento, permanencia, visita y
/// auditoría en una sola transacción; índices únicos de permanencia son la última defensa ante carreras.
/// </summary>
[Route("api/visitas")]
public sealed class VisitasController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private const string Entidad = "Visita";
    private const string CabeceraIdempotencia = "Idempotency-Key";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---------- Consulta ----------

    [HttpGet]
    [RequiereModulo(CodigosModulo.Visitas, AccionModulo.Consultar)]
    public async Task<ActionResult<PaginaVisitasDto>> Listar(
        [FromQuery] string? buscar = null, [FromQuery] string? estado = null, [FromQuery] int? idSede = null, [FromQuery] int? idArea = null,
        [FromQuery] int? idResponsable = null, [FromQuery] DateOnly? desde = null, [FromQuery] DateOnly? hasta = null,
        [FromQuery] string? orden = null, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken ct = default)
    {
        pagina = Math.Max(pagina, 1);
        tamano = Math.Clamp(tamano, 1, 100);
        var query = await VisitasEnAlcanceAsync(ct);
        if (idSede is int sede) query = query.Where(x => x.IdSede == sede);
        if (idArea is int area) query = query.Where(x => x.IdArea == area);
        if (idResponsable is int responsable) query = query.Where(x => x.IdMembresiaResponsable == responsable);
        // Fecha de la visita: la esperada o, si no se indicó, la de registro (UTC).
        if (desde is DateOnly d)
        {
            var inicio = d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.FechaEsperada != null ? x.FechaEsperada >= d : x.CreadoEn >= inicio);
        }
        if (hasta is DateOnly h)
        {
            var fin = h.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.FechaEsperada != null ? x.FechaEsperada <= h : x.CreadoEn < fin);
        }
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            var documento = $"{ReglasPersona.NormalizarDocumento(buscar)}%";
            query = query.Where(x => Db.Personas.Any(p => p.IdPersona == x.IdPersona
                && (EF.Functions.ILike(p.Nombres + " " + p.Apellidos, texto) || EF.Functions.Like(p.NumeroDocumentoNormalizado, documento))));
        }

        // Indicadores sobre los mismos filtros, sin el de estado.
        var conteos = await query.GroupBy(x => x.Estado).Select(g => new { Estado = g.Key, Total = g.Count() }).ToListAsync(ct);
        int Conteo(string e) => conteos.Where(x => x.Estado == e).Sum(x => x.Total);
        if (estado is not null && Estados.Contains(estado)) query = query.Where(x => x.Estado == estado);

        var total = await query.CountAsync(ct);
        var proyectada = Proyectar(query);
        proyectada = orden switch
        {
            "creado_asc" => proyectada.OrderBy(x => x.CreadoEn).ThenBy(x => x.IdVisita),
            "persona" => proyectada.OrderBy(x => x.Persona).ThenByDescending(x => x.IdVisita),
            "estado" => proyectada.OrderBy(x => x.Estado).ThenByDescending(x => x.IdVisita),
            "entrada_desc" => proyectada.OrderByDescending(x => x.EntradaEn).ThenByDescending(x => x.IdVisita),
            _ => proyectada.OrderByDescending(x => x.CreadoEn).ThenByDescending(x => x.IdVisita),
        };
        return Ok(new PaginaVisitasDto
        {
            Registros = await proyectada.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct),
            Total = total, Pagina = pagina, Tamano = tamano,
            Indicadores = new IndicadoresVisitasDto { Pendientes = Conteo(Pendiente), Autorizadas = Conteo(Autorizada), Dentro = Conteo(Ingreso), Finalizadas = Conteo(Finalizada) },
        });
    }

    [HttpGet("{id:int}")]
    [RequiereModulo(CodigosModulo.Visitas, AccionModulo.Consultar)]
    public async Task<ActionResult<VisitaDetalleDto>> Obtener(int id, CancellationToken ct)
    {
        var detalle = await DetalleAsync(id, ct);
        return detalle is null ? NoEncontrado("La visita no existe o no está en tu alcance.") : Ok(detalle);
    }

    /// <summary>Sedes, destinos, responsables y puntos de acceso del alcance del usuario.</summary>
    [HttpGet("opciones")]
    [RequiereModulo(CodigosModulo.Visitas, AccionModulo.Consultar)]
    public async Task<ActionResult<OpcionesVisitaDto>> Opciones(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var puntos = await PuntosAlcanceAsync(ct);
        var sedes = await Db.Sedes.AsNoTracking().Where(x => x.Activo && (alcance == null || alcance.Contains(x.IdSede)))
            .OrderBy(x => x.Nombre).Select(x => new OpcionCatalogoDto { Id = x.IdSede, Nombre = x.Nombre }).ToListAsync(ct);
        var idsSede = sedes.Select(x => x.Id).ToList();
        var designaciones = await Db.AreasResponsables.AsNoTracking().Where(x => x.Activo).Select(x => new { x.IdMembresia, x.IdArea }).ToListAsync(ct);
        var responsables = await (
            from m in Db.MembresiasOrganizacion.AsNoTracking()
            join u in Db.Usuarios on m.IdUsuario equals u.IdUsuario
            where (m.Estado == "Activa" || m.Estado == "Aceptada") && u.Activo
            orderby u.Correo
            select new ResponsableVisitaDto { IdMembresia = m.IdMembresia, Nombre = u.Correo }).ToListAsync(ct);
        foreach (var r in responsables) r.Areas = designaciones.Where(x => x.IdMembresia == r.IdMembresia).Select(x => x.IdArea).ToList();
        return Ok(new OpcionesVisitaDto
        {
            Sedes = sedes,
            Areas = await Db.Areas.AsNoTracking().Where(x => x.Activo && idsSede.Contains(x.IdSede)).OrderBy(x => x.Nombre)
                .Select(x => new OpcionAreaDto { Id = x.IdArea, IdSede = x.IdSede, Nombre = x.Nombre }).ToListAsync(ct),
            Responsables = responsables,
            PuntosAcceso = await (
                from pa in Db.PuntosAcceso.AsNoTracking()
                join z in Db.Zonas on pa.IdZona equals z.IdZona
                where pa.Activo && z.Activo && idsSede.Contains(pa.IdSede) && (puntos == null || puntos.Contains(pa.IdPuntoAcceso))
                orderby pa.Nombre
                select new PuntoAccesoVisitaDto { Id = pa.IdPuntoAcceso, IdSede = pa.IdSede, IdZona = z.IdZona, Zona = z.Nombre, Nombre = pa.Nombre, ModoAcceso = pa.ModoAcceso }).ToListAsync(ct),
            PermiteSinAutorizacion = await PoliticaSinAutorizacionAsync(ct),
        });
    }

    /// <summary>Autorizaciones de la persona que aún pueden vincularse a una visita (Pendiente o Aprobada, no vencidas).</summary>
    [HttpGet("autorizaciones")]
    [RequiereModulo(CodigosModulo.Visitas, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<AutorizacionDisponibleDto>>> AutorizacionesDisponibles([FromQuery] int idPersona, CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        return Ok(await Db.Autorizaciones.AsNoTracking()
            .Where(x => x.IdPersona == idPersona && (x.Estado == AutorizacionPendiente || x.Estado == AutorizacionAprobada)
                && (x.FechaFin == null || x.FechaFin >= hoy) && (alcance == null || alcance.Contains(x.IdSede)))
            .OrderByDescending(x => x.FechaInicio)
            .Select(x => new AutorizacionDisponibleDto
            {
                IdAutorizacion = x.IdAutorizacion, Estado = x.Estado, Modalidad = x.Modalidad, IdSede = x.IdSede, IdArea = x.IdArea,
                FechaInicio = x.FechaInicio, FechaFin = x.FechaFin,
            }).ToListAsync(ct));
    }

    /// <summary>Visitas con permanencia abierta (personas dentro). Fuente persistida: la pantalla la vuelve a pedir al reconectar.</summary>
    [HttpGet("presencia")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<PresenciaVisitaDto>>> Presencia([FromQuery] int? idSede = null, CancellationToken ct = default)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var query =
            from pe in Db.Permanencias.AsNoTracking()
            join v in Db.Visitas on pe.IdVisita equals v.IdVisita
            join p in Db.Personas on pe.IdPersona equals p.IdPersona
            join s in Db.Sedes on pe.IdSede equals s.IdSede
            join a in Db.Areas on v.IdArea equals a.IdArea
            join m in Db.MovimientosAcceso on pe.IdMovimientoEntrada equals m.IdMovimiento
            join z in Db.PuntosAcceso on m.IdPuntoAcceso equals z.IdPuntoAcceso
            where pe.Estado == "Abierta" && (alcance == null || alcance.Contains(pe.IdSede)) && (idSede == null || pe.IdSede == idSede)
            orderby pe.AbiertaEn
            select new PresenciaVisitaDto
            {
                IdVisita = v.IdVisita, IdPermanencia = pe.IdPermanencia, IdPersona = p.IdPersona, Persona = p.Nombres + " " + p.Apellidos,
                DocumentoPersona = p.NumeroDocumento, IdSede = s.IdSede, Sede = s.Nombre, Area = a.Nombre, PuntoEntrada = z.Nombre,
                Responsable = Db.Usuarios.Where(u => Db.MembresiasOrganizacion.Any(mo => mo.IdMembresia == v.IdMembresiaResponsable && mo.IdUsuario == u.IdUsuario)).Select(u => u.Correo).FirstOrDefault(),
                EntradaEn = pe.AbiertaEn, ZonaHoraria = s.ZonaHoraria,
            };
        var filas = await query.ToListAsync(ct);
        var ahora = DateTime.UtcNow;
        foreach (var fila in filas) fila.SegundosDentro = Math.Max(0, (long)(ahora - fila.EntradaEn).TotalSeconds);
        return Ok(filas);
    }

    // ---------- Registro y gestión ----------

    /// <summary>
    /// Registra la visita. Con autorización Aprobada queda Autorizada; con autorización Pendiente o sin autorización
    /// queda Pendiente, salvo que la organización permita visitas sin autorización (se registra la política aplicada,
    /// sin atribuir aprobación a nadie).
    /// </summary>
    [HttpPost]
    [RequiereModulo(CodigosModulo.Visitas, AccionModulo.Anadir)]
    public async Task<ActionResult<VisitaDetalleDto>> Registrar([FromBody] VisitaEntrada request, CancellationToken ct)
    {
        var persona = await Db.Personas.AsNoTracking().SingleOrDefaultAsync(x => x.IdPersona == request.IdPersona, ct);
        if (persona is null) return Invalido("La persona no existe en tu organización.");
        if (!persona.Activo) return Invalido("La persona está inactiva: no se pueden registrar visitas.");
        var sede = await Db.Sedes.AsNoTracking().SingleOrDefaultAsync(x => x.IdSede == request.IdSede && x.Activo, ct);
        if (sede is null) return Invalido("La sede no existe en tu organización o está inactiva.");
        if (!EnAlcance(await SedesAlcanceAsync(ct), sede.IdSede)) return Prohibido("La sede no está en tu alcance.");
        if (!await Db.Areas.AnyAsync(x => x.IdArea == request.IdArea && x.IdSede == sede.IdSede && x.Activo, ct))
            return Invalido("El destino no pertenece a la sede o está inactivo.");
        if (await ValidarResponsableAsync(request.IdArea, request.IdMembresiaResponsable, ct) is { } responsableInvalido) return Invalido(responsableInvalido);
        if ((request.HoraEsperadaInicio is null) != (request.HoraEsperadaFin is null)) return Invalido("Indique ambas horas del horario esperado o ninguna.");
        if (request.HoraEsperadaInicio is TimeOnly hi && request.HoraEsperadaFin is TimeOnly hf && hf <= hi) return Invalido("La hora de fin esperada debe ser posterior a la de inicio.");
        var hoy = DateOnly.FromDateTime(HoraLocal(DateTime.UtcNow, sede.ZonaHoraria));
        if (request.FechaEsperada is DateOnly esperada && esperada < hoy) return Invalido("La fecha esperada no puede ser anterior a hoy.");

        var estado = Pendiente;
        string? politica = null;
        if (request.IdAutorizacion is int idAutorizacion)
        {
            var autorizacion = await Db.Autorizaciones.AsNoTracking().SingleOrDefaultAsync(x => x.IdAutorizacion == idAutorizacion, ct);
            if (ValidarVinculo(autorizacion, request.IdPersona, sede.IdSede, request.IdArea, hoy) is { } invalida) return Invalido(invalida);
            if (autorizacion!.Estado == AutorizacionAprobada) estado = Autorizada;
        }
        else if (await PoliticaSinAutorizacionAsync(ct))
        {
            estado = Autorizada;
            politica = ParametroSinAutorizacion;
        }

        var ahora = DateTime.UtcNow;
        var visita = new Visita
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = persona.IdPersona, IdSede = sede.IdSede, IdArea = request.IdArea,
            IdMembresiaResponsable = request.IdMembresiaResponsable, IdAutorizacion = request.IdAutorizacion, PoliticaAplicada = politica,
            Motivo = Limpiar(request.Motivo), Observacion = Limpiar(request.Observacion), FechaEsperada = request.FechaEsperada,
            HoraEsperadaInicio = request.HoraEsperadaInicio, HoraEsperadaFin = request.HoraEsperadaFin,
            Estado = estado, IdUsuarioRegistra = User.IdUsuario()!.Value, CreadoEn = ahora, Version = 1,
        };
        Db.Visitas.Add(visita);
        await Db.SaveChangesAsync(ct);
        Auditar(OrganizacionActiva, "Registrar", Entidad, visita.IdVisita.ToString(), null, Resumen(visita));
        if (politica is not null)
            Auditar(OrganizacionActiva, "AplicarPoliticaSinAutorizacion", Entidad, visita.IdVisita.ToString(), null, new { Politica = politica, visita.IdSede, visita.IdArea });
        await Db.SaveChangesAsync(ct);
        return Created($"/api/visitas/{visita.IdVisita}", await DetalleAsync(visita.IdVisita, ct));
    }

    /// <summary>Vincula la autorización que habilita una visita Pendiente; si está Aprobada, la visita pasa a Autorizada.</summary>
    [HttpPost("{id:int}/autorizacion")]
    [RequiereModulo(CodigosModulo.Visitas, AccionModulo.Editar)]
    public async Task<ActionResult<VisitaDetalleDto>> VincularAutorizacion(int id, [FromBody] VincularAutorizacionEntrada request, CancellationToken ct)
    {
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var visita = await BloquearVisitaAsync(id, ct);
        if (visita is null || !EnAlcance(await SedesAlcanceAsync(ct), visita.IdSede)) return NoEncontrado("La visita no existe o no está en tu alcance.");
        if (visita.Estado != Pendiente) return Conflicto($"Solo se vincula una autorización a visitas pendientes; esta está {visita.Estado}.");
        var zona = await ZonaHorariaAsync(visita.IdSede, ct);
        var autorizacion = await Db.Autorizaciones.AsNoTracking().SingleOrDefaultAsync(x => x.IdAutorizacion == request.IdAutorizacion, ct);
        if (ValidarVinculo(autorizacion, visita.IdPersona, visita.IdSede, visita.IdArea, DateOnly.FromDateTime(HoraLocal(DateTime.UtcNow, zona))) is { } invalida)
            return Invalido(invalida);

        var anterior = Resumen(visita);
        visita.IdAutorizacion = autorizacion!.IdAutorizacion;
        visita.PoliticaAplicada = null;
        if (autorizacion.Estado == AutorizacionAprobada) visita.Estado = Autorizada;
        Tocar(visita);
        Auditar(OrganizacionActiva, "VincularAutorizacion", Entidad, id.ToString(), anterior, Resumen(visita));
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Ok(await DetalleAsync(id, ct));
    }

    [HttpPost("{id:int}/cancelar")]
    [RequiereModulo(CodigosModulo.Visitas, AccionModulo.Editar)]
    public async Task<ActionResult<VisitaDetalleDto>> Cancelar(int id, [FromBody] CancelarVisitaEntrada request, CancellationToken ct)
    {
        if (Limpiar(request.Motivo) is not { } motivo) return Invalido("Indique el motivo de la cancelación.");
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var visita = await BloquearVisitaAsync(id, ct);
        if (visita is null || !EnAlcance(await SedesAlcanceAsync(ct), visita.IdSede)) return NoEncontrado("La visita no existe o no está en tu alcance.");
        if (!Cancelable(visita.Estado))
            return Conflicto(visita.Estado == Ingreso
                ? "La persona ya ingresó: registra su salida en lugar de cancelar la visita."
                : $"No se puede cancelar una visita {visita.Estado.ToLowerInvariant()}.");
        var anterior = Resumen(visita);
        visita.Estado = Cancelada;
        visita.MotivoCierre = motivo;
        Tocar(visita);
        Auditar(OrganizacionActiva, "Cancelar", Entidad, id.ToString(), anterior, Resumen(visita));
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Ok(await DetalleAsync(id, ct));
    }

    // ---------- Entrada y salida ----------

    [HttpPost("{id:int}/entrada")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Anadir)]
    public Task<IActionResult> RegistrarEntrada(int id, [FromBody] MovimientoVisitaEntrada request, [FromHeader(Name = CabeceraIdempotencia)] string? clave, CancellationToken ct) =>
        RegistrarMovimientoAsync(id, request, clave, entrada: true, ct);

    [HttpPost("{id:int}/salida")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Editar)]
    public Task<IActionResult> RegistrarSalida(int id, [FromBody] MovimientoVisitaEntrada request, [FromHeader(Name = CabeceraIdempotencia)] string? clave, CancellationToken ct) =>
        RegistrarMovimientoAsync(id, request, clave, entrada: false, ct);

    private async Task<IActionResult> RegistrarMovimientoAsync(int id, MovimientoVisitaEntrada request, string? clave, bool entrada, CancellationToken ct)
    {
        clave = clave?.Trim();
        if (string.IsNullOrEmpty(clave) || clave.Length > 100)
            return Invalido($"Falta la cabecera {CabeceraIdempotencia} (máximo 100 caracteres): evita registrar dos veces el mismo movimiento.");
        var comando = entrada ? "visita.entrada" : "visita.salida";
        var observacion = Limpiar(request.Observacion);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{comando}|{id}|{request.IdPuntoAcceso}|{observacion}")));
        var accion = entrada ? "RegistrarEntrada" : "RegistrarSalida";
        var alcance = await SedesAlcanceAsync(ct);
        var puntosPropios = await PuntosAlcanceAsync(ct);

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        // El bloqueo serializa operadores y reintentos sobre la misma visita; la réplica concurrente ve la respuesta guardada.
        var visita = await BloquearVisitaAsync(id, ct);
        if (visita is null || !EnAlcance(alcance, visita.IdSede)) return NoEncontrado("La visita no existe o no está en tu alcance.");

        var previa = await Db.SolicitudesIdempotentes.AsNoTracking().SingleOrDefaultAsync(x => x.ClaveIdempotencia == clave, ct);
        if (previa is not null)
        {
            if (previa.Comando != comando || previa.HashSolicitud != hash)
                return Conflicto($"La clave {CabeceraIdempotencia} ya se usó para otra solicitud.");
            Response.Headers["Idempotent-Replayed"] = "true";
            return new ContentResult { StatusCode = previa.CodigoResultado, Content = previa.Respuesta, ContentType = "application/json" };
        }

        var sede = await Db.Sedes.AsNoTracking().SingleAsync(x => x.IdSede == visita.IdSede, ct);
        // El punto debe ser de la organización (filtro global) y de la sede de la visita; él y su zona, activos.
        var punto = await Db.PuntosAcceso.AsNoTracking().SingleOrDefaultAsync(x => x.IdPuntoAcceso == request.IdPuntoAcceso && x.IdSede == visita.IdSede, ct);
        if (punto is null || !punto.Activo) return Invalido("El punto de acceso no existe en la sede de la visita o está inactivo.");
        if (!await Db.Zonas.AnyAsync(x => x.IdZona == punto.IdZona && x.Activo, ct)) return Invalido("La zona del punto de acceso está inactiva.");
        if (puntosPropios is not null && !puntosPropios.Contains(punto.IdPuntoAcceso))
            return await DenegarAsync(transaccion, visita, accion, StatusCodes.Status403Forbidden, ["No estás asignado a este punto de acceso."], null, ct);
        var modo = punto.ModoAcceso;
        if (modo != "Ambos" && modo != (entrada ? "Entrada" : "Salida"))
            return Invalido(entrada ? "El punto de acceso solo registra salidas." : "El punto de acceso solo registra entradas.");

        var ahora = DateTime.UtcNow;
        return entrada
            ? await ConfirmarEntradaAsync(transaccion, visita, sede, punto, observacion, comando, clave, hash, ahora, ct)
            : await ConfirmarSalidaAsync(transaccion, visita, punto, observacion, comando, clave, hash, ahora, ct);
    }

    private async Task<IActionResult> ConfirmarEntradaAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion, Visita visita, Sede sede, PuntoAcceso punto,
        string? observacion, string comando, string clave, string hash, DateTime ahora, CancellationToken ct)
    {
        var evaluacion = await EvaluarIngresoAsync(visita, sede, [punto.IdZona], ahora, ct);
        if (evaluacion.Motivos.Count > 0)
            return await DenegarAsync(transaccion, visita, "RegistrarEntrada", StatusCodes.Status409Conflict, evaluacion.Motivos, evaluacion.EstadoDerivado, ct);

        var movimiento = new MovimientoAcceso
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = visita.IdPersona, IdSede = visita.IdSede, IdPuntoAcceso = punto.IdPuntoAcceso, IdZona = punto.IdZona,
            IdVisita = visita.IdVisita, IdAutorizacion = visita.IdAutorizacion, IdUsuarioRegistra = User.IdUsuario()!.Value,
            TipoMovimiento = "Entrada", Resultado = "Confirmado", Medio = "Manual", OcurridoEn = ahora, Observacion = observacion,
        };
        Db.MovimientosAcceso.Add(movimiento);
        if (!await GuardarAsync(ct)) return await ConflictoConcurrenciaAsync(transaccion, visita, ct);
        var permanencia = new Permanencia
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = visita.IdPersona, IdSede = visita.IdSede, IdVisita = visita.IdVisita,
            IdMovimientoEntrada = movimiento.IdMovimiento, Estado = "Abierta", AbiertaEn = ahora,
        };
        Db.Permanencias.Add(permanencia);
        // Índices únicos: una permanencia por visita y una abierta por persona y sede.
        if (!await GuardarAsync(ct)) return await ConflictoConcurrenciaAsync(transaccion, visita, ct);
        movimiento.IdPermanencia = permanencia.IdPermanencia;

        if (evaluacion.Autorizacion is { } autorizacion && CupoEfectivo(autorizacion) is int cupo)
        {
            // Consumo atómico: la condición se reevalúa tras el bloqueo de fila, así que solo un ingreso gana el último cupo.
            var consumidas = await Db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE public.autorizacion SET ingresos_consumidos = ingresos_consumidos + 1 WHERE id_organizacion = {OrganizacionActiva} AND id_autorizacion = {autorizacion.IdAutorizacion} AND ingresos_consumidos < {cupo}", ct);
            if (consumidas == 0)
                return await DenegarAsync(transaccion, visita, "RegistrarEntrada", StatusCodes.Status409Conflict,
                    [cupo == 1 ? "La autorización de ingreso único ya fue utilizada." : "La autorización agotó su cupo de ingresos."], null, ct);
        }

        var anterior = Resumen(visita);
        visita.Estado = Ingreso;
        Tocar(visita);
        Auditar(OrganizacionActiva, "RegistrarEntrada", Entidad, visita.IdVisita.ToString(), anterior,
            new { visita.Estado, IdMovimiento = movimiento.IdMovimiento, permanencia.IdPermanencia, IdPuntoAcceso = punto.IdPuntoAcceso, IdZonaPunto = punto.IdZona, visita.IdAutorizacion, visita.PoliticaAplicada, OcurridoEn = ahora });
        return await ConfirmarAsync(transaccion, visita.IdVisita, comando, clave, hash, ct);
    }

    private async Task<IActionResult> ConfirmarSalidaAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion, Visita visita, PuntoAcceso punto,
        string? observacion, string comando, string clave, string hash, DateTime ahora, CancellationToken ct)
    {
        // La revocación o el vencimiento posteriores al ingreso no impiden la salida: aquí no se evalúa la autorización.
        if (visita.Estado == Finalizada)
            return await DenegarAsync(transaccion, visita, "RegistrarSalida", StatusCodes.Status409Conflict, ["La salida de esta visita ya fue registrada."], null, ct);
        var permanencia = await Db.Permanencias.SingleOrDefaultAsync(x => x.IdVisita == visita.IdVisita && x.Estado == "Abierta", ct);
        if (visita.Estado != Ingreso || permanencia is null)
            return await DenegarAsync(transaccion, visita, "RegistrarSalida", StatusCodes.Status409Conflict,
                ["Inconsistencia: la visita no tiene una entrada abierta. No se registró la salida; repórtala para su revisión."], null, ct);
        var duracion = (long)Math.Floor((ahora - permanencia.AbiertaEn).TotalSeconds);
        if (duracion < 0)
            return await DenegarAsync(transaccion, visita, "RegistrarSalida", StatusCodes.Status409Conflict,
                ["Inconsistencia: la entrada registrada es posterior al instante de salida. No se registró la salida."], null, ct);

        var movimiento = new MovimientoAcceso
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = visita.IdPersona, IdSede = visita.IdSede, IdPuntoAcceso = punto.IdPuntoAcceso, IdZona = punto.IdZona,
            IdVisita = visita.IdVisita, IdAutorizacion = visita.IdAutorizacion, IdPermanencia = permanencia.IdPermanencia,
            IdUsuarioRegistra = User.IdUsuario()!.Value, TipoMovimiento = "Salida", Resultado = "Confirmado", Medio = "Manual",
            OcurridoEn = ahora, Observacion = observacion,
        };
        Db.MovimientosAcceso.Add(movimiento);
        if (!await GuardarAsync(ct)) return await ConflictoConcurrenciaAsync(transaccion, visita, ct);
        permanencia.Estado = "Cerrada";
        permanencia.CerradaEn = ahora;
        permanencia.IdMovimientoSalida = movimiento.IdMovimiento;
        permanencia.DuracionSegundos = duracion;
        var anterior = Resumen(visita);
        visita.Estado = Finalizada;
        Tocar(visita);
        Auditar(OrganizacionActiva, "RegistrarSalida", Entidad, visita.IdVisita.ToString(), anterior,
            new { visita.Estado, IdMovimiento = movimiento.IdMovimiento, permanencia.IdPermanencia, IdPuntoAcceso = punto.IdPuntoAcceso, IdZonaPunto = punto.IdZona, DuracionSegundos = duracion, OcurridoEn = ahora });
        return await ConfirmarAsync(transaccion, visita.IdVisita, comando, clave, hash, ct);
    }

    /// <summary>Guarda los cambios, la respuesta idempotente y confirma la transacción.</summary>
    private async Task<IActionResult> ConfirmarAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion, int idVisita, string comando, string clave, string hash, CancellationToken ct)
    {
        await Db.SaveChangesAsync(ct);
        var detalle = await DetalleAsync(idVisita, ct);
        Db.SolicitudesIdempotentes.Add(new SolicitudIdempotente
        {
            IdOrganizacion = OrganizacionActiva, ClaveIdempotencia = clave, Comando = comando, HashSolicitud = hash,
            CodigoResultado = StatusCodes.Status200OK, Respuesta = JsonSerializer.Serialize(detalle, Json), CreadoEn = DateTime.UtcNow,
        });
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Ok(detalle);
    }

    /// <summary>
    /// Deshace la operación, conserva (si aplica) el estado terminal derivado de la autorización o la fecha, audita la
    /// denegación con sus motivos y responde con ellos. Nunca abre permanencias ni registra movimientos.
    /// </summary>
    private async Task<IActionResult> DenegarAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion, Visita visita, string accion, int codigo,
        List<string> motivos, string? estadoDerivado, CancellationToken ct)
    {
        await transaccion.RollbackAsync(ct);
        Db.ChangeTracker.Clear();
        if (estadoDerivado is not null)
        {
            var actual = await Db.Visitas.SingleAsync(x => x.IdVisita == visita.IdVisita, ct);
            if (Cancelable(actual.Estado))
            {
                var anterior = Resumen(actual);
                actual.Estado = estadoDerivado;
                actual.MotivoCierre = motivos[0];
                Tocar(actual);
                Auditar(OrganizacionActiva, "ActualizarEstado", Entidad, visita.IdVisita.ToString(), anterior, Resumen(actual));
            }
        }
        Auditar(OrganizacionActiva, accion, Entidad, visita.IdVisita.ToString(), null, new { Motivos = motivos }, resultado: "Denegado");
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { Db.ChangeTracker.Clear(); }
        var problema = new ProblemDetails
        {
            Status = codigo,
            Title = codigo == StatusCodes.Status403Forbidden ? "Acceso denegado" : accion == "RegistrarEntrada" ? "Ingreso denegado" : "Salida no registrada",
            Detail = string.Join(" ", motivos),
        };
        problema.Extensions["motivos"] = motivos;
        return new ObjectResult(problema) { StatusCode = codigo, ContentTypes = { "application/problem+json" } };
    }

    private async Task<IActionResult> ConflictoConcurrenciaAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion, Visita visita, CancellationToken ct) =>
        await DenegarAsync(transaccion, visita, visita.Estado == Ingreso ? "RegistrarSalida" : "RegistrarEntrada", StatusCodes.Status409Conflict,
            ["Otro registro simultáneo cambió la permanencia de esta persona en la sede. Consulta el estado actual."], null, ct);

    // ---------- Evaluación ----------

    private sealed record Evaluacion(List<string> Motivos, Autorizacion? Autorizacion, string? EstadoDerivado);

    /// <summary>Evalúa si la visita puede ingresar en <paramref name="ahora"/>. <paramref name="zonasPunto"/> null = sin punto elegido.</summary>
    private async Task<Evaluacion> EvaluarIngresoAsync(Visita visita, Sede sede, IReadOnlyCollection<int>? zonasPunto, DateTime ahora, CancellationToken ct)
    {
        var motivos = new List<string>();
        var local = HoraLocal(ahora, sede.ZonaHoraria);
        var hoy = DateOnly.FromDateTime(local);
        switch (visita.Estado)
        {
            case Ingreso: return new([ "La visita ya registró su entrada." ], null, null);
            case Finalizada: return new([ "La visita ya finalizó." ], null, null);
            case Rechazada or Cancelada or Vencida: return new([ $"La visita está {visita.Estado.ToLowerInvariant()} y no habilita el ingreso." ], null, null);
        }

        var autorizacion = visita.IdAutorizacion is int idAutorizacion
            ? await Db.Autorizaciones.AsNoTracking().SingleOrDefaultAsync(x => x.IdAutorizacion == idAutorizacion, ct)
            : null;
        var derivado = EstadoDerivado(visita, autorizacion, hoy);

        var persona = await Db.Personas.AsNoTracking().Where(x => x.IdPersona == visita.IdPersona).Select(x => x.Activo).SingleAsync(ct);
        if (!persona) motivos.Add("La persona está inactiva.");
        if (!sede.Activo) motivos.Add("La sede está inactiva.");
        if (!await Db.Areas.AnyAsync(x => x.IdArea == visita.IdArea && x.Activo, ct)) motivos.Add("El destino está inactivo.");
        motivos.AddRange(MotivosVisita(visita, autorizacion is not null, visita.PoliticaAplicada is not null && await PoliticaSinAutorizacionAsync(ct), hoy));
        if (autorizacion is not null)
        {
            var horarios = await Db.AutorizacionesHorarios.AsNoTracking().Where(x => x.IdAutorizacion == autorizacion.IdAutorizacion).ToListAsync(ct);
            var zonas = await Db.AutorizacionesZonas.AsNoTracking().Where(x => x.IdAutorizacion == autorizacion.IdAutorizacion).Select(x => x.IdZona).ToListAsync(ct);
            motivos.AddRange(MotivosAutorizacion(autorizacion, horarios, zonas, visita, zonasPunto, local));
        }
        var abierta = await Db.Permanencias.AsNoTracking()
            .Where(x => x.IdPersona == visita.IdPersona && x.IdSede == visita.IdSede && x.Estado == "Abierta" && x.IdVisita != visita.IdVisita)
            .Select(x => (int?)x.IdVisita).FirstOrDefaultAsync(ct);
        if (abierta is int otra) motivos.Add($"La persona ya está dentro de esta sede por la visita #{otra}; registra antes su salida.");
        return new(motivos.Distinct().ToList(), autorizacion, derivado);
    }

    private static string? ValidarVinculo(Autorizacion? autorizacion, int idPersona, int idSede, int idArea, DateOnly hoy)
    {
        if (autorizacion is null) return "La autorización no existe en tu organización.";
        if (autorizacion.IdPersona != idPersona) return "La autorización corresponde a otra persona.";
        if (autorizacion.IdSede != idSede) return "La autorización no es para la sede de la visita.";
        if (autorizacion.IdArea != idArea) return "La autorización no es para el destino de la visita.";
        if (autorizacion.Estado is not (AutorizacionPendiente or AutorizacionAprobada))
            return $"La autorización está {autorizacion.Estado.ToLowerInvariant()} y no puede habilitar visitas.";
        if (autorizacion.FechaFin is DateOnly fin && fin < hoy) return "La autorización ya venció.";
        return null;
    }

    // ---------- Alcance y apoyo ----------

    /// <summary>Puntos de acceso asignados; null = sin restricción por punto.</summary>
    private async Task<List<int>?> PuntosAlcanceAsync(CancellationToken ct)
    {
        var membresia = await MembresiaPropiaAsync(ct);
        if (membresia is null) return null;
        var puntos = await Db.MembresiasPuntosAcceso.AsNoTracking().Where(x => x.IdMembresia == membresia).Select(x => x.IdPuntoAcceso).ToListAsync(ct);
        return puntos.Count == 0 ? null : puntos;
    }

    private async Task<IQueryable<Visita>> VisitasEnAlcanceAsync(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var query = Db.Visitas.AsNoTracking();
        return alcance is null ? query : query.Where(x => alcance.Contains(x.IdSede));
    }

    private async Task<bool> PoliticaSinAutorizacionAsync(CancellationToken ct) =>
        await Db.ParametrosOrganizacion.AsNoTracking().AnyAsync(x => x.Clave == ParametroSinAutorizacion && x.Valor.ToLower() == "true", ct);

    private Task<string> ZonaHorariaAsync(int idSede, CancellationToken ct) =>
        Db.Sedes.Where(x => x.IdSede == idSede).Select(x => x.ZonaHoraria).SingleAsync(ct);

    /// <summary>Bloquea la fila (SELECT … FOR UPDATE) y la carga con seguimiento. Requiere una transacción abierta.</summary>
    private async Task<Visita?> BloquearVisitaAsync(int id, CancellationToken ct)
    {
        await Db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM public.visita WHERE id_organizacion = {OrganizacionActiva} AND id_visita = {id} FOR UPDATE", ct);
        return await Db.Visitas.SingleOrDefaultAsync(x => x.IdVisita == id, ct);
    }

    private void Tocar(Visita visita)
    {
        visita.Version++;
        visita.IdUsuarioActualiza = User.IdUsuario();
        visita.ActualizadoEn = DateTime.UtcNow;
    }

    private static object Resumen(Visita x) => new
    {
        x.Estado, x.IdPersona, x.IdSede, x.IdArea, x.IdMembresiaResponsable, x.IdAutorizacion, x.PoliticaAplicada,
        x.FechaEsperada, x.HoraEsperadaInicio, x.HoraEsperadaFin, x.MotivoCierre, x.ExcepcionTipoAcceso, x.MotivoExcepcion,
    };

    private IQueryable<VisitaListaDto> Proyectar(IQueryable<Visita> origen) =>
        from v in origen
        join p in Db.Personas on v.IdPersona equals p.IdPersona
        join s in Db.Sedes on v.IdSede equals s.IdSede
        join a in Db.Areas on v.IdArea equals a.IdArea
        select new VisitaListaDto
        {
            IdVisita = v.IdVisita, IdPersona = p.IdPersona, Persona = p.Nombres + " " + p.Apellidos, DocumentoPersona = p.NumeroDocumento,
            PersonaActiva = p.Activo, ExcepcionTipoAcceso = v.ExcepcionTipoAcceso, MotivoExcepcion = v.MotivoExcepcion, IdSede = s.IdSede, Sede = s.Nombre, IdArea = a.IdArea, Area = a.Nombre,
            IdMembresiaResponsable = v.IdMembresiaResponsable,
            Responsable = Db.Usuarios.Where(u => Db.MembresiasOrganizacion.Any(m => m.IdMembresia == v.IdMembresiaResponsable && m.IdUsuario == u.IdUsuario)).Select(u => u.Correo).FirstOrDefault(),
            Motivo = v.Motivo, FechaEsperada = v.FechaEsperada, HoraEsperadaInicio = v.HoraEsperadaInicio, HoraEsperadaFin = v.HoraEsperadaFin,
            IdAutorizacion = v.IdAutorizacion, PoliticaAplicada = v.PoliticaAplicada, Estado = v.Estado, CreadoEn = v.CreadoEn,
            EntradaEn = Db.Permanencias.Where(x => x.IdVisita == v.IdVisita).Select(x => (DateTime?)x.AbiertaEn).FirstOrDefault(),
            SalidaEn = Db.Permanencias.Where(x => x.IdVisita == v.IdVisita).Select(x => x.CerradaEn).FirstOrDefault(),
            ZonaHoraria = s.ZonaHoraria,
        };

    private async Task<VisitaDetalleDto?> DetalleAsync(int id, CancellationToken ct)
    {
        var lista = await Proyectar((await VisitasEnAlcanceAsync(ct)).Where(x => x.IdVisita == id)).SingleOrDefaultAsync(ct);
        if (lista is null) return null;
        var visita = await Db.Visitas.AsNoTracking().SingleAsync(x => x.IdVisita == id, ct);
        var sede = await Db.Sedes.AsNoTracking().SingleAsync(x => x.IdSede == visita.IdSede, ct);
        var detalle = new VisitaDetalleDto
        {
            Visita = lista, Observacion = visita.Observacion, MotivoCierre = visita.MotivoCierre, ActualizadaEn = visita.ActualizadoEn,
            RegistradaPor = await Db.Usuarios.Where(u => u.IdUsuario == visita.IdUsuarioRegistra).Select(u => u.Correo).FirstOrDefaultAsync(ct),
            ActualizadaPor = await Db.Usuarios.Where(u => u.IdUsuario == visita.IdUsuarioActualiza).Select(u => u.Correo).FirstOrDefaultAsync(ct),
            Permanencia = await Db.Permanencias.AsNoTracking().Where(x => x.IdVisita == id).Select(x => new PermanenciaVisitaDto
            {
                IdPermanencia = x.IdPermanencia, Estado = x.Estado, AbiertaEn = x.AbiertaEn, CerradaEn = x.CerradaEn, DuracionSegundos = x.DuracionSegundos,
            }).SingleOrDefaultAsync(ct),
        };
        if (visita.IdAutorizacion is int idAutorizacion)
        {
            detalle.Autorizacion = await Db.Autorizaciones.AsNoTracking().Where(x => x.IdAutorizacion == idAutorizacion).Select(x => new AutorizacionVisitaDto
            {
                IdAutorizacion = x.IdAutorizacion, Estado = x.Estado, Modalidad = x.Modalidad, FechaInicio = x.FechaInicio, FechaFin = x.FechaFin,
                CupoIngresos = x.CupoIngresos, IngresosConsumidos = x.IngresosConsumidos, IdSede = x.IdSede, IdArea = x.IdArea,
                Horarios = Db.AutorizacionesHorarios.Where(h => h.IdAutorizacion == x.IdAutorizacion).OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
                    .Select(h => new HorarioAutorizacionDto { DiaSemana = h.DiaSemana, HoraInicio = h.HoraInicio, HoraFin = h.HoraFin }).ToList(),
                Zonas = (from az in Db.AutorizacionesZonas join z in Db.Zonas on az.IdZona equals z.IdZona where az.IdAutorizacion == x.IdAutorizacion select z.Nombre).ToList(),
            }).SingleOrDefaultAsync(ct);
        }
        // El historial de movimientos es un permiso aparte (ControlAccesos.Consultar).
        var propios = await AccesosPropiosAsync(ct);
        if (propios.TryGetValue(CodigosModulo.ControlAccesos, out var control) && control.Consultar)
        {
            detalle.Movimientos = await (
                from m in Db.MovimientosAcceso.AsNoTracking()
                join pa in Db.PuntosAcceso on m.IdPuntoAcceso equals pa.IdPuntoAcceso
                join z in Db.Zonas on m.IdZona equals z.IdZona
                where m.IdVisita == id
                orderby m.OcurridoEn, m.IdMovimiento
                select new MovimientoVisitaDto
                {
                    IdMovimiento = m.IdMovimiento, TipoMovimiento = m.TipoMovimiento, Resultado = m.Resultado, PuntoAcceso = pa.Nombre, Zona = z.Nombre,
                    Operador = Db.Usuarios.Where(u => u.IdUsuario == m.IdUsuarioRegistra).Select(u => u.Correo).FirstOrDefault(),
                    OcurridoEn = m.OcurridoEn, Observacion = m.Observacion,
                }).ToListAsync(ct);
        }
        var evaluacion = await EvaluarIngresoAsync(visita, sede, null, DateTime.UtcNow, ct);
        detalle.Evaluacion = new EvaluacionIngresoDto
        {
            PuedeIngresar = evaluacion.Motivos.Count == 0,
            PuedeSalir = visita.Estado == Ingreso && detalle.Permanencia?.Estado == "Abierta",
            Motivos = visita.Estado == Ingreso ? [] : evaluacion.Motivos,
        };
        return detalle;
    }
}
