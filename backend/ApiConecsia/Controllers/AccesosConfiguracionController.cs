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

namespace ApiConecsia.Controllers;

/// <summary>
/// Configuración del módulo Control de accesos por organización (ADR-029): categorías de acceso (terminología, flujo de
/// aprobación, tipos de permanencia y objetos admitidos), horario base de la organización o de cada sede, zona horaria y
/// parámetros. Consultar = Control de accesos·Consultar; modificar = Configuración·Editar (Propuesta, D-40).
/// Sin borrado: las categorías se inactivan y conservan los registros que las usan.
/// </summary>
[Route("api/accesos/configuracion")]
[Produces("application/json")]
public sealed class AccesosConfiguracionController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    /// <summary>Categorías de acceso de la organización (activas e inactivas).</summary>
    [HttpGet("categorias")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<CategoriaAccesoDto>>> Categorias(CancellationToken ct) =>
        Ok(await Db.CategoriasAcceso.AsNoTracking().OrderBy(x => x.Orden).ThenBy(x => x.Nombre).Select(x => Dto(x)).ToListAsync(ct));

    [HttpPost("categorias")]
    [RequiereModulo(CodigosModulo.Configuracion, AccionModulo.Editar)]
    [ProducesResponseType(typeof(CategoriaAccesoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoriaAccesoDto>> CrearCategoria([FromBody] CategoriaAccesoEntrada request, CancellationToken ct)
    {
        var categoria = new CategoriaAcceso { IdOrganizacion = OrganizacionActiva, CreadoEn = DateTime.UtcNow };
        Aplicar(categoria, request);
        Db.CategoriasAcceso.Add(categoria);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una categoría con ese código.");
        Auditar(OrganizacionActiva, "Crear", "CategoriaAcceso", categoria.IdCategoriaAcceso.ToString(), null, Dto(categoria));
        await Db.SaveChangesAsync(ct);
        return Created($"/api/accesos/configuracion/categorias/{categoria.IdCategoriaAcceso}", Dto(categoria));
    }

    /// <summary>Modifica una categoría. Los cambios aplican a registros nuevos; los existentes conservan su decisión.</summary>
    [HttpPut("categorias/{id:int}")]
    [RequiereModulo(CodigosModulo.Configuracion, AccionModulo.Editar)]
    public async Task<ActionResult<CategoriaAccesoDto>> EditarCategoria(int id, [FromBody] CategoriaAccesoEntrada request, CancellationToken ct)
    {
        var categoria = await Db.CategoriasAcceso.SingleOrDefaultAsync(x => x.IdCategoriaAcceso == id, ct);
        if (categoria is null) return NoEncontrado("La categoría no existe en tu organización.");
        var anterior = Dto(categoria);
        Aplicar(categoria, request);
        Auditar(OrganizacionActiva, "Modificar", "CategoriaAcceso", id.ToString(), anterior, Dto(categoria));
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe una categoría con ese código.");
        return Ok(Dto(categoria));
    }

    /// <summary>Horario base de la organización (<paramref name="idSede"/> nulo) o de una sede.</summary>
    [HttpGet("horario-base")]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<HorarioBaseDto>> HorarioBase([FromQuery] int? idSede, CancellationToken ct)
    {
        string zona;
        if (idSede is int s)
        {
            if (await Db.Sedes.AsNoTracking().Where(x => x.IdSede == s).Select(x => x.ZonaHoraria).SingleOrDefaultAsync(ct) is not { } deSede)
                return NoEncontrado("La sede no existe en tu organización.");
            zona = deSede;
        }
        else zona = await ZonaOrganizacionAsync(ct);
        return Ok(new HorarioBaseDto
        {
            IdSede = idSede, ZonaHoraria = zona,
            Franjas = await Db.HorariosBase.AsNoTracking().Where(x => x.IdSede == idSede).OrderBy(x => x.DiaSemana).ThenBy(x => x.HoraInicio)
                .Select(x => new FranjaHorariaDto { DiaSemana = x.DiaSemana, HoraInicio = x.HoraInicio, HoraFin = x.HoraFin }).ToListAsync(ct),
        });
    }

    /// <summary>Reemplaza el horario base de la organización o de una sede. Sin franjas, la sede vuelve a heredar el de la organización.</summary>
    [HttpPut("horario-base")]
    [RequiereModulo(CodigosModulo.Configuracion, AccionModulo.Editar)]
    public async Task<ActionResult<HorarioBaseDto>> GuardarHorarioBase([FromBody] HorarioBaseEntrada request, CancellationToken ct)
    {
        if (request.IdSede is int s && !await Db.Sedes.AnyAsync(x => x.IdSede == s, ct)) return NoEncontrado("La sede no existe en tu organización.");
        var actuales = await Db.HorariosBase.Where(x => x.IdSede == request.IdSede).ToListAsync(ct);
        var anterior = actuales.Select(x => new { x.DiaSemana, x.HoraInicio, x.HoraFin }).ToList();
        Db.HorariosBase.RemoveRange(actuales);
        foreach (var f in RegistroAccesoValidator.Convertir(request.Franjas))
            Db.HorariosBase.Add(new HorarioBase { IdOrganizacion = OrganizacionActiva, IdSede = request.IdSede, DiaSemana = f.DiaSemana, HoraInicio = f.HoraInicio, HoraFin = f.HoraFin });
        Auditar(OrganizacionActiva, "CambiarHorarioBase", "HorarioBase", request.IdSede?.ToString() ?? "organizacion", anterior, request.Franjas);
        await Db.SaveChangesAsync(ct);
        return await HorarioBase(request.IdSede, ct);
    }

    [HttpGet]
    [RequiereModulo(CodigosModulo.ControlAccesos, AccionModulo.Consultar)]
    public async Task<ActionResult<ConfiguracionAccesoDto>> Obtener(CancellationToken ct) => Ok(await ConfiguracionAsync(ct));

    private async Task<ConfiguracionAccesoDto> ConfiguracionAsync(CancellationToken ct)
    {
        var parametros = await Db.ParametrosOrganizacion.AsNoTracking().ToDictionaryAsync(x => x.Clave, x => x.Valor, ct);
        return new ConfiguracionAccesoDto
        {
            ZonaHoraria = await ZonaOrganizacionAsync(ct),
            FueraDeHorario = parametros.GetValueOrDefault(ReglasAcceso.ParametroFueraHorario) == "Alertar" ? "Alertar" : "Bloquear",
            PermitePermanente = string.Equals(parametros.GetValueOrDefault(ReglasAcceso.ParametroPermitePermanente)
                ?? parametros.GetValueOrDefault(ReglasAutorizacion.ParametroPermitePermanente), "true", StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>Zona horaria de la organización (valor por defecto de sedes nuevas), modo fuera de horario y modalidad Permanente.</summary>
    [HttpPut]
    [RequiereModulo(CodigosModulo.Configuracion, AccionModulo.Editar)]
    public async Task<ActionResult<ConfiguracionAccesoDto>> Guardar([FromBody] ConfiguracionAccesoEntrada request, CancellationToken ct)
    {
        if (request.FueraDeHorario is not (null or "Bloquear" or "Alertar")) return Invalido("Fuera de horario debe ser Bloquear o Alertar.");
        var anterior = await ConfiguracionAsync(ct);
        if (Limpiar(request.ZonaHoraria) is { } zona)
        {
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(zona, out _)) return Invalido("La zona horaria no es válida.");
            // organizacion no tiene filtro global: se actualiza solo la organización activa.
            var organizacion = await Db.Organizaciones.SingleAsync(x => x.IdOrganizacion == OrganizacionActiva, ct);
            organizacion.ZonaHoraria = zona;
        }
        if (request.FueraDeHorario is { } modo) await FijarParametroAsync(ReglasAcceso.ParametroFueraHorario, modo, ct);
        if (request.PermitePermanente is bool permite) await FijarParametroAsync(ReglasAcceso.ParametroPermitePermanente, permite ? "true" : "false", ct);
        await Db.SaveChangesAsync(ct);
        var nuevo = await ConfiguracionAsync(ct);
        Auditar(OrganizacionActiva, "Configurar", "ControlAccesos", OrganizacionActiva.ToString(), anterior, nuevo);
        await Db.SaveChangesAsync(ct);
        return Ok(nuevo);
    }

    private async Task FijarParametroAsync(string clave, string valor, CancellationToken ct)
    {
        var parametro = await Db.ParametrosOrganizacion.SingleOrDefaultAsync(x => x.Clave == clave, ct);
        if (parametro is null) Db.ParametrosOrganizacion.Add(new ParametroOrganizacion { IdOrganizacion = OrganizacionActiva, Clave = clave, Valor = valor, ActualizadoEn = DateTime.UtcNow });
        else { parametro.Valor = valor; parametro.ActualizadoEn = DateTime.UtcNow; }
    }

    private Task<string> ZonaOrganizacionAsync(CancellationToken ct) =>
        Db.Organizaciones.AsNoTracking().Where(x => x.IdOrganizacion == OrganizacionActiva).Select(x => x.ZonaHoraria).SingleAsync(ct);

    private static void Aplicar(CategoriaAcceso c, CategoriaAccesoEntrada r)
    {
        c.Codigo = r.Codigo!.Trim().ToUpperInvariant();
        c.Nombre = r.Nombre!.Trim();
        c.Descripcion = Limpiar(r.Descripcion);
        c.FlujoAprobacion = r.FlujoAprobacion!;
        c.ModalidadesPermitidas = r.ModalidadesPermitidas.Distinct().ToArray();
        c.ExigeAnfitrion = r.ExigeAnfitrion;
        c.ExigeMotivo = r.ExigeMotivo;
        c.PermiteVehiculos = r.PermiteVehiculos;
        c.PermiteDispositivos = r.PermiteDispositivos;
        c.Orden = r.Orden;
        c.Activo = r.Activo;
    }

    private static CategoriaAccesoDto Dto(CategoriaAcceso x) => new()
    {
        IdCategoriaAcceso = x.IdCategoriaAcceso, Codigo = x.Codigo, Nombre = x.Nombre, Descripcion = x.Descripcion, FlujoAprobacion = x.FlujoAprobacion,
        ModalidadesPermitidas = x.ModalidadesPermitidas, ExigeAnfitrion = x.ExigeAnfitrion, ExigeMotivo = x.ExigeMotivo,
        PermiteVehiculos = x.PermiteVehiculos, PermiteDispositivos = x.PermiteDispositivos, Orden = x.Orden, Activo = x.Activo,
    };
}
