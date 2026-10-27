using ApiConecsia.Helpers;
using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

/// <summary>
/// Zonas empresariales: una organización superior (administradora) agrupa empresas. La organización superior
/// administra los accesos de las áreas comunes (sedes de la zona); cada empresa conserva los suyos (ver SedeController
/// y AutorizacionController). Crear, editar e inactivar zonas cruza organizaciones: solo el administrador de plataforma.
/// Las demás organizaciones consultan las zonas en las que participan; solo la superior ve la lista de empresas.
/// </summary>
[Route("api/zonas-empresariales")]
public sealed class ZonaEmpresarialController(ConecsiaDbContext db, IOrganizationContext organizationContext, IModuloAccesoService accesos)
    : AdministracionControllerBase(db, organizationContext, accesos)
{
    private IQueryable<ZonaEmpresarialOrganizacion> Vinculos => Db.ZonasEmpresarialesOrganizaciones.IgnoreQueryFilters();

    [HttpGet]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<ZonaEmpresarialAdministracionDto>>> Listar([FromQuery] string? buscar = null, [FromQuery] bool? activa = null, CancellationToken ct = default)
    {
        var query = Visibles();
        if (activa is bool valor) query = query.Where(x => x.Activa == valor);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(z => EF.Functions.ILike(z.Nombre, texto)
                || Vinculos.Any(l => l.IdZonaEmpresarial == z.IdZonaEmpresarial && l.EsAdministradora && Db.Organizaciones.Any(o => o.IdOrganizacion == l.IdOrganizacion && EF.Functions.ILike(o.Nombre, texto))));
        }
        return Ok(await MapearAsync(await query.OrderBy(x => x.Nombre).ToListAsync(ct), ct));
    }

    [HttpGet("{id:int}")]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Consultar)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> Obtener(int id, CancellationToken ct)
    {
        var zona = await Visibles().SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, ct);
        return zona is null ? NoEncontrado("La zona empresarial no existe o no está en tu alcance.") : Ok((await MapearAsync([zona], ct))[0]);
    }

    /// <summary>Empresas que pueden vincularse: activas, que no son organización superior y sin otra zona activa.</summary>
    [HttpGet("empresas-disponibles")]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<EmpresaZonaDto>>> EmpresasDisponibles([FromQuery] int? idZona = null, CancellationToken ct = default)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma gestiona las empresas de una zona.");
        var superior = idZona is int z ? await SuperiorDeAsync(z, ct) : null;
        return Ok(await Db.Organizaciones
            .Where(o => o.Estado == "Activa" && o.IdOrganizacion != superior
                && !Vinculos.Any(l => l.IdOrganizacion == o.IdOrganizacion && l.Activa && (l.EsAdministradora || l.IdZonaEmpresarial != idZona)))
            .OrderBy(o => o.Nombre)
            .Select(o => new EmpresaZonaDto { IdOrganizacion = o.IdOrganizacion, Nombre = o.Nombre, Identificacion = o.Identificacion, Estado = o.Estado })
            .ToListAsync(ct));
    }

    [HttpPost]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Anadir)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> Crear([FromBody] ZonaEmpresarialAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede crear zonas empresariales.");
        var datos = DatosSuperior(request);
        if (await ValidarAsync(request, datos, null, null, ct) is { } error) return error;

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var organizacion = new Organizacion { CreadoEn = DateTime.UtcNow };
        ReglasOrganizacion.Aplicar(organizacion, datos);
        var zona = new ZonaEmpresarial { Nombre = request.Nombre.Trim(), Descripcion = Limpiar(request.Descripcion), Activa = request.Activa, CreadaEn = DateTime.UtcNow };
        Db.Organizaciones.Add(organizacion);
        Db.ZonasEmpresariales.Add(zona);
        await Db.SaveChangesAsync(ct);

        var empresas = request.IdEmpresas.Distinct().ToList();
        using (Db.PermitirEscrituraEn(empresas.Append(organizacion.IdOrganizacion)))
        {
            Db.ZonasEmpresarialesOrganizaciones.Add(new ZonaEmpresarialOrganizacion { IdOrganizacion = organizacion.IdOrganizacion, IdZonaEmpresarial = zona.IdZonaEmpresarial, EsAdministradora = true, Activa = true });
            foreach (var empresa in empresas)
                if (await ReglasOrganizacion.AsignarZonaAsync(Db, empresa, zona.IdZonaEmpresarial, ct) is { } invalido) return Invalido(invalido);
            Auditar(organizacion.IdOrganizacion, "Crear", "ZonaEmpresarial", zona.IdZonaEmpresarial.ToString(), null, new { zona.Nombre, organizacion = organizacion.Nombre, empresas });
            if (!await GuardarAsync(ct)) return Conflicto("Una de las empresas ya pertenece a otra zona empresarial.");
        }
        await transaccion.CommitAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = zona.IdZonaEmpresarial }, (await MapearAsync([zona], ct))[0]);
    }

    [HttpPut("{id:int}")]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Editar)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> Editar(int id, [FromBody] ZonaEmpresarialAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede editar zonas empresariales.");
        var zona = await Db.ZonasEmpresariales.SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, ct);
        if (zona is null) return NoEncontrado("La zona empresarial no existe.");
        var idSuperior = await SuperiorDeAsync(id, ct);
        var organizacion = idSuperior is int sup ? await Db.Organizaciones.SingleAsync(x => x.IdOrganizacion == sup, ct) : null;
        if (organizacion is null) return Conflicto("La zona no tiene una organización superior activa.");
        var datos = DatosSuperior(request);
        if (await ValidarAsync(request, datos, id, organizacion.IdOrganizacion, ct) is { } error) return error;

        var anterior = (await MapearAsync([zona], ct))[0];
        zona.Nombre = request.Nombre.Trim(); zona.Descripcion = Limpiar(request.Descripcion); zona.Activa = request.Activa;
        ReglasOrganizacion.Aplicar(organizacion, datos);

        var actuales = await Vinculos.Where(x => x.IdZonaEmpresarial == id && !x.EsAdministradora && x.Activa).Select(x => x.IdOrganizacion).ToListAsync(ct);
        var solicitadas = request.IdEmpresas.Distinct().ToList();
        var afectadas = actuales.Union(solicitadas).ToList();
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        using (Db.PermitirEscrituraEn(afectadas))
        {
            // Retirar no borra: el vínculo queda inactivo y se conserva la historia de la empresa en la zona.
            foreach (var retirada in actuales.Except(solicitadas))
                await ReglasOrganizacion.AsignarZonaAsync(Db, retirada, null, ct);
            foreach (var nueva in solicitadas.Except(actuales))
                if (await ReglasOrganizacion.AsignarZonaAsync(Db, nueva, id, ct) is { } invalido) return Invalido(invalido);
            Auditar(organizacion.IdOrganizacion, "Modificar", "ZonaEmpresarial", id.ToString(), anterior, new { zona.Nombre, zona.Activa, organizacion = organizacion.Nombre, empresas = solicitadas });
            if (!await GuardarAsync(ct)) return Conflicto("Una de las empresas ya pertenece a otra zona empresarial.");
        }
        await transaccion.CommitAsync(ct);
        Accesos.InvalidarOrganizacion(organizacion.IdOrganizacion);
        return Ok((await MapearAsync([zona], ct))[0]);
    }

    /// <summary>
    /// Activa o inactiva la zona junto con su organización superior. No borra vínculos, sedes, usuarios ni
    /// autorizaciones; las empresas conservan su organización y sus propios accesos.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    [RequiereModulo(CodigosModulo.ZonasEmpresariales, AccionModulo.Editar)]
    public async Task<ActionResult<ZonaEmpresarialAdministracionDto>> CambiarEstado(int id, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede activar o inactivar zonas empresariales.");
        var zona = await Db.ZonasEmpresariales.SingleOrDefaultAsync(x => x.IdZonaEmpresarial == id, ct);
        if (zona is null) return NoEncontrado("La zona empresarial no existe.");
        var activa = request.Activo!.Value;
        var anterior = zona.Activa;
        zona.Activa = activa;
        if (await SuperiorDeAsync(id, ct) is int idSuperior)
        {
            var organizacion = await Db.Organizaciones.SingleAsync(x => x.IdOrganizacion == idSuperior, ct);
            organizacion.Estado = activa ? "Activa" : "Inactiva";
            Accesos.InvalidarOrganizacion(idSuperior);
        }
        Auditar(null, activa ? "Activar" : "Inactivar", "ZonaEmpresarial", id.ToString(), new { Activa = anterior }, new { zona.Activa });
        await Db.SaveChangesAsync(ct);
        return Ok((await MapearAsync([zona], ct))[0]);
    }

    // ---------- Auxiliares ----------

    /// <summary>Plataforma: todas; los demás: zonas en las que su organización activa participa.</summary>
    private IQueryable<ZonaEmpresarial> Visibles()
    {
        if (EsPlataforma) return Db.ZonasEmpresariales;
        var orgId = OrganizacionActiva;
        return Db.ZonasEmpresariales.Where(z => Vinculos.Any(l => l.IdZonaEmpresarial == z.IdZonaEmpresarial && l.IdOrganizacion == orgId && l.Activa));
    }

    private Task<int?> SuperiorDeAsync(int idZona, CancellationToken ct) =>
        Vinculos.Where(x => x.IdZonaEmpresarial == idZona && x.EsAdministradora && x.Activa).Select(x => (int?)x.IdOrganizacion).SingleOrDefaultAsync(ct);

    /// <summary>El estado de la organización superior sigue al de la zona; no puede pertenecer a otra zona.</summary>
    private static OrganizacionAdministracionEntrada DatosSuperior(ZonaEmpresarialAdministracionEntrada request)
    {
        var datos = request.OrganizacionSuperior!;
        datos.Estado = request.Activa ? "Activa" : "Inactiva";
        datos.IdZonaEmpresarial = null;
        return datos;
    }

    private async Task<ActionResult?> ValidarAsync(ZonaEmpresarialAdministracionEntrada request, OrganizacionAdministracionEntrada datos, int? idZona, int? idSuperior, CancellationToken ct)
    {
        var nombre = request.Nombre.Trim().ToUpper();
        if (await Db.ZonasEmpresariales.AnyAsync(x => x.IdZonaEmpresarial != idZona && x.Nombre.ToUpper() == nombre, ct))
            return Conflicto("Ya existe una zona empresarial con ese nombre.");
        if (await ReglasOrganizacion.ValidarDatosAsync(Db, datos, ct) is { } invalido) return Invalido(invalido);
        if (await ReglasOrganizacion.DuplicadoAsync(Db, datos, idSuperior, ct) is { } duplicado) return Conflicto(duplicado);
        if (idSuperior is int sup && await ReglasOrganizacion.ZonaDeEmpresaAsync(Db, sup, ct) is not null)
            return Conflicto("La organización superior pertenece como empresa a una zona; retírela primero.");

        var empresas = request.IdEmpresas.Distinct().ToList();
        if (idSuperior is int propia && empresas.Contains(propia)) return Invalido("La organización superior no puede ser empresa de su propia zona.");
        var existentes = await Db.Organizaciones.Where(x => empresas.Contains(x.IdOrganizacion)).Select(x => new { x.IdOrganizacion, x.Estado }).ToListAsync(ct);
        if (existentes.Count != empresas.Count) return Invalido("Una o más empresas no existen.");
        var actuales = idZona is int z ? await Vinculos.Where(x => x.IdZonaEmpresarial == z && !x.EsAdministradora && x.Activa).Select(x => x.IdOrganizacion).ToListAsync(ct) : [];
        if (existentes.Any(x => x.Estado != "Activa" && !actuales.Contains(x.IdOrganizacion))) return Invalido("No se puede vincular una empresa inactiva.");
        var enOtraZona = await (from l in Vinculos
                                join o in Db.Organizaciones on l.IdOrganizacion equals o.IdOrganizacion
                                where empresas.Contains(l.IdOrganizacion) && l.Activa && (l.EsAdministradora || l.IdZonaEmpresarial != idZona)
                                select o.Nombre).FirstOrDefaultAsync(ct);
        return enOtraZona is null ? null : Conflicto($"{enOtraZona} ya pertenece a otra zona empresarial o es organización superior.");
    }

    private async Task<List<ZonaEmpresarialAdministracionDto>> MapearAsync(IReadOnlyList<ZonaEmpresarial> zonas, CancellationToken ct)
    {
        var ids = zonas.Select(x => x.IdZonaEmpresarial).ToArray();
        var vinculos = await Vinculos.Where(x => ids.Contains(x.IdZonaEmpresarial) && x.Activa).ToListAsync(ct);
        var superiores = vinculos.Where(x => x.EsAdministradora).Select(x => x.IdOrganizacion).ToList();
        var organizaciones = await ReglasOrganizacion.Proyectar(Db, Db.Organizaciones.Where(x => superiores.Contains(x.IdOrganizacion))).ToListAsync(ct);
        var idsEmpresas = vinculos.Where(x => !x.EsAdministradora).Select(x => x.IdOrganizacion).ToList();
        var empresas = await Db.Organizaciones.Where(x => idsEmpresas.Contains(x.IdOrganizacion))
            .Select(x => new EmpresaZonaDto { IdOrganizacion = x.IdOrganizacion, Nombre = x.Nombre, Identificacion = x.Identificacion, Estado = x.Estado }).ToListAsync(ct);
        var sedes = await Db.Sedes.IgnoreQueryFilters().Where(x => x.IdZonaEmpresarial != null && ids.Contains(x.IdZonaEmpresarial.Value))
            .GroupBy(x => x.IdZonaEmpresarial).Select(g => new { Id = g.Key, Total = g.Count() }).ToListAsync(ct);
        int? propia = EsPlataforma ? null : OrganizacionActiva;

        return zonas.Select(zona =>
        {
            var idSuperior = vinculos.SingleOrDefault(x => x.IdZonaEmpresarial == zona.IdZonaEmpresarial && x.EsAdministradora)?.IdOrganizacion;
            // Solo la plataforma y la organización superior ven qué empresas pertenecen a la zona.
            var verEmpresas = propia is null || propia == idSuperior;
            var miembros = vinculos.Where(x => x.IdZonaEmpresarial == zona.IdZonaEmpresarial && !x.EsAdministradora).Select(x => x.IdOrganizacion).ToHashSet();
            return new ZonaEmpresarialAdministracionDto
            {
                IdZonaEmpresarial = zona.IdZonaEmpresarial, Nombre = zona.Nombre, Descripcion = zona.Descripcion, Activa = zona.Activa, CreadaEn = zona.CreadaEn,
                TotalSedes = sedes.SingleOrDefault(x => x.Id == zona.IdZonaEmpresarial)?.Total ?? 0,
                OrganizacionSuperior = organizaciones.SingleOrDefault(x => x.IdOrganizacion == idSuperior),
                Empresas = verEmpresas ? empresas.Where(x => miembros.Contains(x.IdOrganizacion)).OrderBy(x => x.Nombre).ToList() : [],
            };
        }).ToList();
    }
}
