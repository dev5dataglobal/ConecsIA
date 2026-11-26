using System.Text.Json;
using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ApiConecsia.Validators;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ApiConecsia.Services.ReglasAcceso;

namespace ApiConecsia.Controllers;

/// <summary>
/// Control de accesos unificado (ADR-029): un único punto de creación del registro de acceso (tabla autorizacion) con su
/// categoría, tipo de permanencia, horario y vehículos/dispositivos; decisiones; entradas y salidas (la visita es el
/// episodio que nace en cada entrada); presencia; historial y bitácora. Organización = la activa validada en servidor;
/// alcance por sedes de la membresía (D-34). Permisos (Propuesta, D-40/D-52):
/// consultar = Control de accesos·Consultar; crear = Control de accesos·Añadir; aprobar/rechazar = Autorizaciones·Editar o
/// el anfitrión del registro si la categoría usa flujo Anfitrion; revocar = Autorizaciones·Editar; cancelar = Control de
/// accesos·Editar; cambiar horario = Control de accesos·Editar (Autorizaciones·Editar si ya está aprobado);
/// entrada = Control de accesos·Añadir; salida = Control de accesos·Editar.
/// </summary>
[Route("api/accesos")]
[Produces("application/json")]
public sealed partial class AccesosController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private const string Entidad = "Autorizacion";
    private const string EntidadObjeto = "AutorizacionObjeto";
    private static readonly DateOnly SinFin = new(9999, 12, 31);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---------- Consulta ----------

    /// <summary>Lista registros de acceso con filtros y paginación en servidor.</summary>
    /// <param name="buscar">Documento o nombre de la persona.</param>
    /// <param name="estado">Pendiente | Aprobada | Rechazada | Revocada | Cancelada.</param>
    /// <param name="desde">Registros cuyo periodo termina en o después de esta fecha.</param>
    /// <param name="hasta">Registros cuyo periodo empieza en o antes de esta fecha.</param>
    [HttpGet]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<PaginaRegistrosAccesoDto>> Listar(
        [FromQuery] string? buscar, [FromQuery] string? estado, [FromQuery] int? idCategoria, [FromQuery] int? idSede, [FromQuery] int? idArea,
        [FromQuery] int? idAnfitrion, [FromQuery] string? modalidad, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 100);
        var query = await EnAlcanceAsync(ct);
        if (Limpiar(buscar) is { } texto)
        {
            var patron = $"%{texto.ToUpperInvariant()}%";
            query = query.Where(a => Db.Personas.Any(p => p.IdPersona == a.IdPersona &&
                (EF.Functions.ILike(p.NumeroDocumentoNormalizado, patron) || EF.Functions.ILike(p.Nombres + " " + p.Apellidos, patron))));
        }
        if (Limpiar(estado) is { } e) query = query.Where(a => a.Estado == e);
        if (idCategoria is int c) query = query.Where(a => a.IdCategoriaAcceso == c);
        if (idSede is int s) query = query.Where(a => a.IdSede == s);
        if (idArea is int ar) query = query.Where(a => a.IdArea == ar);
        if (idAnfitrion is int an) query = query.Where(a => a.IdMembresiaResponsable == an);
        if (Limpiar(modalidad) is { } m) query = query.Where(a => a.Modalidad == m);
        if (desde is DateOnly d) query = query.Where(a => a.FechaFin == null || a.FechaFin >= d);
        if (hasta is DateOnly h) query = query.Where(a => a.FechaInicio <= h);

        var total = await query.CountAsync(ct);
        var ids = await query.OrderByDescending(a => a.CreadoEn).ThenByDescending(a => a.IdAutorizacion)
            .Skip((pagina - 1) * tamano).Take(tamano).Select(a => a.IdAutorizacion).ToListAsync(ct);
        var filas = await ProyectarAsync(ids, detalle: false, ct);
        return Ok(new PaginaRegistrosAccesoDto { Registros = filas, Total = total, Pagina = pagina, Tamano = tamano });
    }

    /// <summary>Detalle con horario efectivo, objetos, historial de estados, movimientos y motivos de ingreso actuales.</summary>
    [HttpGet("{id:int}")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<RegistroAccesoDto>> Obtener(int id, CancellationToken ct)
    {
        if (!await (await EnAlcanceAsync(ct)).AnyAsync(x => x.IdAutorizacion == id, ct)) return NoEncontrado("El registro de acceso no existe o no está en tu alcance.");
        return Ok((await ProyectarAsync([id], detalle: true, ct)).Single());
    }

    /// <summary>Opciones del formulario: categorías activas, sedes y destinos del alcance, anfitriones, puntos y parámetros.</summary>
    [HttpGet("opciones")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<OpcionesAccesoDto>> Opciones(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var sedes = await Db.Sedes.AsNoTracking().Where(x => x.Activo && (alcance == null || alcance.Contains(x.IdSede)))
            .OrderBy(x => x.Nombre).Select(x => new OpcionSimpleDto { Id = x.IdSede, Nombre = x.Nombre }).ToListAsync(ct);
        var idsSede = sedes.Select(x => x.Id).ToList();
        return Ok(new OpcionesAccesoDto
        {
            Categorias = await Db.CategoriasAcceso.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Orden).ThenBy(x => x.Nombre)
                .Select(x => AConCategoria(x)).ToListAsync(ct),
            Sedes = sedes,
            Areas = await Db.Areas.AsNoTracking().Where(x => x.Activo && idsSede.Contains(x.IdSede)).OrderBy(x => x.Nombre)
                .Select(x => new OpcionAreaAccesoDto
                {
                    Id = x.IdArea, IdSede = x.IdSede, Nombre = x.Nombre,
                    Responsables = Db.AreasResponsables.Where(r => r.IdArea == x.IdArea && r.Activo).Select(r => r.IdMembresia).ToList(),
                }).ToListAsync(ct),
            Anfitriones = await (
                from m in Db.MembresiasOrganizacion.AsNoTracking()
                join u in Db.Usuarios on m.IdUsuario equals u.IdUsuario
                where (m.Estado == "Activa" || m.Estado == "Aceptada") && u.Activo
                orderby u.Correo
                select new OpcionSimpleDto { Id = m.IdMembresia, Nombre = u.Correo }).ToListAsync(ct),
            PuntosAcceso = await Db.PuntosAcceso.AsNoTracking().Where(x => x.Activo && idsSede.Contains(x.IdSede)).OrderBy(x => x.Nombre)
                .Select(x => new OpcionPuntoAccesoDto { Id = x.IdPuntoAcceso, IdSede = x.IdSede, Nombre = x.Nombre, ModoAcceso = x.ModoAcceso }).ToListAsync(ct),
            PermitePermanente = await PermitePermanenteAsync(ct),
            FueraDeHorario = await ModoFueraHorarioAsync(ct),
        });
    }

    // ---------- Creación ----------

    /// <summary>
    /// Crea el registro de acceso (único punto de creación). Con flujo Automatico de la categoría nace Aprobado
    /// (origen Politica, sin decisor humano); si no, Pendiente. Los vehículos y dispositivos se crean con decisión propia.
    /// </summary>
    /// <response code="201">Registro creado.</response>
    /// <response code="400">Datos no válidos.</response>
    /// <response code="409">Existe un registro incompatible o la política no permite la modalidad.</response>
    [HttpPost]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Anadir)]
    [ProducesResponseType(typeof(RegistroAccesoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegistroAccesoDto>> Crear([FromBody] RegistroAccesoEntrada request, CancellationToken ct)
    {
        var persona = await Db.Personas.AsNoTracking().SingleOrDefaultAsync(x => x.IdPersona == request.IdPersona, ct);
        if (persona is null) return Invalido("La persona no existe en tu organización.");
        if (!persona.Activo) return Invalido("La persona está inactiva: no se puede registrar su acceso.");
        var categoria = await Db.CategoriasAcceso.AsNoTracking().SingleOrDefaultAsync(x => x.IdCategoriaAcceso == request.IdCategoriaAcceso && x.Activo, ct);
        if (categoria is null) return Invalido("La categoría de acceso no existe o está inactiva.");
        if (!categoria.ModalidadesPermitidas.Contains(request.Modalidad))
            return Invalido($"La categoría «{categoria.Nombre}» no admite el tipo de permanencia {ReglasAutorizacion.TextoModalidad(request.Modalidad!)}.");
        if (categoria.ExigeMotivo && Limpiar(request.Motivo) is null) return Invalido("Indique el motivo del ingreso.");
        if (request.Objetos.Any(o => o.TipoObjeto == "Vehiculo") && !categoria.PermiteVehiculos) return Invalido($"La categoría «{categoria.Nombre}» no admite vehículos.");
        if (request.Objetos.Any(o => o.TipoObjeto == "Dispositivo") && !categoria.PermiteDispositivos) return Invalido($"La categoría «{categoria.Nombre}» no admite dispositivos.");
        var sede = await Db.Sedes.AsNoTracking().SingleOrDefaultAsync(x => x.IdSede == request.IdSede && x.Activo, ct);
        if (sede is null) return Invalido("La sede no existe en tu organización o está inactiva.");
        if (!EnAlcance(await SedesAlcanceAsync(ct), sede.IdSede)) return Prohibido("La sede no está en tu alcance.");
        if (!await Db.Areas.AnyAsync(x => x.IdArea == request.IdArea && x.IdSede == sede.IdSede && x.Activo, ct))
            return Invalido("El destino no pertenece a la sede o está inactivo.");
        if (await ValidarResponsableAsync(request.IdArea, request.IdMembresiaResponsable, ct) is { } responsable) return Invalido(responsable);

        var local = ReglasAutorizacion.HoraLocal(DateTime.UtcNow, sede.ZonaHoraria);
        var (condiciones, invalida) = ReglasAcceso.Normalizar(request.Modalidad!, request.FechaInicio, request.FechaFin, request.HoraInicio, request.HoraFin,
            RegistroAccesoValidator.Convertir(request.Franjas), request.UsaHorarioBase, local);
        if (invalida is not null) return Invalido(invalida);
        if (condiciones!.Modalidad == ReglasAutorizacion.Permanente && !await PermitePermanenteAsync(ct))
            return Conflicto("La política de la organización no permite accesos permanentes.");

        var propios = await AccesosPropiosAsync(ct);
        foreach (var objeto in request.Objetos)
        {
            var modulo = objeto.TipoObjeto == "Vehiculo" ? CodigosModulo.Vehiculos : CodigosModulo.Dispositivos;
            if (!(propios.TryGetValue(modulo, out var acciones) && acciones.Anadir)) return Prohibido($"Tu perfil no puede autorizar {(modulo == CodigosModulo.Vehiculos ? "vehículos" : "dispositivos")}.");
            if (await ValidarObjetoAsync(objeto.TipoObjeto!, persona.IdPersona, objeto.IdObjeto!.Value, ct) is { } error) return Invalido(error);
        }

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        // Misma clave que las solicitudes de acceso anteriores: serializa las de la persona para verificar incompatibles (D-41).
        await Db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({-OrganizacionActiva}, {persona.IdPersona})", ct);
        var hoy = DateOnly.FromDateTime(local);
        var cruce = await Db.Autorizaciones.AsNoTracking()
            .Where(x => x.IdPersona == persona.IdPersona && x.IdSede == sede.IdSede && x.IdArea == request.IdArea
                && (x.Estado == Pendiente || x.Estado == Aprobada)
                && (x.FechaFin == null || x.FechaFin >= hoy)
                && (x.CupoIngresos == null || x.IngresosConsumidos < x.CupoIngresos)
                && (condiciones.FechaFin == null || x.FechaInicio <= condiciones.FechaFin)
                && (x.FechaFin == null || x.FechaFin >= condiciones.FechaInicio))
            .Select(x => new { x.IdAutorizacion, x.Estado, x.Modalidad }).FirstOrDefaultAsync(ct);
        if (cruce is not null)
            return Conflicto($"La persona ya tiene el registro de acceso #{cruce.IdAutorizacion} ({ReglasAutorizacion.TextoModalidad(cruce.Modalidad)}, {cruce.Estado.ToLowerInvariant()}) para esta sede y destino en un periodo que se cruza.");

        var automatico = categoria.FlujoAprobacion == FlujoAutomatico;
        var ahora = DateTime.UtcNow;
        var idUsuario = User.IdUsuario()!.Value;
        var registro = new Autorizacion
        {
            IdOrganizacion = OrganizacionActiva, IdPersona = persona.IdPersona, IdSede = sede.IdSede, IdArea = request.IdArea,
            IdMembresiaResponsable = request.IdMembresiaResponsable, IdUsuarioCrea = idUsuario, IdCategoriaAcceso = categoria.IdCategoriaAcceso,
            Modalidad = condiciones.Modalidad, FechaInicio = condiciones.FechaInicio, FechaFin = condiciones.FechaFin,
            HoraInicio = condiciones.HoraInicio, HoraFin = condiciones.HoraFin, UsaHorarioBase = request.UsaHorarioBase,
            CupoIngresos = condiciones.Modalidad == ReglasAutorizacion.Unica ? 1 : null,
            Motivo = Limpiar(request.Motivo), Observacion = Limpiar(request.Observacion),
            Estado = automatico ? Aprobada : Pendiente, DecididoEn = automatico ? ahora : null,
            Origen = automatico ? "Politica" : "Manual", Version = 1, CreadoEn = ahora,
        };
        Db.Autorizaciones.Add(registro);
        await Db.SaveChangesAsync(ct);
        foreach (var f in condiciones.Franjas)
            Db.AutorizacionesHorarios.Add(new AutorizacionHorario { IdOrganizacion = OrganizacionActiva, IdAutorizacion = registro.IdAutorizacion, DiaSemana = f.DiaSemana, HoraInicio = f.HoraInicio, HoraFin = f.HoraFin });
        RegistrarTransicion(registro, "", automatico ? $"Aprobación automática por la categoría «{categoria.Nombre}»." : registro.Motivo);

        var objetos = new List<AutorizacionObjeto>();
        foreach (var o in request.Objetos)
        {
            var objeto = NuevoObjeto(registro, o.TipoObjeto!, o.IdObjeto!.Value, automatico, idUsuario, ahora);
            Db.AutorizacionesObjeto.Add(objeto);
            objetos.Add(objeto);
        }
        await Db.SaveChangesAsync(ct);
        foreach (var objeto in objetos)
        {
            RegistrarTransicionObjeto(objeto, null, automatico ? "Aprobación automática con el registro de acceso." : null);
            Auditar(OrganizacionActiva, "Solicitar", EntidadObjeto, objeto.IdAutorizacionObjeto.ToString(), null, ResumenObjeto(objeto));
        }
        Auditar(OrganizacionActiva, "Crear", Entidad, registro.IdAutorizacion.ToString(), null,
            new { Resumen = Resumen(registro), Categoria = categoria.Codigo, Franjas = condiciones.Franjas, Objetos = objetos.Count });
        if (automatico) Auditar(OrganizacionActiva, "AprobarPorPolitica", Entidad, registro.IdAutorizacion.ToString(), null, new { categoria.Codigo, categoria.FlujoAprobacion });
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Created($"/api/accesos/{registro.IdAutorizacion}", (await ProyectarAsync([registro.IdAutorizacion], detalle: true, ct)).Single());
    }

    // ---------- Decisiones del registro ----------

    /// <summary>Aprueba un registro pendiente (motivo opcional).</summary>
    [HttpPost("{id:int}/aprobar")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public Task<ActionResult<RegistroAccesoDto>> Aprobar(int id, [FromBody] DecisionAccesoEntrada request, CancellationToken ct) => DecidirAsync(id, "Aprobar", request.Motivo, ct);

    /// <summary>Rechaza un registro pendiente (motivo obligatorio).</summary>
    [HttpPost("{id:int}/rechazar")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public Task<ActionResult<RegistroAccesoDto>> Rechazar(int id, [FromBody] DecisionAccesoEntrada request, CancellationToken ct) => DecidirAsync(id, "Rechazar", request.Motivo, ct);

    /// <summary>Revoca un registro aprobado (motivo obligatorio). No impide registrar la salida de quien ya está dentro.</summary>
    [HttpPost("{id:int}/revocar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<RegistroAccesoDto>> Revocar(int id, [FromBody] DecisionAccesoEntrada request, CancellationToken ct) => DecidirAsync(id, "Revocar", request.Motivo, ct);

    /// <summary>Cancela un registro pendiente o aprobado (motivo obligatorio).</summary>
    [HttpPost("{id:int}/cancelar")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Editar)]
    public Task<ActionResult<RegistroAccesoDto>> Cancelar(int id, [FromBody] DecisionAccesoEntrada request, CancellationToken ct) => DecidirAsync(id, "Cancelar", request.Motivo, ct);

    private async Task<ActionResult<RegistroAccesoDto>> DecidirAsync(int id, string accion, string? motivoEntrada, CancellationToken ct)
    {
        var motivo = Limpiar(motivoEntrada);
        if (MotivoObligatorio(accion) && motivo is null)
            return Invalido(accion switch { "Rechazar" => "Indique el motivo del rechazo.", "Revocar" => "Indique el motivo de la revocación.", _ => "Indique el motivo de la cancelación." });
        var registro = await Db.Autorizaciones.SingleOrDefaultAsync(x => x.IdAutorizacion == id, ct);
        if (registro is null || !EnAlcance(await SedesAlcanceAsync(ct), registro.IdSede)) return NoEncontrado("El registro de acceso no existe o no está en tu alcance.");
        if (accion is "Aprobar" or "Rechazar" && await PuedeDecidirAsync(registro, ct) is { } prohibido) return Prohibido(prohibido);
        if (ValidarTransicion(registro.Estado, accion) is { } invalida) return Conflicto(invalida);
        if (accion == "Aprobar")
        {
            var zona = await Db.Sedes.Where(x => x.IdSede == registro.IdSede).Select(x => x.ZonaHoraria).SingleAsync(ct);
            if (ReglasAutorizacion.Vencio(registro, ReglasAutorizacion.HoraLocal(DateTime.UtcNow, zona))) return Conflicto("El registro venció antes de aprobarse: crea uno nuevo con fechas vigentes.");
            if (!await Db.Personas.AnyAsync(x => x.IdPersona == registro.IdPersona && x.Activo, ct)) return Conflicto("No se puede aprobar: la persona está inactiva.");
        }

        var anterior = Resumen(registro);
        var estadoAnterior = registro.Estado;
        registro.Estado = EstadoDestino(accion);
        registro.IdUsuarioDecide = User.IdUsuario()!.Value;
        registro.DecididoEn = DateTime.UtcNow;
        registro.MotivoDecision = motivo;
        registro.Version++;
        RegistrarTransicion(registro, estadoAnterior, motivo);
        Auditar(OrganizacionActiva, accion, Entidad, id.ToString(), anterior, Resumen(registro));
        try
        {
            // version es token de concurrencia: dos decisiones simultáneas dejan una sola transición.
            await Db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            Db.ChangeTracker.Clear();
            return Conflicto("Otro usuario cambió este registro al mismo tiempo. Consulta su estado actual.");
        }
        return Ok((await ProyectarAsync([id], detalle: true, ct)).Single());
    }

    /// <summary>Autorizaciones·Editar, o el anfitrión del registro cuando la categoría usa flujo Anfitrion (D-52, Propuesta).</summary>
    private async Task<string?> PuedeDecidirAsync(Autorizacion registro, CancellationToken ct)
    {
        var propios = await AccesosPropiosAsync(ct);
        if (propios.TryGetValue(CodigosModulo.Autorizaciones, out var a) && a.Editar) return null;
        var flujo = await Db.CategoriasAcceso.Where(x => x.IdCategoriaAcceso == registro.IdCategoriaAcceso).Select(x => x.FlujoAprobacion).SingleOrDefaultAsync(ct);
        if (flujo == FlujoAnfitrion && await MembresiaPropiaAsync(ct) == registro.IdMembresiaResponsable) return null;
        return flujo == FlujoAnfitrion
            ? "Solo el anfitrión del registro o un usuario con permiso de aprobar autorizaciones puede decidirlo."
            : "Tu perfil no puede aprobar ni rechazar registros de acceso.";
    }

    /// <summary>Cambia las franjas propias o pasa a usar el horario base. Auditado con valor anterior y nuevo.</summary>
    [HttpPut("{id:int}/horario")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Editar)]
    public async Task<ActionResult<RegistroAccesoDto>> CambiarHorario(int id, [FromBody] HorarioAccesoEntrada request, CancellationToken ct)
    {
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var registro = await BloquearRegistroAsync(id, ct);
        if (registro is null || !EnAlcance(await SedesAlcanceAsync(ct), registro.IdSede)) return NoEncontrado("El registro de acceso no existe o no está en tu alcance.");
        if (registro.Estado is not (Pendiente or Aprobada)) return Conflicto($"No se cambia el horario de un registro {registro.Estado.ToLowerInvariant()}.");
        if (registro.Modalidad is not (ReglasAutorizacion.Recurrente or ReglasAutorizacion.Permanente))
            return Conflicto("Solo los accesos recurrentes y permanentes tienen franjas por día.");
        // Ampliar el horario de un acceso aprobado equivale a una nueva autorización.
        if (registro.Estado == Aprobada && !((await AccesosPropiosAsync(ct)).TryGetValue(CodigosModulo.Autorizaciones, out var a) && a.Editar))
            return Prohibido("Cambiar el horario de un acceso aprobado requiere permiso para aprobar autorizaciones.");

        var actuales = await Db.AutorizacionesHorarios.Where(x => x.IdAutorizacion == id).ToListAsync(ct);
        var anterior = new { registro.UsaHorarioBase, Franjas = actuales.Select(x => new Franja(x.DiaSemana, x.HoraInicio, x.HoraFin)).ToList() };
        Db.AutorizacionesHorarios.RemoveRange(actuales);
        var nuevas = RegistroAccesoValidator.Convertir(request.Franjas);
        foreach (var f in nuevas)
            Db.AutorizacionesHorarios.Add(new AutorizacionHorario { IdOrganizacion = OrganizacionActiva, IdAutorizacion = id, DiaSemana = f.DiaSemana, HoraInicio = f.HoraInicio, HoraFin = f.HoraFin });
        registro.UsaHorarioBase = request.UsaHorarioBase;
        registro.Version++;
        Auditar(OrganizacionActiva, "CambiarHorario", Entidad, id.ToString(), anterior, new { registro.UsaHorarioBase, Franjas = nuevas, Motivo = Limpiar(request.Motivo) });
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return Ok((await ProyectarAsync([id], detalle: true, ct)).Single());
    }

    // ---------- Vehículos y dispositivos ----------

    /// <summary>Agrega un vehículo o dispositivo al registro; nace Pendiente con decisión propia.</summary>
    [HttpPost("{id:int}/objetos")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Anadir)]
    public async Task<ActionResult<RegistroAccesoDto>> AgregarObjeto(int id, [FromBody] ObjetoAccesoEntrada request, CancellationToken ct)
    {
        var registro = await Db.Autorizaciones.AsNoTracking().SingleOrDefaultAsync(x => x.IdAutorizacion == id, ct);
        if (registro is null || !EnAlcance(await SedesAlcanceAsync(ct), registro.IdSede)) return NoEncontrado("El registro de acceso no existe o no está en tu alcance.");
        if (registro.Estado is not (Pendiente or Aprobada)) return Conflicto($"No se agregan objetos a un registro {registro.Estado.ToLowerInvariant()}.");
        var modulo = request.TipoObjeto == "Vehiculo" ? CodigosModulo.Vehiculos : CodigosModulo.Dispositivos;
        if (!((await AccesosPropiosAsync(ct)).TryGetValue(modulo, out var acciones) && acciones.Anadir)) return Prohibido("Tu perfil no puede autorizar este tipo de objeto.");
        var categoria = await Db.CategoriasAcceso.AsNoTracking().SingleOrDefaultAsync(x => x.IdCategoriaAcceso == registro.IdCategoriaAcceso, ct);
        if (categoria is not null && !(request.TipoObjeto == "Vehiculo" ? categoria.PermiteVehiculos : categoria.PermiteDispositivos))
            return Invalido($"La categoría «{categoria.Nombre}» no admite este tipo de objeto.");
        if (await ValidarObjetoAsync(request.TipoObjeto!, registro.IdPersona, request.IdObjeto!.Value, ct) is { } error) return Invalido(error);
        if (await Db.AutorizacionesObjeto.AnyAsync(x => x.IdAutorizacion == id && x.TipoObjeto == request.TipoObjeto
                && (x.IdVehiculo == request.IdObjeto || x.IdDispositivo == request.IdObjeto) && (x.Estado == Pendiente || x.Estado == Aprobada), ct))
            return Conflicto("El objeto ya está en este registro de acceso.");

        var objeto = NuevoObjeto(registro, request.TipoObjeto!, request.IdObjeto.Value, automatico: false, User.IdUsuario()!.Value, DateTime.UtcNow);
        Db.AutorizacionesObjeto.Add(objeto);
        await Db.SaveChangesAsync(ct);
        RegistrarTransicionObjeto(objeto, null, null);
        Auditar(OrganizacionActiva, "Solicitar", EntidadObjeto, objeto.IdAutorizacionObjeto.ToString(), null, ResumenObjeto(objeto));
        await Db.SaveChangesAsync(ct);
        return Ok((await ProyectarAsync([id], detalle: true, ct)).Single());
    }

    /// <summary>Aprueba el vehículo o dispositivo, independiente de la decisión sobre la persona.</summary>
    [HttpPost("{id:int}/objetos/{idObjeto:int}/aprobar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<RegistroAccesoDto>> AprobarObjeto(int id, int idObjeto, [FromBody] DecisionAccesoEntrada request, CancellationToken ct) =>
        DecidirObjetoAsync(id, idObjeto, Pendiente, Aprobada, "Aprobar", Limpiar(request.Motivo), ct);

    /// <summary>Rechaza el vehículo o dispositivo (motivo obligatorio); la persona conserva su decisión.</summary>
    [HttpPost("{id:int}/objetos/{idObjeto:int}/rechazar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<RegistroAccesoDto>> RechazarObjeto(int id, int idObjeto, [FromBody] DecisionAccesoEntrada request, CancellationToken ct) =>
        Limpiar(request.Motivo) is { } motivo
            ? DecidirObjetoAsync(id, idObjeto, Pendiente, Rechazada, "Rechazar", motivo, ct)
            : Task.FromResult<ActionResult<RegistroAccesoDto>>(Invalido("Indique el motivo del rechazo."));

    /// <summary>Revoca un vehículo o dispositivo aprobado (motivo obligatorio).</summary>
    [HttpPost("{id:int}/objetos/{idObjeto:int}/revocar")]
    [RequiereModulo(CodigosModulo.Autorizaciones, AccionModulo.Editar)]
    public Task<ActionResult<RegistroAccesoDto>> RevocarObjeto(int id, int idObjeto, [FromBody] DecisionAccesoEntrada request, CancellationToken ct) =>
        Limpiar(request.Motivo) is { } motivo
            ? DecidirObjetoAsync(id, idObjeto, Aprobada, Revocada, "Revocar", motivo, ct)
            : Task.FromResult<ActionResult<RegistroAccesoDto>>(Invalido("Indique el motivo de la revocación."));

    private async Task<ActionResult<RegistroAccesoDto>> DecidirObjetoAsync(int id, int idObjeto, string desde, string hacia, string accion, string? motivo, CancellationToken ct)
    {
        var objeto = await Db.AutorizacionesObjeto.SingleOrDefaultAsync(x => x.IdAutorizacionObjeto == idObjeto && x.IdAutorizacion == id, ct);
        if (objeto is null || !EnAlcance(await SedesAlcanceAsync(ct), objeto.IdSede)) return NoEncontrado("El objeto no pertenece a este registro o no está en tu alcance.");
        var modulo = objeto.TipoObjeto == "Vehiculo" ? CodigosModulo.Vehiculos : CodigosModulo.Dispositivos;
        if (!((await AccesosPropiosAsync(ct)).TryGetValue(modulo, out var acciones) && acciones.Alguna)) return Prohibido("Tu perfil no tiene acceso al módulo de este objeto.");
        if (objeto.Estado != desde) return Conflicto($"No se puede {accion.ToLowerInvariant()} un objeto en estado {objeto.Estado}.");
        if (hacia == Aprobada && await ValidarObjetoAsync(objeto.TipoObjeto, objeto.IdPersona, objeto.IdVehiculo ?? objeto.IdDispositivo!.Value, ct) is { } invalido)
            return Conflicto($"No se puede aprobar: {invalido}");

        var anterior = ResumenObjeto(objeto);
        var estadoAnterior = objeto.Estado;
        objeto.Estado = hacia;
        objeto.MotivoDecision = motivo;
        objeto.IdUsuarioDecide = User.IdUsuario()!.Value;
        objeto.DecididoEn = DateTime.UtcNow;
        RegistrarTransicionObjeto(objeto, estadoAnterior, motivo);
        Auditar(OrganizacionActiva, accion, EntidadObjeto, idObjeto.ToString(), anterior, ResumenObjeto(objeto));
        try
        {
            await Db.SaveChangesAsync(ct); // xmin como token de concurrencia (ADR-026).
        }
        catch (DbUpdateConcurrencyException)
        {
            Db.ChangeTracker.Clear();
            return Conflicto("Otro usuario decidió este objeto al mismo tiempo. Consulta su estado actual.");
        }
        return Ok((await ProyectarAsync([id], detalle: true, ct)).Single());
    }

    // ---------- Apoyo ----------

    private async Task<IQueryable<Autorizacion>> EnAlcanceAsync(CancellationToken ct)
    {
        var alcance = await SedesAlcanceAsync(ct);
        var query = Db.Autorizaciones.AsNoTracking();
        return alcance is null ? query : query.Where(x => alcance.Contains(x.IdSede));
    }

    /// <summary>Bloquea la fila del registro (SELECT … FOR UPDATE) y la carga con seguimiento. Requiere transacción.</summary>
    private async Task<Autorizacion?> BloquearRegistroAsync(int id, CancellationToken ct)
    {
        await Db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM public.autorizacion WHERE id_organizacion = {OrganizacionActiva} AND id_autorizacion = {id} FOR UPDATE", ct);
        return await Db.Autorizaciones.SingleOrDefaultAsync(x => x.IdAutorizacion == id, ct);
    }

    private async Task<bool> PermitePermanenteAsync(CancellationToken ct) =>
        await Db.ParametrosOrganizacion.AsNoTracking().AnyAsync(x =>
            (x.Clave == ParametroPermitePermanente || x.Clave == ReglasAutorizacion.ParametroPermitePermanente) && x.Valor.ToLower() == "true", ct);

    private async Task<string> ModoFueraHorarioAsync(CancellationToken ct) =>
        await Db.ParametrosOrganizacion.AsNoTracking().Where(x => x.Clave == ParametroFueraHorario).Select(x => x.Valor).FirstOrDefaultAsync(ct) == "Alertar"
            ? "Alertar" : "Bloquear";

    /// <summary>Persona y objeto activos y asociados (vehículo con vínculo activo; dispositivo bajo su responsabilidad).</summary>
    private async Task<string?> ValidarObjetoAsync(string tipoObjeto, int idPersona, int idObjeto, CancellationToken ct)
    {
        if (!await Db.Personas.AnyAsync(x => x.IdPersona == idPersona && x.Activo, ct)) return "La persona está inactiva.";
        if (tipoObjeto == "Vehiculo")
        {
            var activo = await Db.Vehiculos.AsNoTracking().Where(x => x.IdVehiculo == idObjeto).Select(x => (bool?)x.Activo).SingleOrDefaultAsync(ct);
            if (activo is null) return "El vehículo no existe en tu organización.";
            if (activo == false) return "El vehículo está inactivo.";
            return await Db.PersonasVehiculos.AnyAsync(x => x.IdPersona == idPersona && x.IdVehiculo == idObjeto && x.Activo, ct) ? null : "El vehículo no está asociado a la persona.";
        }
        var dispositivo = await Db.Dispositivos.AsNoTracking().Where(x => x.IdDispositivo == idObjeto).Select(x => new { x.IdPersonaResponsable, x.Activo }).SingleOrDefaultAsync(ct);
        if (dispositivo is null) return "El dispositivo no existe en tu organización.";
        if (dispositivo.IdPersonaResponsable != idPersona) return "El dispositivo no pertenece a la persona.";
        return dispositivo.Activo ? null : "El dispositivo está inactivo.";
    }

    /// <summary>Objeto con la vigencia del registro; sin fecha final se usa 9999-12-31 (la vigencia real la da el registro).</summary>
    private static AutorizacionObjeto NuevoObjeto(Autorizacion registro, string tipo, int idObjeto, bool automatico, int idUsuario, DateTime ahora) => new()
    {
        IdOrganizacion = registro.IdOrganizacion, TipoObjeto = tipo, IdVehiculo = tipo == "Vehiculo" ? idObjeto : null, IdDispositivo = tipo == "Dispositivo" ? idObjeto : null,
        IdPersona = registro.IdPersona, IdSede = registro.IdSede, IdArea = registro.IdArea, IdAutorizacion = registro.IdAutorizacion,
        FechaInicio = registro.FechaInicio, FechaFin = registro.FechaFin ?? SinFin, HoraInicio = registro.HoraInicio, HoraFin = registro.HoraFin,
        Motivo = registro.Motivo, Estado = automatico ? Aprobada : Pendiente, IdUsuarioSolicita = idUsuario, SolicitadoEn = ahora,
        IdUsuarioDecide = automatico ? idUsuario : null, DecididoEn = automatico ? ahora : null,
    };

    private void RegistrarTransicion(Autorizacion registro, string anterior, string? observacion) =>
        Db.AutorizacionesTransiciones.Add(new AutorizacionTransicion
        {
            IdOrganizacion = registro.IdOrganizacion, IdAutorizacion = registro.IdAutorizacion, EstadoAnterior = anterior,
            EstadoNuevo = registro.Estado, IdUsuarioActor = User.IdUsuario(), Observacion = observacion, OcurridoEn = DateTime.UtcNow,
        });

    private void RegistrarTransicionObjeto(AutorizacionObjeto objeto, string? anterior, string? motivo) =>
        Db.AutorizacionesObjetoTransiciones.Add(new AutorizacionObjetoTransicion
        {
            IdOrganizacion = objeto.IdOrganizacion, IdAutorizacionObjeto = objeto.IdAutorizacionObjeto, IdSede = objeto.IdSede,
            EstadoAnterior = anterior, EstadoNuevo = objeto.Estado, Motivo = motivo, IdUsuarioActor = User.IdUsuario()!.Value, OcurridoEn = DateTime.UtcNow,
        });

    private static object Resumen(Autorizacion x) => new
    {
        x.Estado, x.IdCategoriaAcceso, x.IdPersona, x.IdSede, x.IdArea, x.IdMembresiaResponsable, x.Modalidad, x.FechaInicio, x.FechaFin,
        x.HoraInicio, x.HoraFin, x.UsaHorarioBase, x.CupoIngresos, x.Origen, x.IdUsuarioCrea, x.IdUsuarioDecide, x.DecididoEn, x.MotivoDecision,
    };

    private static object ResumenObjeto(AutorizacionObjeto x) => new
    {
        x.Estado, x.TipoObjeto, x.IdVehiculo, x.IdDispositivo, x.IdPersona, x.IdAutorizacion, x.FechaInicio, x.FechaFin, x.MotivoDecision, x.IdUsuarioDecide,
    };

    private static CategoriaAccesoDto AConCategoria(CategoriaAcceso x) => new()
    {
        IdCategoriaAcceso = x.IdCategoriaAcceso, Codigo = x.Codigo, Nombre = x.Nombre, Descripcion = x.Descripcion, FlujoAprobacion = x.FlujoAprobacion,
        ModalidadesPermitidas = x.ModalidadesPermitidas, ExigeAnfitrion = x.ExigeAnfitrion, ExigeMotivo = x.ExigeMotivo,
        PermiteVehiculos = x.PermiteVehiculos, PermiteDispositivos = x.PermiteDispositivos, Orden = x.Orden, Activo = x.Activo,
    };

    /// <summary>Proyección de los registros indicados (en ese orden) con horario efectivo, vigencia y objetos; en detalle, historial y movimientos.</summary>
    private async Task<List<RegistroAccesoDto>> ProyectarAsync(List<int> ids, bool detalle, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var registros = await Db.Autorizaciones.AsNoTracking().Where(x => ids.Contains(x.IdAutorizacion)).ToListAsync(ct);
        var idsPersona = registros.Select(x => x.IdPersona).Distinct().ToList();
        var idsSede = registros.Select(x => x.IdSede).Distinct().ToList();
        var idsArea = registros.Select(x => x.IdArea).Distinct().ToList();
        var idsCategoria = registros.Where(x => x.IdCategoriaAcceso != null).Select(x => x.IdCategoriaAcceso!.Value).Distinct().ToList();
        var idsMembresia = registros.Select(x => x.IdMembresiaResponsable).Distinct().ToList();
        var idsUsuario = registros.Select(x => x.IdUsuarioCrea).Concat(registros.Where(x => x.IdUsuarioDecide != null).Select(x => x.IdUsuarioDecide!.Value)).Distinct().ToList();

        var personas = await Db.Personas.AsNoTracking().Where(x => idsPersona.Contains(x.IdPersona)).ToDictionaryAsync(x => x.IdPersona, ct);
        var sedes = await Db.Sedes.AsNoTracking().Where(x => idsSede.Contains(x.IdSede)).ToDictionaryAsync(x => x.IdSede, ct);
        var areas = await Db.Areas.AsNoTracking().Where(x => idsArea.Contains(x.IdArea)).ToDictionaryAsync(x => x.IdArea, x => x.Nombre, ct);
        var categorias = await Db.CategoriasAcceso.AsNoTracking().Where(x => idsCategoria.Contains(x.IdCategoriaAcceso)).ToDictionaryAsync(x => x.IdCategoriaAcceso, ct);
        var anfitriones = await (from m in Db.MembresiasOrganizacion.AsNoTracking() join u in Db.Usuarios on m.IdUsuario equals u.IdUsuario
                                 where idsMembresia.Contains(m.IdMembresia) select new { m.IdMembresia, u.Correo }).ToDictionaryAsync(x => x.IdMembresia, x => x.Correo, ct);
        var usuarios = await Db.Usuarios.AsNoTracking().Where(x => idsUsuario.Contains(x.IdUsuario)).ToDictionaryAsync(x => x.IdUsuario, x => x.Correo, ct);
        var franjas = (await Db.AutorizacionesHorarios.AsNoTracking().Where(x => ids.Contains(x.IdAutorizacion)).OrderBy(x => x.DiaSemana).ThenBy(x => x.HoraInicio).ToListAsync(ct))
            .ToLookup(x => x.IdAutorizacion, x => new Franja(x.DiaSemana, x.HoraInicio, x.HoraFin));
        var bases = await Db.HorariosBase.AsNoTracking().Where(x => x.IdSede == null || idsSede.Contains(x.IdSede!.Value)).ToListAsync(ct);
        var objetos = (await (
            from o in Db.AutorizacionesObjeto.AsNoTracking()
            where o.IdAutorizacion != null && ids.Contains(o.IdAutorizacion!.Value)
            orderby o.IdAutorizacionObjeto
            select new
            {
                o.IdAutorizacion,
                Dto = new ObjetoAccesoDto
                {
                    IdAutorizacionObjeto = o.IdAutorizacionObjeto, TipoObjeto = o.TipoObjeto, IdObjeto = o.IdVehiculo ?? o.IdDispositivo ?? 0,
                    Objeto = o.TipoObjeto == "Vehiculo"
                        ? Db.Vehiculos.Where(v => v.IdVehiculo == o.IdVehiculo).Select(v => v.Placa + (v.Marca != null ? " · " + v.Marca : "") + (v.Color != null ? " · " + v.Color : "")).FirstOrDefault()
                        : Db.Dispositivos.Where(d => d.IdDispositivo == o.IdDispositivo)
                            .Select(d => Db.TiposDispositivo.Where(t => t.IdTipoDispositivo == d.IdTipoDispositivo).Select(t => t.Nombre).FirstOrDefault() + " · " + d.CodigoIdentificador).FirstOrDefault(),
                    Estado = o.Estado, MotivoDecision = o.MotivoDecision, DecididoEn = o.DecididoEn,
                    DecididoPor = Db.Usuarios.Where(u => u.IdUsuario == o.IdUsuarioDecide).Select(u => u.Correo).FirstOrDefault(),
                },
            }).ToListAsync(ct)).ToLookup(x => x.IdAutorizacion!.Value, x => x.Dto);
        var vehiculosObjetos = objetos.SelectMany(g => g).Where(o => o.TipoObjeto == "Vehiculo").Select(o => o.IdObjeto).Distinct().ToList();
        var dispositivosObjetos = objetos.SelectMany(g => g).Where(o => o.TipoObjeto == "Dispositivo").Select(o => o.IdObjeto).Distinct().ToList();
        var abiertas = await EntradasObjetoAbiertas()
            .Where(e => (e.IdVehiculo != null && vehiculosObjetos.Contains(e.IdVehiculo.Value)) || (e.IdDispositivo != null && dispositivosObjetos.Contains(e.IdDispositivo.Value)))
            .Select(e => new { e.TipoObjeto, IdObjeto = e.IdVehiculo ?? e.IdDispositivo!.Value, e.OcurridoEn }).ToListAsync(ct);
        foreach (var o in objetos.SelectMany(g => g))
            if (abiertas.FirstOrDefault(e => e.TipoObjeto == o.TipoObjeto && e.IdObjeto == o.IdObjeto) is { } abierta)
            {
                o.Dentro = true;
                o.EntradaEn = abierta.OcurridoEn;
            }
        var dentro = (await (from pe in Db.Permanencias.AsNoTracking() join v in Db.Visitas on pe.IdVisita equals v.IdVisita
                             where pe.Estado == "Abierta" && v.IdAutorizacion != null && ids.Contains(v.IdAutorizacion!.Value)
                             select v.IdAutorizacion!.Value).ToListAsync(ct)).ToHashSet();

        var ahora = DateTime.UtcNow;
        var resultado = new List<RegistroAccesoDto>();
        foreach (var id in ids)
        {
            var a = registros.Single(x => x.IdAutorizacion == id);
            var persona = personas[a.IdPersona];
            var sede = sedes[a.IdSede];
            var categoria = a.IdCategoriaAcceso is int ic && categorias.TryGetValue(ic, out var cat) ? cat : null;
            var horario = HorarioEfectivo(a, franjas[id], bases);
            var local = ReglasAutorizacion.HoraLocal(ahora, sede.ZonaHoraria);
            var dto = new RegistroAccesoDto
            {
                IdAutorizacion = a.IdAutorizacion, IdCategoriaAcceso = a.IdCategoriaAcceso, Categoria = categoria?.Nombre, FlujoAprobacion = categoria?.FlujoAprobacion,
                IdPersona = a.IdPersona, Persona = $"{persona.Nombres} {persona.Apellidos}", DocumentoPersona = persona.NumeroDocumento, PersonaActiva = persona.Activo,
                IdSede = a.IdSede, Sede = sede.Nombre, IdArea = a.IdArea, Area = areas.GetValueOrDefault(a.IdArea, ""), IdMembresiaResponsable = a.IdMembresiaResponsable,
                Anfitrion = anfitriones.GetValueOrDefault(a.IdMembresiaResponsable), Motivo = a.Motivo, Observacion = a.Observacion, Modalidad = a.Modalidad,
                FechaInicio = a.FechaInicio, FechaFin = a.FechaFin, HoraInicio = a.HoraInicio, HoraFin = a.HoraFin, UsaHorarioBase = a.UsaHorarioBase,
                CupoIngresos = ReglasVisita.CupoEfectivo(a), IngresosConsumidos = a.IngresosConsumidos, Estado = a.Estado, Vigencia = ReglasAcceso.Vigencia(a, horario, local),
                Origen = a.Origen, CreadoPor = usuarios.GetValueOrDefault(a.IdUsuarioCrea), CreadoEn = a.CreadoEn,
                DecididoPor = a.IdUsuarioDecide is int d ? usuarios.GetValueOrDefault(d) : null, DecididoEn = a.DecididoEn, MotivoDecision = a.MotivoDecision,
                Dentro = dentro.Contains(id), ZonaHoraria = sede.ZonaHoraria,
                Horario = horario.Select(f => new FranjaHorariaDto { DiaSemana = f.DiaSemana, HoraInicio = f.HoraInicio, HoraFin = f.HoraFin }).ToList(),
                Objetos = objetos[id].ToList(),
            };
            if (detalle)
            {
                var evaluacion = Evaluar(a, horario, local);
                dto.MotivosIngreso = [.. evaluacion.Bloqueos, .. evaluacion.Horario];
                dto.Historial = await (from t in Db.AutorizacionesTransiciones.AsNoTracking()
                                       where t.IdAutorizacion == id
                                       orderby t.OcurridoEn, t.IdTransicion
                                       select new TransicionAccesoDto
                                       {
                                           EstadoAnterior = t.EstadoAnterior == "" ? null : t.EstadoAnterior, EstadoNuevo = t.EstadoNuevo, Observacion = t.Observacion, OcurridoEn = t.OcurridoEn,
                                           Actor = Db.Usuarios.Where(u => u.IdUsuario == t.IdUsuarioActor).Select(u => u.Correo).FirstOrDefault(),
                                       }).ToListAsync(ct);
                dto.Movimientos = await ProyectarMovimientos(Db.MovimientosAcceso.AsNoTracking().Where(m => m.IdAutorizacion == id))
                    .OrderByDescending(m => m.OcurridoEn).Take(50).ToListAsync(ct);
            }
            resultado.Add(dto);
        }
        return resultado;
    }
}
