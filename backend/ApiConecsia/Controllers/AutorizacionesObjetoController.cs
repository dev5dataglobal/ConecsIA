using ApiConecsia.Helpers;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

/// <summary>
/// Autorizaciones de ingreso de vehículos y dispositivos (RF-16/RF-17, ciclo de ADR-009). Ciclo propio y separado
/// de la autorización de la persona: aprobar un objeto no autoriza el ingreso de la persona; se vincula a ella solo
/// por trazabilidad (<c>id_autorizacion</c> opcional).
/// Estados persistidos: Pendiente → Aprobada | Rechazada; Aprobada → Revocada. «Vencida» y «Vigente» se calculan.
/// Permisos (módulos existentes): consultar = Autorizaciones.Consultar; solicitar = Autorizaciones.Añadir + Añadir en
/// el módulo del objeto; decidir = Autorizaciones.Editar + acceso al módulo del objeto. Solicitante y aprobador salen
/// de la identidad autenticada; cada transición queda en un historial de solo inserción y en la auditoría.
/// </summary>
[Route("api/autorizaciones-objeto")]
public sealed class AutorizacionesObjetoController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private const string Entidad = "AutorizacionObjeto";
    private const string Vehiculo = "Vehiculo";
    private const string Dispositivo = "Dispositivo";
    private const string Pendiente = "Pendiente", Aprobada = "Aprobada", Rechazada = "Rechazada", Revocada = "Revocada";

    [HttpGet]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<AutorizacionObjetoDto>>> Listar(
        [FromQuery] int? idPersona = null, [FromQuery] int? idSede = null, [FromQuery] string? estado = null, [FromQuery] string? tipoObjeto = null,
        [FromQuery] DateOnly? desde = null, [FromQuery] DateOnly? hasta = null, [FromQuery] string? buscar = null, CancellationToken ct = default)
    {
        var (vehiculos, dispositivos) = await ModulosObjetoAsync(ct);
        var query = Db.AutorizacionesObjeto.AsNoTracking()
            .Where(x => (vehiculos && x.TipoObjeto == Vehiculo) || (dispositivos && x.TipoObjeto == Dispositivo));
        if (idPersona is int persona) query = query.Where(x => x.IdPersona == persona);
        if (idSede is int sede) query = query.Where(x => x.IdSede == sede);
        if (estado is Pendiente or Aprobada or Rechazada or Revocada) query = query.Where(x => x.Estado == estado);
        if (tipoObjeto is Vehiculo or Dispositivo) query = query.Where(x => x.TipoObjeto == tipoObjeto);
        // Periodo: autorizaciones cuyo intervalo se cruza con [desde, hasta].
        if (desde is DateOnly d) query = query.Where(x => x.FechaFin >= d);
        if (hasta is DateOnly h) query = query.Where(x => x.FechaInicio <= h);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x =>
                Db.Personas.Any(p => p.IdPersona == x.IdPersona && (EF.Functions.ILike(p.Nombres + " " + p.Apellidos, texto) || EF.Functions.ILike(p.NumeroDocumento, texto)))
                || Db.Vehiculos.Any(v => v.IdVehiculo == x.IdVehiculo && EF.Functions.ILike(v.Placa, texto))
                || Db.Dispositivos.Any(v => v.IdDispositivo == x.IdDispositivo && EF.Functions.ILike(v.CodigoIdentificador, texto)));
        }
        var filas = await Proyectar(query.OrderByDescending(x => x.SolicitadoEn)).Take(500).ToListAsync(ct);
        foreach (var fila in filas) fila.Dto.Vigencia = Vigencia(fila.Dto, fila.ZonaHoraria);
        return Ok(filas.Select(x => x.Dto));
    }

    [HttpGet("{id:int}")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Consultar)]
    public async Task<ActionResult<AutorizacionObjetoDto>> Obtener(int id, CancellationToken ct)
    {
        var dto = await DetalleAsync(id, ct);
        if (dto is null) return NoEncontrado("La autorización no existe o no está en tu alcance.");
        var (vehiculos, dispositivos) = await ModulosObjetoAsync(ct);
        if (!(dto.TipoObjeto == Vehiculo ? vehiculos : dispositivos)) return Prohibido("Tu perfil no tiene acceso al módulo de este objeto.");
        return Ok(dto);
    }

    /// <summary>Sedes y áreas activas para el formulario.</summary>
    [HttpGet("opciones")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Consultar)]
    public async Task<ActionResult<OpcionesAutorizacionObjetoDto>> Opciones(CancellationToken ct) => Ok(new OpcionesAutorizacionObjetoDto
    {
        Sedes = await Db.Sedes.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre).Select(x => new OpcionCatalogoDto { Id = x.IdSede, Nombre = x.Nombre }).ToListAsync(ct),
        Areas = await Db.Areas.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre).Select(x => new OpcionAreaDto { Id = x.IdArea, IdSede = x.IdSede, Nombre = x.Nombre }).ToListAsync(ct),
    });

    /// <summary>Solicita la autorización: queda Pendiente hasta que un usuario facultado decida.</summary>
    [HttpPost]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Anadir)]
    public async Task<ActionResult<AutorizacionObjetoDto>> Solicitar([FromBody] AutorizacionObjetoEntrada request, CancellationToken ct)
    {
        if (request.TipoObjeto is not (Vehiculo or Dispositivo)) return Invalido("El tipo de objeto debe ser Vehiculo o Dispositivo.");
        var accesosPropios = await AccesosPropiosAsync(ct);
        var modulo = request.TipoObjeto == Vehiculo ? CodigosModulo.Vehiculos : CodigosModulo.Dispositivos;
        if (!accesosPropios.TryGetValue(modulo, out var accionesObjeto) || !accionesObjeto.Anadir)
            return Prohibido("Tu perfil no puede solicitar autorizaciones para este tipo de objeto.");

        var idObjeto = request.TipoObjeto == Vehiculo ? request.IdVehiculo : request.IdDispositivo;
        if (idObjeto is null or < 1) return Invalido(request.TipoObjeto == Vehiculo ? "Seleccione el vehículo." : "Seleccione el dispositivo.");
        var (inicio, fin) = (request.FechaInicio!.Value, request.FechaFin!.Value);
        if (fin < inicio) return Invalido("La fecha de fin no puede ser anterior a la de inicio.");
        if ((request.HoraInicio is null) != (request.HoraFin is null)) return Invalido("Indique ambas horas del horario o ninguna.");
        if (request.HoraInicio is TimeOnly hi && request.HoraFin is TimeOnly hf && hf <= hi) return Invalido("La hora de fin debe ser posterior a la de inicio.");

        var sede = await Db.Sedes.AsNoTracking().SingleOrDefaultAsync(x => x.IdSede == request.IdSede && x.Activo, ct);
        if (sede is null) return Invalido("La sede no existe en tu organización o está inactiva.");
        if (fin < Hoy(sede.ZonaHoraria)) return Invalido("El periodo ya venció: la fecha de fin es anterior a hoy.");
        if (request.IdArea is int idArea && !await Db.Areas.AnyAsync(x => x.IdArea == idArea && x.IdSede == sede.IdSede && x.Activo, ct))
            return Invalido("El área no pertenece a la sede o está inactiva.");
        if (request.IdAutorizacion is int idAutorizacion && !await Db.Autorizaciones.AnyAsync(x => x.IdAutorizacion == idAutorizacion && x.IdPersona == request.IdPersona, ct))
            return Invalido("La autorización de la persona no existe o no corresponde a esa persona.");
        if (await ValidarSujetoAsync(request.TipoObjeto, request.IdPersona, idObjeto.Value, ct) is { } invalido) return Invalido(invalido);

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        // Serializa solicitudes concurrentes del mismo objeto para que la verificación de duplicados sea fiable.
        await BloquearObjetoAsync(request.TipoObjeto, idObjeto.Value, ct);
        var hoy = Hoy(sede.ZonaHoraria);
        var duplicada = await Db.AutorizacionesObjeto.AnyAsync(x =>
            x.TipoObjeto == request.TipoObjeto && (x.IdVehiculo == idObjeto || x.IdDispositivo == idObjeto) && x.IdSede == sede.IdSede
            && (x.Estado == Pendiente || x.Estado == Aprobada) && x.FechaFin >= hoy && x.FechaInicio <= fin && x.FechaFin >= inicio, ct);
        if (duplicada) return Conflicto("Ya existe una autorización pendiente o aprobada para este objeto y sede en un periodo que se cruza.");

        var autorizacion = new AutorizacionObjeto
        {
            IdOrganizacion = OrganizacionActiva, TipoObjeto = request.TipoObjeto,
            IdVehiculo = request.TipoObjeto == Vehiculo ? idObjeto : null, IdDispositivo = request.TipoObjeto == Dispositivo ? idObjeto : null,
            IdPersona = request.IdPersona, IdSede = sede.IdSede, IdArea = request.IdArea, IdAutorizacion = request.IdAutorizacion,
            FechaInicio = inicio, FechaFin = fin, HoraInicio = request.HoraInicio, HoraFin = request.HoraFin, Motivo = Limpiar(request.Motivo),
            Estado = Pendiente, IdUsuarioSolicita = User.IdUsuario()!.Value, SolicitadoEn = DateTime.UtcNow,
        };
        Db.AutorizacionesObjeto.Add(autorizacion);
        await Db.SaveChangesAsync(ct);
        RegistrarTransicion(autorizacion, null, autorizacion.Motivo);
        Auditar(OrganizacionActiva, "Solicitar", Entidad, autorizacion.IdAutorizacionObjeto.ToString(), null, Resumen(autorizacion));
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Created($"/api/autorizaciones-objeto/{autorizacion.IdAutorizacionObjeto}", await DetalleAsync(autorizacion.IdAutorizacionObjeto, ct));
    }

    [HttpPost("{id:int}/aprobar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<AutorizacionObjetoDto>> Aprobar(int id, [FromBody] DecisionAutorizacionEntrada request, CancellationToken ct) =>
        DecidirAsync(id, Pendiente, Aprobada, "Aprobar", Limpiar(request.Motivo), ct);

    [HttpPost("{id:int}/rechazar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<AutorizacionObjetoDto>> Rechazar(int id, [FromBody] DecisionAutorizacionEntrada request, CancellationToken ct) =>
        Limpiar(request.Motivo) is { } motivo
            ? DecidirAsync(id, Pendiente, Rechazada, "Rechazar", motivo, ct)
            : Task.FromResult<ActionResult<AutorizacionObjetoDto>>(Invalido("Indique el motivo del rechazo."));

    [HttpPost("{id:int}/revocar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<AutorizacionObjetoDto>> Revocar(int id, [FromBody] DecisionAutorizacionEntrada request, CancellationToken ct) =>
        Limpiar(request.Motivo) is { } motivo
            ? DecidirAsync(id, Aprobada, Revocada, "Revocar", motivo, ct)
            : Task.FromResult<ActionResult<AutorizacionObjetoDto>>(Invalido("Indique el motivo de la revocación."));

    // ---------- Apoyo ----------

    private async Task<ActionResult<AutorizacionObjetoDto>> DecidirAsync(int id, string desde, string hacia, string accion, string? motivo, CancellationToken ct)
    {
        var autorizacion = await Db.AutorizacionesObjeto.SingleOrDefaultAsync(x => x.IdAutorizacionObjeto == id, ct);
        if (autorizacion is null) return NoEncontrado("La autorización no existe o no está en tu alcance.");
        var (vehiculos, dispositivos) = await ModulosObjetoAsync(ct);
        if (!(autorizacion.TipoObjeto == Vehiculo ? vehiculos : dispositivos)) return Prohibido("Tu perfil no tiene acceso al módulo de este objeto.");
        if (autorizacion.Estado != desde)
            return Conflicto($"No se puede {accion.ToLowerInvariant()} una autorización en estado {autorizacion.Estado}.");

        if (hacia == Aprobada)
        {
            var zona = await Db.Sedes.Where(x => x.IdSede == autorizacion.IdSede).Select(x => x.ZonaHoraria).SingleAsync(ct);
            if (autorizacion.FechaFin < Hoy(zona)) return Conflicto("La autorización venció antes de aprobarse.");
            var idObjeto = autorizacion.IdVehiculo ?? autorizacion.IdDispositivo!.Value;
            if (await ValidarSujetoAsync(autorizacion.TipoObjeto, autorizacion.IdPersona, idObjeto, ct) is { } invalido)
                return Conflicto($"No se puede aprobar: {invalido}");
        }

        var anterior = autorizacion.Estado;
        autorizacion.Estado = hacia;
        autorizacion.MotivoDecision = motivo;
        autorizacion.IdUsuarioDecide = User.IdUsuario()!.Value;
        autorizacion.DecididoEn = DateTime.UtcNow;
        RegistrarTransicion(autorizacion, anterior, motivo);
        Auditar(OrganizacionActiva, accion, Entidad, id.ToString(), new { Estado = anterior }, Resumen(autorizacion));
        try
        {
            // xmin: si otra decisión se guardó primero, esta falla y no se registra una segunda transición.
            await Db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            Db.ChangeTracker.Clear();
            return Conflicto("Otro usuario decidió esta autorización al mismo tiempo. Consulta su estado actual.");
        }
        return Ok(await DetalleAsync(id, ct));
    }

    /// <summary>Persona activa y objeto activo asociado a ella (vehículo con vínculo activo; dispositivo bajo su responsabilidad).</summary>
    private async Task<string?> ValidarSujetoAsync(string tipoObjeto, int idPersona, int idObjeto, CancellationToken ct)
    {
        var persona = await Db.Personas.AsNoTracking().Where(x => x.IdPersona == idPersona).Select(x => (bool?)x.Activo).SingleOrDefaultAsync(ct);
        if (persona is null) return "La persona no existe en tu organización.";
        if (persona == false) return "La persona está inactiva.";
        if (tipoObjeto == Vehiculo)
        {
            var vehiculo = await Db.Vehiculos.AsNoTracking().Where(x => x.IdVehiculo == idObjeto).Select(x => (bool?)x.Activo).SingleOrDefaultAsync(ct);
            if (vehiculo is null) return "El vehículo no existe en tu organización.";
            if (vehiculo == false) return "El vehículo está inactivo.";
            if (!await Db.PersonasVehiculos.AnyAsync(x => x.IdPersona == idPersona && x.IdVehiculo == idObjeto && x.Activo, ct))
                return "El vehículo no está asociado a la persona.";
            return null;
        }
        var dispositivo = await Db.Dispositivos.AsNoTracking().Where(x => x.IdDispositivo == idObjeto).Select(x => new { x.IdPersonaResponsable, x.Activo }).SingleOrDefaultAsync(ct);
        if (dispositivo is null) return "El dispositivo no existe en tu organización.";
        if (dispositivo.IdPersonaResponsable != idPersona) return "El dispositivo no pertenece a la persona.";
        if (!dispositivo.Activo) return "El dispositivo está inactivo.";
        return null;
    }

    private Task BloquearObjetoAsync(string tipoObjeto, int idObjeto, CancellationToken ct) =>
        Db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({OrganizacionActiva * 2 + (tipoObjeto == Vehiculo ? 0 : 1)}, {idObjeto})", ct);

    private void RegistrarTransicion(AutorizacionObjeto autorizacion, string? anterior, string? motivo) =>
        Db.AutorizacionesObjetoTransiciones.Add(new AutorizacionObjetoTransicion
        {
            IdOrganizacion = autorizacion.IdOrganizacion, IdAutorizacionObjeto = autorizacion.IdAutorizacionObjeto, IdSede = autorizacion.IdSede,
            EstadoAnterior = anterior, EstadoNuevo = autorizacion.Estado, Motivo = motivo, IdUsuarioActor = User.IdUsuario()!.Value, OcurridoEn = DateTime.UtcNow,
        });

    private static object Resumen(AutorizacionObjeto x) => new
    {
        x.Estado, x.TipoObjeto, x.IdVehiculo, x.IdDispositivo, x.IdPersona, x.IdSede, x.IdArea, x.IdAutorizacion,
        x.FechaInicio, x.FechaFin, x.HoraInicio, x.HoraFin, x.MotivoDecision,
    };

    private async Task<(bool Vehiculos, bool Dispositivos)> ModulosObjetoAsync(CancellationToken ct)
    {
        var propios = await AccesosPropiosAsync(ct);
        return (propios.TryGetValue(CodigosModulo.Vehiculos, out var v) && v.Alguna, propios.TryGetValue(CodigosModulo.Dispositivos, out var d) && d.Alguna);
    }

    /// <summary>Fecha actual en la zona horaria de la sede (ADR-008); UTC si la zona no se reconoce.</summary>
    private static DateOnly Hoy(string zonaHoraria)
    {
        var zona = TimeZoneInfo.TryFindSystemTimeZoneById(zonaHoraria, out var encontrada) ? encontrada : TimeZoneInfo.Utc;
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona));
    }

    /// <summary>
    /// Evaluación de vigencia por fecha (no persistida). El horario y la sede se validarán en cada ingreso (HU-048/HU-050).
    /// </summary>
    private static string Vigencia(AutorizacionObjetoDto x, string zonaHoraria)
    {
        if (x.Estado is not (Pendiente or Aprobada)) return "NoAplica";
        var hoy = Hoy(zonaHoraria);
        if (x.FechaFin < hoy) return "Vencida";
        if (x.Estado == Pendiente) return "NoAplica";
        return x.FechaInicio > hoy ? "Programada" : "Vigente";
    }

    private sealed class Fila
    {
        public AutorizacionObjetoDto Dto { get; set; } = null!;
        public string ZonaHoraria { get; set; } = "UTC";
    }

    private IQueryable<Fila> Proyectar(IQueryable<AutorizacionObjeto> origen) =>
        from a in origen
        join p in Db.Personas on a.IdPersona equals p.IdPersona
        join s in Db.Sedes on a.IdSede equals s.IdSede
        select new Fila
        {
            ZonaHoraria = s.ZonaHoraria,
            Dto = new AutorizacionObjetoDto
            {
                IdAutorizacionObjeto = a.IdAutorizacionObjeto, TipoObjeto = a.TipoObjeto, IdVehiculo = a.IdVehiculo, IdDispositivo = a.IdDispositivo,
                Objeto = a.TipoObjeto == Vehiculo
                    ? Db.Vehiculos.Where(v => v.IdVehiculo == a.IdVehiculo).Select(v => v.Placa).FirstOrDefault()
                    : Db.Dispositivos.Where(d => d.IdDispositivo == a.IdDispositivo)
                        .Select(d => Db.TiposDispositivo.Where(t => t.IdTipoDispositivo == d.IdTipoDispositivo).Select(t => t.Nombre).FirstOrDefault() + " · " + d.CodigoIdentificador).FirstOrDefault(),
                ObjetoActivo = a.TipoObjeto == Vehiculo
                    ? Db.Vehiculos.Any(v => v.IdVehiculo == a.IdVehiculo && v.Activo)
                    : Db.Dispositivos.Any(d => d.IdDispositivo == a.IdDispositivo && d.Activo),
                IdPersona = a.IdPersona, Persona = p.Nombres + " " + p.Apellidos, DocumentoPersona = p.NumeroDocumento,
                IdSede = a.IdSede, Sede = s.Nombre, IdArea = a.IdArea, Area = Db.Areas.Where(x => x.IdArea == a.IdArea).Select(x => x.Nombre).FirstOrDefault(),
                IdAutorizacion = a.IdAutorizacion, FechaInicio = a.FechaInicio, FechaFin = a.FechaFin, HoraInicio = a.HoraInicio, HoraFin = a.HoraFin,
                Motivo = a.Motivo, Estado = a.Estado, MotivoDecision = a.MotivoDecision,
                SolicitadoPor = Db.Usuarios.Where(u => u.IdUsuario == a.IdUsuarioSolicita).Select(u => u.Correo).FirstOrDefault(), SolicitadoEn = a.SolicitadoEn,
                DecididoPor = Db.Usuarios.Where(u => u.IdUsuario == a.IdUsuarioDecide).Select(u => u.Correo).FirstOrDefault(), DecididoEn = a.DecididoEn,
            },
        };

    private async Task<AutorizacionObjetoDto?> DetalleAsync(int id, CancellationToken ct)
    {
        var fila = await Proyectar(Db.AutorizacionesObjeto.AsNoTracking().Where(x => x.IdAutorizacionObjeto == id)).SingleOrDefaultAsync(ct);
        if (fila is null) return null;
        fila.Dto.Vigencia = Vigencia(fila.Dto, fila.ZonaHoraria);
        fila.Dto.Historial = await (
            from t in Db.AutorizacionesObjetoTransiciones.AsNoTracking()
            where t.IdAutorizacionObjeto == id
            orderby t.OcurridoEn, t.IdTransicion
            select new TransicionAutorizacionObjetoDto
            {
                IdTransicion = t.IdTransicion, EstadoAnterior = t.EstadoAnterior, EstadoNuevo = t.EstadoNuevo, Motivo = t.Motivo, OcurridoEn = t.OcurridoEn,
                Actor = Db.Usuarios.Where(u => u.IdUsuario == t.IdUsuarioActor).Select(u => u.Correo).FirstOrDefault(),
            }).ToListAsync(ct);
        return fila.Dto;
    }
}
