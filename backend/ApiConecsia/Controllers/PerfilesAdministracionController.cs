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
/// Perfiles (roles) de la organización activa. La matriz de acciones por módulo se gestiona en
/// <c>PUT /api/roles/{idRol}/modulos</c> (<see cref="ModuloPermisoController"/>).
/// Inactivar un perfil no borra asignaciones: sus usuarios dejan de recibir sus permisos hasta reactivarlo.
/// </summary>
[Route("api/administracion/perfiles")]
public sealed class PerfilesAdministracionController(ConecsiaDbContext db, IOrganizationContext organizationContext, IModuloAccesoService accesos)
    : AdministracionControllerBase(db, organizationContext, accesos)
{
    [HttpGet]
    [RequiereModulo(CodigosModulo.Perfiles, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<PerfilAdministracionDto>>> Listar([FromQuery] string? buscar = null, [FromQuery] string? estado = null, CancellationToken ct = default)
    {
        var query = Db.Roles.Where(x => x.IdOrganizacion == OrganizacionActiva);
        if (estado == "Activo") query = query.Where(x => x.Activo);
        else if (estado == "Inactivo") query = query.Where(x => !x.Activo);
        if (!string.IsNullOrWhiteSpace(buscar)) query = query.Where(x => EF.Functions.ILike(x.Nombre, $"%{buscar.Trim()}%"));
        return Ok(await MapearAsync(await query.OrderBy(x => x.Nombre).ToListAsync(ct), ct));
    }

    [HttpGet("{idRol:int}")]
    [RequiereModulo(CodigosModulo.Perfiles, AccionModulo.Consultar)]
    public async Task<ActionResult<PerfilAdministracionDto>> Obtener(int idRol, CancellationToken ct)
    {
        var rol = await Db.Roles.SingleOrDefaultAsync(x => x.IdRol == idRol, ct);
        return rol is null ? NoEncontrado("El perfil no existe en la organización activa.") : Ok((await MapearAsync([rol], ct))[0]);
    }

    [HttpPost]
    [RequiereModulo(CodigosModulo.Perfiles, AccionModulo.Anadir)]
    public async Task<ActionResult<PerfilAdministracionDto>> Crear([FromBody] PerfilAdministracionEntrada request, CancellationToken ct)
    {
        var orgId = OrganizacionActiva;
        var nombre = request.Nombre.Trim();
        if (await NombreOcupadoAsync(nombre, null, ct)) return Conflicto("Ya existe un perfil con ese nombre.");
        var permisos = request.IdPermisos is null ? [] : await ValidarPermisosAsync(request.IdPermisos, ct);
        if (permisos is null) return Prohibido("Solo puedes otorgar permisos que tienes.");

        var rol = new Rol { IdOrganizacion = orgId, Nombre = nombre, Descripcion = Limpiar(request.Descripcion), Activo = request.Activo };
        Db.Roles.Add(rol);
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un perfil con ese nombre.");
        foreach (var permiso in permisos) Db.RolesPermisos.Add(new RolPermiso { IdOrganizacion = orgId, IdRol = rol.IdRol, IdPermiso = permiso.IdPermiso });
        Auditar(orgId, "Crear", "Rol", rol.IdRol.ToString(), null, new { rol.Nombre, rol.Activo });
        await Db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { idRol = rol.IdRol }, (await MapearAsync([rol], ct))[0]);
    }

    [HttpPut("{idRol:int}")]
    [RequiereModulo(CodigosModulo.Perfiles, AccionModulo.Editar)]
    public async Task<ActionResult<PerfilAdministracionDto>> Editar(int idRol, [FromBody] PerfilAdministracionEntrada request, CancellationToken ct)
    {
        var orgId = OrganizacionActiva;
        var rol = await Db.Roles.SingleOrDefaultAsync(x => x.IdRol == idRol, ct);
        if (rol is null) return NoEncontrado("El perfil no existe en la organización activa.");
        var nombre = request.Nombre.Trim();
        if (await NombreOcupadoAsync(nombre, idRol, ct)) return Conflicto("Ya existe un perfil con ese nombre.");

        var anterior = new { rol.Nombre, rol.Descripcion, rol.Activo };
        if (request.IdPermisos is not null)
        {
            var permisos = await ValidarPermisosAsync(request.IdPermisos, ct);
            if (permisos is null) return Prohibido("Solo puedes otorgar permisos que tienes.");
            Db.RolesPermisos.RemoveRange(await Db.RolesPermisos.Where(x => x.IdRol == idRol).ToListAsync(ct));
            foreach (var permiso in permisos) Db.RolesPermisos.Add(new RolPermiso { IdOrganizacion = orgId, IdRol = idRol, IdPermiso = permiso.IdPermiso });
        }
        rol.Nombre = nombre; rol.Descripcion = Limpiar(request.Descripcion); rol.Activo = request.Activo;
        Auditar(orgId, "Modificar", "Rol", idRol.ToString(), anterior, new { rol.Nombre, rol.Descripcion, rol.Activo });
        if (!await GuardarAsync(ct)) return Conflicto("Ya existe un perfil con ese nombre.");
        Accesos.InvalidarOrganizacion(orgId);
        return Ok((await MapearAsync([rol], ct))[0]);
    }

    [HttpPatch("{idRol:int}/estado")]
    [RequiereModulo(CodigosModulo.Perfiles, AccionModulo.Editar)]
    public async Task<ActionResult<PerfilAdministracionDto>> CambiarEstado(int idRol, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var orgId = OrganizacionActiva;
        var rol = await Db.Roles.SingleOrDefaultAsync(x => x.IdRol == idRol, ct);
        if (rol is null) return NoEncontrado("El perfil no existe en la organización activa.");
        var anterior = rol.Activo;
        rol.Activo = request.Activo!.Value;
        Auditar(orgId, rol.Activo ? "Activar" : "Inactivar", "Rol", idRol.ToString(), new { Activo = anterior }, new { rol.Activo });
        await Db.SaveChangesAsync(ct);
        Accesos.InvalidarOrganizacion(orgId);
        return Ok((await MapearAsync([rol], ct))[0]);
    }

    private Task<bool> NombreOcupadoAsync(string nombre, int? excluir, CancellationToken ct) =>
        Db.Roles.AnyAsync(x => x.IdOrganizacion == OrganizacionActiva && x.IdRol != excluir && x.Nombre.ToUpper() == nombre.ToUpper(), ct);

    /// <summary>Permisos por código: solo los activos y, sin «permisos.administrar», solo los que el usuario ya tiene.</summary>
    private async Task<List<Permiso>?> ValidarPermisosAsync(IReadOnlyList<int> ids, CancellationToken ct)
    {
        var distintos = ids.Distinct().ToList();
        var permisos = await Db.Permisos.Where(x => x.Activo && distintos.Contains(x.IdPermiso)).ToListAsync(ct);
        var propios = EsPlataforma || AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.PermisosAdministrar)
            || permisos.All(x => AdministracionAuthorization.HasPermission(User, x.Codigo));
        return permisos.Count == distintos.Count && propios ? permisos : null;
    }

    private async Task<List<PerfilAdministracionDto>> MapearAsync(IReadOnlyList<Rol> roles, CancellationToken ct)
    {
        var ids = roles.Select(x => x.IdRol).ToArray();
        var modulos = await (from mp in Db.ModulosPermisos
                             join modulo in Db.AppModulos on mp.IdAppModulo equals modulo.IdAppModulo
                             where ids.Contains(mp.IdRol) && modulo.Activo && (mp.PuedeConsultar || mp.PuedeAnadir || mp.PuedeEditar)
                             group mp by mp.IdRol into g
                             select new { IdRol = g.Key, Total = g.Count() }).ToListAsync(ct);
        var usuarios = await (from link in Db.MembresiasRoles
                              join membresia in Db.MembresiasOrganizacion on link.IdMembresia equals membresia.IdMembresia
                              where ids.Contains(link.IdRol) && membresia.Estado == "Activa"
                              group link by link.IdRol into g
                              select new { IdRol = g.Key, Total = g.Count() }).ToListAsync(ct);
        var permisos = await (from link in Db.RolesPermisos
                              join permiso in Db.Permisos on link.IdPermiso equals permiso.IdPermiso
                              where ids.Contains(link.IdRol)
                              select new { link.IdRol, permiso }).ToListAsync(ct);
        return roles.Select(rol => new PerfilAdministracionDto
        {
            IdRol = rol.IdRol, IdOrganizacion = rol.IdOrganizacion, Nombre = rol.Nombre, Descripcion = rol.Descripcion, Activo = rol.Activo,
            TotalModulos = modulos.SingleOrDefault(x => x.IdRol == rol.IdRol)?.Total ?? 0,
            TotalUsuarios = usuarios.SingleOrDefault(x => x.IdRol == rol.IdRol)?.Total ?? 0,
            Permisos = permisos.Where(x => x.IdRol == rol.IdRol).Select(x => new PermisoDto { IdPermiso = x.permiso.IdPermiso, Codigo = x.permiso.Codigo, Descripcion = x.permiso.Descripcion, Activo = x.permiso.Activo }).ToArray(),
        }).ToList();
    }
}
