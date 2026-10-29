using ApiConecsia.Helpers;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ApiConecsia.Services.ReglasAutorizacion;

namespace ApiConecsia.Controllers;

/// <summary>
/// Solicitudes de autorización de ingreso de personas (RF-09/RF-10, PRD §6.6) desde el módulo Control de accesos.
/// La persona ya existe (se llega desde «Gestionar acceso» en Personas): aquí no se crea ni se duplica.
/// Ciclo: toda solicitud nace Pendiente (no habilita ingresos) → Aprobada | Rechazada. Aprobada ≠ vigente: la entrada
/// (Visitas) exige además fecha, hora, días, sede, destino, zonas y cupo. Una Rechazada se conserva en el historial.
/// Permisos (Propuesta, D-40): consultar = Control de accesos · Consultar; solicitar = Control de accesos · Añadir;
/// aprobar/rechazar = Autorizaciones · Editar (acción que se concede al perfil Administrador de organización).
/// Organización: siempre la activa validada en servidor; alcance por sede de la membresía (D-34).
/// </summary>
[Route("api/autorizaciones-acceso")]
public sealed class AutorizacionesAccesoController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private const string Entidad = "Autorizacion";

    // ---------- Consulta ----------

    [HttpGet]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<AutorizacionAccesoDto>>> Listar(
        [FromQuery] int? idPersona = null, [FromQuery] string? estado = null, [FromQuery] string? modalidad = null,
        [FromQuery] int? idSede = null, [FromQuery] string? buscar = null, CancellationToken ct = default)
    {
        var query = await EnAlcanceAsync(ct);
        if (idPersona is int persona) query = query.Where(x => x.IdPersona == persona);
        if (idSede is int sede) query = query.Where(x => x.IdSede == sede);
        if (estado is not null && Estados.Contains(estado)) query = query.Where(x => x.Estado == estado);
        if (modalidad is not null && Modalidades.Contains(modalidad)) query = query.Where(x => x.Modalidad == modalidad);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x => Db.Personas.Any(p => p.IdPersona == x.IdPersona
                && (EF.Functions.ILike(p.Nombres + " " + p.Apellidos, texto) || EF.Functions.ILike(p.NumeroDocumento, texto))));
        }
        var filas = await Proyectar(query.OrderByDescending(x => x.CreadoEn).ThenByDescending(x => x.IdAutorizacion)).Take(500).ToListAsync(ct);
        await CompletarAsync(filas, false, ct);
        return Ok(filas);
    }

    [HttpGet("{id:int}")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<AutorizacionAccesoDto>> Obtener(int id, CancellationToken ct)
    {
        var dto = await DetalleAsync(id, ct);
        return dto is null ? NoEncontrado("La autorización no existe o no está en tu alcance.") : Ok(dto);
    }

    /// <summary>Sedes, destinos, responsables y zonas del alcance, y la política de la modalidad Permanente.</summary>
    [HttpGet("opciones")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<OpcionesAutorizacionAccesoDto>> Opciones(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
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
        return Ok(new OpcionesAutorizacionAccesoDto
        {
            Sedes = sedes,
            Areas = await Db.Areas.AsNoTracking().Where(x => x.Activo && idsSede.Contains(x.IdSede)).OrderBy(x => x.Nombre)
                .Select(x => new OpcionAreaDto { Id = x.IdArea, IdSede = x.IdSede, Nombre = x.Nombre }).ToListAsync(ct),
            Responsables = responsables,
            Zonas = await Db.Zonas.AsNoTracking().Where(x => x.Activo && idsSede.Contains(x.IdSede)).OrderBy(x => x.Nombre)
                .Select(x => new ZonaAutorizacionDto { Id = x.IdZona, IdSede = x.IdSede, Nombre = x.Nombre }).ToListAsync(ct),
            PermitePermanente = await PermitePermanenteAsync(ct),
            ExigeZonas = await ExigeZonasAsync(ct),
            IdMembresiaPropia = await MembresiaPropiaAsync(ct),
        });
    }

    // ---------- Solicitud ----------

    /// <summary>Crea la solicitud en estado Pendiente. Solicitante = usuario autenticado; organización = la activa.</summary>
    [HttpPost]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Anadir)]
    public async Task<ActionResult<AutorizacionAccesoDto>> Solicitar([FromBody] AutorizacionAccesoEntrada request, CancellationToken ct)
    {
        var persona = await Db.Personas.AsNoTracking().SingleOrDefaultAsync(x => x.IdPersona == request.IdPersona, ct);
        if (persona is null) return Invalido("La persona no existe en tu organización.");
        if (!persona.Activo) return Invalido("La persona está inactiva: no se puede solicitar su acceso.");
        var sede = await Db.Sedes.AsNoTracking().SingleOrDefaultAsync(x => x.IdSede == request.IdSede && x.Activo, ct);
        if (sede is null) return Invalido("La sede no existe en tu organización o está inactiva.");
        if (!EnAlcance(await SedesAlcanceAsync(ct), sede.IdSede)) return Prohibido("La sede no está en tu alcance.");
        if (!await Db.Areas.AnyAsync(x => x.IdArea == request.IdArea && x.IdSede == sede.IdSede && x.Activo, ct))
            return Invalido("El destino no pertenece a la sede o está inactivo.");
        if (await ValidarResponsableAsync(request.IdArea, request.IdMembresiaResponsable, ct) is { } responsable) return Invalido(responsable);

        var ahora = HoraLocal(DateTime.UtcNow, sede.ZonaHoraria);
        var (condiciones, invalida) = ApiConecsia.Services.ReglasAutorizacion.Normalizar(request, ahora);
        if (invalida is not null) return Invalido(invalida);
        if (condiciones!.Modalidad == Permanente && !await PermitePermanenteAsync(ct))
            return Conflicto("La política de la organización no permite solicitar autorizaciones permanentes.");

        var zonas = (request.Zonas ?? []).Distinct().ToList();
        if (await ValidarZonasAsync(zonas, sede.IdSede, ct) is { } zonasInvalidas) return zonasInvalidas;

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        // Clave negativa: no se cruza con el bloqueo de autorizaciones de objeto (org * 2 + tipo). Serializa solicitudes concurrentes de la misma persona para que la verificación de incompatibles sea fiable (doble envío).
        await Db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-OrganizacionActiva}, {persona.IdPersona})", ct);
        var hoy = DateOnly.FromDateTime(ahora);
        var cruce = await Db.Autorizaciones.AsNoTracking()
            .Where(x => x.IdPersona == persona.IdPersona && x.IdSede == sede.IdSede && x.IdArea == request.IdArea
                && (x.Estado == Pendiente || x.Estado == Aprobada)
                && (x.FechaFin == null || x.FechaFin >= hoy)
                && (x.CupoIngresos == null || x.IngresosConsumidos < x.CupoIngresos)
                && (condiciones.FechaFin == null || x.FechaInicio <= condiciones.FechaFin)
                && (x.FechaFin == null || x.FechaFin >= condiciones.FechaInicio))
            .Select(x => new { x.IdAutorizacion, x.Estado, x.Modalidad }).FirstOrDefaultAsync(ct);
        if (cruce is not null)
            return Conflicto($"La persona ya tiene la autorización #{cruce.IdAutorizacion} ({TextoModalidad(cruce.Modalidad)}, {(cruce.Estado == Pendiente ? "pendiente de aprobación" : "aprobada")}) para esta sede y destino en un periodo que se cruza.");

        var autorizacion = new Autorizacion
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = persona.IdPersona, IdSede = sede.IdSede, IdArea = request.IdArea,
            IdMembresiaResponsable = request.IdMembresiaResponsable, IdUsuarioCrea = User.IdUsuario()!.Value,
            Modalidad = condiciones.Modalidad, FechaInicio = condiciones.FechaInicio, FechaFin = condiciones.FechaFin,
            HoraInicio = condiciones.HoraInicio, HoraFin = condiciones.HoraFin, CupoIngresos = condiciones.Modalidad == Unica ? 1 : null,
            Motivo = Limpiar(request.Motivo), Observacion = Limpiar(request.Observacion), Estado = Pendiente, Version = 1, CreadoEn = DateTime.UtcNow,
        };
        Db.Autorizaciones.Add(autorizacion);
        await Db.SaveChangesAsync(ct);
        foreach (var dia in condiciones.Dias)
            Db.AutorizacionesHorarios.Add(new AutorizacionHorario
            {
                IdOrganizacion = OrganizacionActiva, IdAutorizacion = autorizacion.IdAutorizacion, DiaSemana = dia,
                HoraInicio = condiciones.HorarioInicio!.Value, HoraFin = condiciones.HorarioFin!.Value,
            });
        foreach (var zona in zonas)
            Db.AutorizacionesZonas.Add(new AutorizacionZona { IdOrganizacion = OrganizacionActiva, IdAutorizacion = autorizacion.IdAutorizacion, IdZona = zona });
        RegistrarTransicion(autorizacion, "", autorizacion.Motivo);
        Auditar(OrganizacionActiva, "Solicitar", Entidad, autorizacion.IdAutorizacion.ToString(), null,
            new { Resumen = Resumen(autorizacion), Dias = condiciones.Dias, condiciones.HorarioInicio, condiciones.HorarioFin, Zonas = zonas });
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Created($"/api/autorizaciones-acceso/{autorizacion.IdAutorizacion}", await DetalleAsync(autorizacion.IdAutorizacion, ct));
    }

    // ---------- Zonas permitidas ----------

    /// <summary>
    /// Reemplaza las zonas a las que se restringe una solicitud pendiente (vacío = sin restricción, si la política lo admite).
    /// Una vez decidida no se modifica: cambiar el alcance de una autorización aprobada exigiría una nueva aprobación.
    /// </summary>
    [HttpPut("{id:int}/zonas")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Editar)]
    public async Task<ActionResult<AutorizacionAccesoDto>> EditarZonas(int id, [FromBody] ZonasAutorizacionEntrada request, CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        await Db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM public.autorizacion WHERE id_organizacion = {OrganizacionActiva} AND id_autorizacion = {id} FOR UPDATE", ct);
        var autorizacion = await Db.Autorizaciones.SingleOrDefaultAsync(x => x.IdAutorizacion == id, ct);
        if (autorizacion is null || !EnAlcance(alcance, autorizacion.IdSede)) return NoEncontrado("La autorización no existe o no está en tu alcance.");
        if (autorizacion.Estado != Pendiente)
            return Conflicto($"Solo se modifican las zonas de solicitudes pendientes; esta autorización está {autorizacion.Estado.ToLowerInvariant()}.");
        var zonas = request.Zonas.Distinct().ToList();
        if (await ValidarZonasAsync(zonas, autorizacion.IdSede, ct) is { } invalidas) return invalidas;

        var actuales = await Db.AutorizacionesZonas.Where(x => x.IdAutorizacion == id).ToListAsync(ct);
        var anteriores = actuales.Select(x => x.IdZona).Order().ToList();
        Db.AutorizacionesZonas.RemoveRange(actuales.Where(x => !zonas.Contains(x.IdZona)));
        foreach (var zona in zonas.Except(anteriores))
            Db.AutorizacionesZonas.Add(new AutorizacionZona { IdOrganizacion = OrganizacionActiva, IdAutorizacion = id, IdZona = zona });
        autorizacion.Version++;
        Auditar(OrganizacionActiva, "EditarZonas", Entidad, id.ToString(), new { Zonas = anteriores }, new { Zonas = zonas.Order().ToList() });
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Ok(await DetalleAsync(id, ct));
    }

    // ---------- Decisión ----------

    [HttpPost("{id:int}/aprobar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<AutorizacionAccesoDto>> Aprobar(int id, [FromBody] DecisionAutorizacionEntrada request, CancellationToken ct) =>
        DecidirAsync(id, Aprobada, "Aprobar", Limpiar(request.Motivo), ct);

    [HttpPost("{id:int}/rechazar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<AutorizacionAccesoDto>> Rechazar(int id, [FromBody] DecisionAutorizacionEntrada request, CancellationToken ct) =>
        Limpiar(request.Motivo) is { } motivo
            ? DecidirAsync(id, Rechazada, "Rechazar", motivo, ct)
            : Task.FromResult<ActionResult<AutorizacionAccesoDto>>(Invalido("Indique el motivo del rechazo."));

    private async Task<ActionResult<AutorizacionAccesoDto>> DecidirAsync(int id, string hacia, string accion, string? motivo, CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var autorizacion = await Db.Autorizaciones.SingleOrDefaultAsync(x => x.IdAutorizacion == id, ct);
        if (autorizacion is null || !EnAlcance(alcance, autorizacion.IdSede)) return NoEncontrado("La autorización no existe o no está en tu alcance.");
        if (autorizacion.Estado != Pendiente)
            return Conflicto($"Solo se deciden solicitudes pendientes; esta autorización está {autorizacion.Estado.ToLowerInvariant()}.");

        if (hacia == Aprobada)
        {
            var zona = await Db.Sedes.Where(x => x.IdSede == autorizacion.IdSede).Select(x => x.ZonaHoraria).SingleAsync(ct);
            if (Vencio(autorizacion, HoraLocal(DateTime.UtcNow, zona))) return Conflicto("La solicitud venció antes de aprobarse: solicita una nueva con fechas vigentes.");
            if (!await Db.Personas.AnyAsync(x => x.IdPersona == autorizacion.IdPersona && x.Activo, ct)) return Conflicto("No se puede aprobar: la persona está inactiva.");
        }

        var anterior = autorizacion.Estado;
        autorizacion.Estado = hacia;
        autorizacion.IdUsuarioDecide = User.IdUsuario()!.Value;
        autorizacion.DecididoEn = DateTime.UtcNow;
        autorizacion.MotivoDecision = motivo;
        autorizacion.Version++;
        RegistrarTransicion(autorizacion, anterior, motivo);
        Auditar(OrganizacionActiva, accion, Entidad, id.ToString(), new { Estado = anterior }, Resumen(autorizacion));
        try
        {
            // version es token de concurrencia: si otra decisión se guardó primero, esta falla y no registra una segunda transición.
            await Db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            Db.ChangeTracker.Clear();
            return Conflicto("Otro usuario decidió esta solicitud al mismo tiempo. Consulta su estado actual.");
        }
        return Ok(await DetalleAsync(id, ct));
    }

    // ---------- Apoyo ----------

    private async Task<IQueryable<Autorizacion>> EnAlcanceAsync(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var query = Db.Autorizaciones.AsNoTracking();
        return alcance is null ? query : query.Where(x => alcance.Contains(x.IdSede));
    }

    private async Task<bool> ExigeZonasAsync(CancellationToken ct) =>
        await Db.ParametrosOrganizacion.AsNoTracking().AnyAsync(x => x.Clave == ParametroExigeZonas && x.Valor.ToLower() == "true", ct);

    /// <summary>Zonas activas de la organización activa (filtro global) y de la sede de la autorización; obligatorias si la política lo exige.</summary>
    private async Task<ObjectResult?> ValidarZonasAsync(List<int> zonas, int idSede, CancellationToken ct)
    {
        if (zonas.Count == 0)
            return await ExigeZonasAsync(ct) ? Invalido("La política de la organización exige restringir la autorización a una o más zonas.") : null;
        if (await Db.Zonas.CountAsync(x => zonas.Contains(x.IdZona) && x.IdSede == idSede && x.Activo, ct) != zonas.Count)
            return Invalido("Alguna zona no pertenece a la sede de la autorización o está inactiva.");
        return null;
    }

    private async Task<bool> PermitePermanenteAsync(CancellationToken ct) =>
        await Db.ParametrosOrganizacion.AsNoTracking().AnyAsync(x => x.Clave == ParametroPermitePermanente && x.Valor.ToLower() == "true", ct);

    private void RegistrarTransicion(Autorizacion autorizacion, string anterior, string? observacion) =>
        Db.AutorizacionesTransiciones.Add(new AutorizacionTransicion
        {
            IdOrganizacion = autorizacion.IdOrganizacion, IdAutorizacion = autorizacion.IdAutorizacion, EstadoAnterior = anterior,
            EstadoNuevo = autorizacion.Estado, IdUsuarioActor = User.IdUsuario(), Observacion = observacion, OcurridoEn = DateTime.UtcNow,
        });

    private static object Resumen(Autorizacion x) => new
    {
        x.Estado, x.IdPersona, x.IdSede, x.IdArea, x.IdMembresiaResponsable, x.Modalidad, x.FechaInicio, x.FechaFin, x.HoraInicio, x.HoraFin,
        x.CupoIngresos, x.IdUsuarioCrea, x.IdUsuarioDecide, x.DecididoEn, x.MotivoDecision,
    };

    private IQueryable<AutorizacionAccesoDto> Proyectar(IQueryable<Autorizacion> origen) =>
        from a in origen
        join p in Db.Personas on a.IdPersona equals p.IdPersona
        join s in Db.Sedes on a.IdSede equals s.IdSede
        join ar in Db.Areas on a.IdArea equals ar.IdArea
        select new AutorizacionAccesoDto
        {
            IdAutorizacion = a.IdAutorizacion, IdPersona = p.IdPersona, Persona = p.Nombres + " " + p.Apellidos, DocumentoPersona = p.NumeroDocumento,
            PersonaActiva = p.Activo, IdSede = s.IdSede, Sede = s.Nombre, IdArea = ar.IdArea, Area = ar.Nombre, IdMembresiaResponsable = a.IdMembresiaResponsable,
            Responsable = Db.Usuarios.Where(u => Db.MembresiasOrganizacion.Any(m => m.IdMembresia == a.IdMembresiaResponsable && m.IdUsuario == u.IdUsuario)).Select(u => u.Correo).FirstOrDefault(),
            Modalidad = a.Modalidad, FechaInicio = a.FechaInicio, FechaFin = a.FechaFin, HoraInicio = a.HoraInicio, HoraFin = a.HoraFin,
            CupoIngresos = a.CupoIngresos, IngresosConsumidos = a.IngresosConsumidos, Motivo = a.Motivo, Observacion = a.Observacion, Estado = a.Estado,
            SolicitadoPor = Db.Usuarios.Where(u => u.IdUsuario == a.IdUsuarioCrea).Select(u => u.Correo).FirstOrDefault(), SolicitadoEn = a.CreadoEn,
            DecididoPor = Db.Usuarios.Where(u => u.IdUsuario == a.IdUsuarioDecide).Select(u => u.Correo).FirstOrDefault(), DecididoEn = a.DecididoEn,
            MotivoDecision = a.MotivoDecision, ZonaHoraria = s.ZonaHoraria,
        };

    /// <summary>Horarios (para la vigencia y el detalle) y, en el detalle, zonas e historial.</summary>
    private async Task CompletarAsync(List<AutorizacionAccesoDto> filas, bool detalle, CancellationToken ct)
    {
        var ids = filas.Select(x => x.IdAutorizacion).ToList();
        var horarios = await Db.AutorizacionesHorarios.AsNoTracking().Where(x => ids.Contains(x.IdAutorizacion))
            .OrderBy(x => x.DiaSemana).ThenBy(x => x.HoraInicio).ToListAsync(ct);
        foreach (var fila in filas)
        {
            fila.Horarios = horarios.Where(h => h.IdAutorizacion == fila.IdAutorizacion)
                .Select(h => new HorarioAutorizacionDto { DiaSemana = h.DiaSemana, HoraInicio = h.HoraInicio, HoraFin = h.HoraFin }).ToList();
            fila.Vigencia = Vigencia(fila.Estado, fila.Modalidad, fila.FechaInicio, fila.FechaFin, fila.HoraInicio, fila.HoraFin,
                fila.CupoIngresos, fila.IngresosConsumidos, HoraLocal(DateTime.UtcNow, fila.ZonaHoraria));
            if (!detalle) continue;
            fila.Zonas = await (from az in Db.AutorizacionesZonas.AsNoTracking()
                                join z in Db.Zonas on az.IdZona equals z.IdZona
                                where az.IdAutorizacion == fila.IdAutorizacion
                                orderby z.Nombre
                                select new OpcionCatalogoDto { Id = z.IdZona, Nombre = z.Nombre }).ToListAsync(ct);
            fila.Historial = await (
                from t in Db.AutorizacionesTransiciones.AsNoTracking()
                where t.IdAutorizacion == fila.IdAutorizacion
                orderby t.OcurridoEn, t.IdTransicion
                select new TransicionAutorizacionObjetoDto
                {
                    IdTransicion = t.IdTransicion, EstadoAnterior = t.EstadoAnterior == "" ? null : t.EstadoAnterior, EstadoNuevo = t.EstadoNuevo,
                    Motivo = t.Observacion, OcurridoEn = t.OcurridoEn,
                    Actor = Db.Usuarios.Where(u => u.IdUsuario == t.IdUsuarioActor).Select(u => u.Correo).FirstOrDefault(),
                }).ToListAsync(ct);
        }
    }

    private async Task<AutorizacionAccesoDto?> DetalleAsync(int id, CancellationToken ct)
    {
        var fila = await Proyectar((await EnAlcanceAsync(ct)).Where(x => x.IdAutorizacion == id)).SingleOrDefaultAsync(ct);
        if (fila is null) return null;
        await CompletarAsync([fila], true, ct);
        return fila;
    }
}
