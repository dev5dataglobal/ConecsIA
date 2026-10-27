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
/// Organizaciones. El administrador de plataforma consulta y gestiona todas (catálogo de clientes, crea, inactiva,
/// cambia plan y organización superior). Cualquier otro usuario solo ve y edita los datos de su organización activa.
/// Inactivar no borra usuarios, sedes, autorizaciones ni historia: solo bloquea el ingreso a la organización.
/// </summary>
[Route("api/administracion/organizaciones")]
public sealed class OrganizacionesAdministracionController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos,
    AlmacenamientoArchivos almacenamiento) : AdministracionControllerBase(db, organizationContext, accesos)
{
    [HttpGet]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<OrganizacionAdministracionDto>>> Listar([FromQuery] string? buscar = null, [FromQuery] string? estado = null, [FromQuery] int? idTipoOrganizacion = null, CancellationToken ct = default)
    {
        var query = Visibles();
        if (estado is "Activa" or "Inactiva") query = query.Where(x => x.Estado == estado);
        if (idTipoOrganizacion is int tipo) query = query.Where(x => x.IdTipoOrganizacion == tipo);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, texto) || (x.Identificacion != null && EF.Functions.ILike(x.Identificacion, texto)) || (x.Correo != null && EF.Functions.ILike(x.Correo, texto)));
        }
        return Ok(await ReglasOrganizacion.Proyectar(Db, query.OrderBy(x => x.Nombre)).ToListAsync(ct));
    }

    [HttpGet("{idOrganizacion:int}")]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Consultar)]
    public async Task<ActionResult<OrganizacionAdministracionDto>> Obtener(int idOrganizacion, CancellationToken ct)
    {
        var dto = await ReglasOrganizacion.Proyectar(Db, Visibles().Where(x => x.IdOrganizacion == idOrganizacion)).SingleOrDefaultAsync(ct);
        return dto is null ? NoEncontrado("La organización no existe o no está en tu alcance.") : Ok(dto);
    }

    /// <summary>Catálogos del formulario (tipos, planes y zonas empresariales activas). Requiere Añadir o Editar.</summary>
    [HttpGet("catalogos")]
    public async Task<ActionResult<object>> Catalogos(CancellationToken ct)
    {
        if (!HayOrganizacionActiva) return Prohibido("No hay una organización activa.");
        var propios = await AccesosPropiosAsync(ct);
        if (!propios.TryGetValue(CodigosModulo.Organizaciones, out var acciones) || !(acciones.Anadir || acciones.Editar))
            return Prohibido("Necesitas permiso para añadir o editar organizaciones.");
        var links = Db.ZonasEmpresarialesOrganizaciones.IgnoreQueryFilters();
        return Ok(new
        {
            tipos = await Db.TiposOrganizacion.Where(x => x.Activo).OrderBy(x => x.Nombre).Select(x => new OpcionCatalogoDto { Id = x.IdTipoOrganizacion, Nombre = x.Nombre }).ToListAsync(ct),
            planes = await Db.Planes.Where(x => x.Activo).OrderBy(x => x.Nombre).Select(x => new OpcionCatalogoDto { Id = x.IdPlan, Nombre = x.Nombre }).ToListAsync(ct),
            zonas = await (from zona in Db.ZonasEmpresariales
                           where zona.Activa
                           let superior = (from l in links
                                           join org in Db.Organizaciones on l.IdOrganizacion equals org.IdOrganizacion
                                           where l.IdZonaEmpresarial == zona.IdZonaEmpresarial && l.EsAdministradora && l.Activa
                                           select org.Nombre).FirstOrDefault()
                           orderby zona.Nombre
                           select new OpcionCatalogoDto { Id = zona.IdZonaEmpresarial, Nombre = superior == null ? zona.Nombre : zona.Nombre + " · " + superior }).ToListAsync(ct),
        });
    }

    [HttpPost]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Anadir)]
    public async Task<ActionResult<OrganizacionAdministracionDto>> Crear([FromBody] OrganizacionAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede crear organizaciones.");
        if (await ReglasOrganizacion.ValidarDatosAsync(Db, request, ct) is { } invalido) return Invalido(invalido);
        if (await ReglasOrganizacion.DuplicadoAsync(Db, request, null, ct) is { } duplicado) return Conflicto(duplicado);

        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        var entity = new Organizacion { CreadoEn = DateTime.UtcNow };
        ReglasOrganizacion.Aplicar(entity, request);
        Db.Organizaciones.Add(entity);
        await Db.SaveChangesAsync(ct);
        using (Db.PermitirEscrituraEn([entity.IdOrganizacion]))
        {
            if (await ReglasOrganizacion.AsignarZonaAsync(Db, entity.IdOrganizacion, request.IdZonaEmpresarial, ct) is { } error) return Invalido(error);
            await ReglasPersona.SembrarTiposPersonaAsync(Db, entity.IdOrganizacion, ct);
            Auditar(entity.IdOrganizacion, "Crear", "Organizacion", entity.IdOrganizacion.ToString(), null, request);
            if (!await GuardarAsync(ct)) return Conflicto("La empresa ya pertenece a otra zona empresarial.");
        }
        await transaccion.CommitAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { idOrganizacion = entity.IdOrganizacion }, await ProyectarUnaAsync(entity.IdOrganizacion, ct));
    }

    [HttpPut("{idOrganizacion:int}")]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Editar)]
    public async Task<ActionResult<OrganizacionAdministracionDto>> Editar(int idOrganizacion, [FromBody] OrganizacionAdministracionEntrada request, CancellationToken ct)
    {
        var entity = await Visibles().SingleOrDefaultAsync(x => x.IdOrganizacion == idOrganizacion, ct);
        if (entity is null) return NoEncontrado("La organización no existe o no está en tu alcance.");
        var zonaActual = (await ReglasOrganizacion.ZonaDeEmpresaAsync(Db, idOrganizacion, ct))?.IdZonaEmpresarial;
        if (!EsPlataforma && (request.Estado != entity.Estado || request.IdPlan != entity.IdPlan || request.IdZonaEmpresarial != zonaActual))
            return Prohibido("Solo el administrador de plataforma puede cambiar el estado, el plan o la organización superior.");
        if (await ReglasOrganizacion.ValidarDatosAsync(Db, request, ct) is { } invalido) return Invalido(invalido);
        if (await ReglasOrganizacion.DuplicadoAsync(Db, request, idOrganizacion, ct) is { } duplicado) return Conflicto(duplicado);

        var anterior = await ProyectarUnaAsync(idOrganizacion, ct);
        ReglasOrganizacion.Aplicar(entity, request);
        using (Db.PermitirEscrituraEn([idOrganizacion]))
        {
            if (await ReglasOrganizacion.AsignarZonaAsync(Db, idOrganizacion, request.IdZonaEmpresarial, ct) is { } error) return Invalido(error);
            Auditar(idOrganizacion, "Modificar", "Organizacion", idOrganizacion.ToString(), anterior, request);
            if (!await GuardarAsync(ct)) return Conflicto("La empresa ya pertenece a otra zona empresarial.");
        }
        return Ok(await ProyectarUnaAsync(idOrganizacion, ct));
    }

    [HttpPatch("{idOrganizacion:int}/estado")]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Editar)]
    public async Task<ActionResult<OrganizacionAdministracionDto>> CambiarEstado(int idOrganizacion, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        if (!EsPlataforma) return Prohibido("Solo el administrador de plataforma puede activar o inactivar organizaciones.");
        var entity = await Db.Organizaciones.SingleOrDefaultAsync(x => x.IdOrganizacion == idOrganizacion, ct);
        if (entity is null) return NoEncontrado("La organización no existe.");
        var anterior = entity.Estado;
        entity.Estado = request.Activo!.Value ? "Activa" : "Inactiva";
        Auditar(idOrganizacion, request.Activo.Value ? "Activar" : "Inactivar", "Organizacion", idOrganizacion.ToString(), new { Estado = anterior }, new { entity.Estado });
        await Db.SaveChangesAsync(ct);
        Accesos.InvalidarOrganizacion(idOrganizacion);
        return Ok(await ProyectarUnaAsync(idOrganizacion, ct));
    }

    [HttpGet("{idOrganizacion:int}/logo")]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Consultar)]
    public async Task<IActionResult> Logo(int idOrganizacion, CancellationToken ct)
    {
        var idArchivo = await Visibles().Where(x => x.IdOrganizacion == idOrganizacion).Select(x => x.IdArchivoLogo).SingleOrDefaultAsync(ct);
        if (idArchivo is not Guid id || await almacenamiento.AbrirAsync(id, ct) is not { } archivo) return NoEncontrado("La organización no tiene logo.");
        Response.Headers.CacheControl = "private, max-age=300";
        return File(archivo.Contenido, archivo.Archivo.TipoContenido);
    }

    /// <summary>Carga o reemplaza el logo (PNG, JPEG o WebP, máximo 2 MB). El archivo anterior se conserva como historia.</summary>
    [HttpPost("{idOrganizacion:int}/logo")]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Editar)]
    [RequestSizeLimit(AlmacenamientoArchivos.TamanoMaximoImagen + 64 * 1024)]
    public async Task<ActionResult<OrganizacionAdministracionDto>> CargarLogo(int idOrganizacion, IFormFile? archivo, CancellationToken ct)
    {
        var entity = await Visibles().SingleOrDefaultAsync(x => x.IdOrganizacion == idOrganizacion, ct);
        if (entity is null) return NoEncontrado("La organización no existe o no está en tu alcance.");
        if (archivo is null || archivo.Length == 0) return Invalido("Seleccione una imagen.");
        if (archivo.Length > AlmacenamientoArchivos.TamanoMaximoImagen) return Invalido("El logo no puede superar 2 MB.");
        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        var contenido = memoria.ToArray();
        var tipo = AlmacenamientoArchivos.TipoImagen(contenido);
        if (tipo is null) return Invalido("El logo debe ser una imagen PNG, JPEG o WebP.");

        var guardado = await almacenamiento.GuardarAsync(contenido, tipo, idOrganizacion, User.IdUsuario()!.Value, ct);
        var anterior = entity.IdArchivoLogo;
        entity.IdArchivoLogo = guardado.IdArchivo;
        Auditar(idOrganizacion, "CargarLogo", "Organizacion", idOrganizacion.ToString(), new { IdArchivoLogo = anterior }, new { entity.IdArchivoLogo });
        await Db.SaveChangesAsync(ct);
        return Ok(await ProyectarUnaAsync(idOrganizacion, ct));
    }

    [HttpDelete("{idOrganizacion:int}/logo")]
    [RequiereModulo(CodigosModulo.Organizaciones, AccionModulo.Editar)]
    public async Task<ActionResult<OrganizacionAdministracionDto>> QuitarLogo(int idOrganizacion, CancellationToken ct)
    {
        var entity = await Visibles().SingleOrDefaultAsync(x => x.IdOrganizacion == idOrganizacion, ct);
        if (entity is null) return NoEncontrado("La organización no existe o no está en tu alcance.");
        if (entity.IdArchivoLogo is not null)
        {
            Auditar(idOrganizacion, "QuitarLogo", "Organizacion", idOrganizacion.ToString(), new { entity.IdArchivoLogo }, null);
            entity.IdArchivoLogo = null;
            await Db.SaveChangesAsync(ct);
        }
        return Ok(await ProyectarUnaAsync(idOrganizacion, ct));
    }

    /// <summary>Plataforma: todas; los demás: solo la organización activa (nunca la indicada por el cliente).</summary>
    private IQueryable<Organizacion> Visibles() => EsPlataforma
        ? Db.Organizaciones
        : Db.Organizaciones.Where(x => x.IdOrganizacion == OrganizacionActiva);

    private Task<OrganizacionAdministracionDto> ProyectarUnaAsync(int idOrganizacion, CancellationToken ct) =>
        ReglasOrganizacion.Proyectar(Db, Db.Organizaciones.Where(x => x.IdOrganizacion == idOrganizacion)).SingleAsync(ct);
}
