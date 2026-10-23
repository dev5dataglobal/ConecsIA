using ConecsIA.Application.Seguridad;
using System.Security.Claims;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using ApiConecsia.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

[Route("api/administracion")]
[RequiereModulo(CodigosModulo.Administracion)]
public sealed class AdministracionController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IPasswordHasher<Usuario> passwordHasher) : BaseApiController
{
    [HttpGet("usuarios")]
    public async Task<ActionResult<IEnumerable<UsuarioAdministracionDto>>> Usuarios([FromQuery] string? buscar = null, [FromQuery] string? estado = null, [FromQuery] int? organizacionId = null, CancellationToken cancellationToken = default)
    {
        if (!PuedeConsultarUsuarios()) return Forbid();
        var esPlataforma = AdministracionAuthorization.IsPlatform(User, "usuarios.consultar");
        var orgId = esPlataforma ? organizacionId : organizationContext.OrganizationId;
        if (!esPlataforma && orgId is null) return Forbid();
        var membresias = db.MembresiasOrganizacion.AsQueryable();
        if (esPlataforma) membresias = membresias.IgnoreQueryFilters();
        else membresias = membresias.Where(x => x.IdOrganizacion == orgId!.Value);
        if (estado is not null) membresias = membresias.Where(x => x.Estado == estado);
        var query = from membresia in membresias
                    join usuario in db.Usuarios on membresia.IdUsuario equals usuario.IdUsuario
                    join organizacion in db.Organizaciones on membresia.IdOrganizacion equals organizacion.IdOrganizacion
                    join persona in db.Personas on new { membresia.IdOrganizacion, IdPersona = membresia.IdPersona ?? 0 } equals new { persona.IdOrganizacion, persona.IdPersona } into personas
                    from persona in personas.DefaultIfEmpty()
                    where !buscar.HasValue() || usuario.Correo.Contains(buscar!) || (persona != null && (persona.Nombres + " " + persona.Apellidos).Contains(buscar!)) || (persona != null && persona.NumeroDocumento.Contains(buscar!))
                    select new { membresia, usuario, organizacion, persona };
        var rows = await query.OrderBy(x => x.usuario.Correo).Take(200).ToListAsync(cancellationToken);
        var membershipIds = rows.Select(x => x.membresia.IdMembresia).ToArray();
        var roleLinks = db.MembresiasRoles.AsQueryable();
        var roleCatalog = db.Roles.AsQueryable();
        if (esPlataforma) { roleLinks = roleLinks.IgnoreQueryFilters(); roleCatalog = roleCatalog.IgnoreQueryFilters(); }
        var roles = await (from membershipRole in roleLinks
                           join role in roleCatalog on membershipRole.IdRol equals role.IdRol
                           where membershipIds.Contains(membershipRole.IdMembresia)
                           select new { membershipRole.IdMembresia, role.Nombre }).ToListAsync(cancellationToken);
        return Ok(rows.Select(x => new UsuarioAdministracionDto { IdUsuario = x.usuario.IdUsuario, IdMembresia = x.membresia.IdMembresia, IdOrganizacion = x.membresia.IdOrganizacion, Organizacion = x.organizacion.Nombre, Correo = x.usuario.Correo, Nombre = x.persona is null ? null : $"{x.persona.Nombres} {x.persona.Apellidos}", Documento = x.persona?.NumeroDocumento, Perfiles = roles.Where(r => r.IdMembresia == x.membresia.IdMembresia).Select(r => r.Nombre).ToArray(), Estado = x.usuario.Activo && x.membresia.Estado == "Activa" ? "Activo" : x.membresia.Estado }));
    }

    [HttpPut("usuarios/{idMembresia:int}")]
    public async Task<IActionResult> EditarUsuario(int idMembresia, [FromBody] UsuarioAdministracionEdicion request, CancellationToken cancellationToken)
    {
        if (!AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.UsuariosModificar) && !AdministracionAuthorization.IsPlatform(User, "usuarios.modificar")) return Forbid();
        var plataforma = AdministracionAuthorization.IsPlatform(User, "usuarios.modificar");
        var membresias = db.MembresiasOrganizacion.AsQueryable();
        if (plataforma) membresias = membresias.IgnoreQueryFilters();
        else if (organizationContext.OrganizationId is int org) membresias = membresias.Where(x => x.IdOrganizacion == org);
        else return Forbid();
        var membresia = await membresias.SingleOrDefaultAsync(x => x.IdMembresia == idMembresia, cancellationToken);
        if (membresia is null) return NotFound();
        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.IdUsuario == membresia.IdUsuario, cancellationToken);
        if (usuario is null) return NotFound();
        var normalizado = Normalizar(request.Correo);
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.IdUsuario != usuario.IdUsuario && x.CorreoNormalizado == normalizado, cancellationToken)) return Conflict("El correo ya está registrado.");
        var roles = await ValidarRoles(membresia.IdOrganizacion, request.IdRoles, cancellationToken);
        if (roles is null) return Forbid();
        usuario.Correo = request.Correo.Trim(); usuario.CorreoNormalizado = normalizado; usuario.Activo = request.Activo; membresia.IdPersona = request.IdPersona; membresia.Estado = request.Activo ? "Activa" : "Inactiva";
        var existingRoles = await db.MembresiasRoles.Where(x => x.IdOrganizacion == membresia.IdOrganizacion && x.IdMembresia == membresia.IdMembresia).ToListAsync(cancellationToken);
        db.MembresiasRoles.RemoveRange(existingRoles);
        foreach (var role in roles) db.MembresiasRoles.Add(new MembresiaRol { IdOrganizacion = membresia.IdOrganizacion, IdMembresia = membresia.IdMembresia, IdRol = role.IdRol });
        RegistrarAuditoria(membresia.IdOrganizacion, "Modificar", "Usuario", usuario.IdUsuario.ToString(), "Exitoso");
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("usuarios")]
    public async Task<ActionResult<UsuarioAdministracionDto>> CrearUsuario([FromBody] UsuarioAdministracionEntrada request, CancellationToken cancellationToken)
    {
        if (!PuedeCrearUsuarios()) return Forbid();
        var orgId = ResolverOrganizacion(request.IdOrganizacion, "usuarios.crear");
        if (orgId is null) return Forbid();
        if (!await db.Organizaciones.AnyAsync(x => x.IdOrganizacion == orgId.Value && x.Estado == "Activa", cancellationToken)) return NotFound("La organización no existe o está inactiva.");
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.CorreoNormalizado == Normalizar(request.Correo), cancellationToken)) return Conflict("El correo ya está registrado.");
        var roles = await ValidarRoles(orgId.Value, request.IdRoles, cancellationToken);
        if (roles is null) return Forbid();
        var usuario = new Usuario { Correo = request.Correo.Trim(), CorreoNormalizado = Normalizar(request.Correo), CreadoEn = DateTime.UtcNow, Activo = request.Activo, SelloSeguridad = Guid.NewGuid().ToString("N") };
        usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Contrasena);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);
        var membresia = new MembresiaOrganizacion { IdOrganizacion = orgId.Value, IdUsuario = usuario.IdUsuario, IdPersona = request.IdPersona, Estado = request.Activo ? "Activa" : "Inactiva", CreadoEn = DateTime.UtcNow };
        db.MembresiasOrganizacion.Add(membresia);
        foreach (var role in roles) db.MembresiasRoles.Add(new MembresiaRol { IdOrganizacion = orgId.Value, IdMembresia = membresia.IdMembresia, IdRol = role.IdRol });
        RegistrarAuditoria(orgId, "Crear", "Usuario", usuario.IdUsuario.ToString(), "Exitoso");
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Usuarios), new { }, new UsuarioAdministracionDto { IdUsuario = usuario.IdUsuario, IdMembresia = membresia.IdMembresia, IdOrganizacion = orgId.Value, Correo = usuario.Correo, Estado = membresia.Estado, Perfiles = roles.Select(x => x.Nombre).ToArray() });
    }

    [HttpGet("organizaciones")]
    public async Task<ActionResult<IEnumerable<OrganizacionDto>>> Organizaciones(CancellationToken cancellationToken)
    {
        if (!PuedeConsultarOrganizaciones()) return Forbid();
        var query = db.Organizaciones.AsQueryable();
        if (!AdministracionAuthorization.IsPlatform(User, "organizaciones.consultar")) { if (organizationContext.OrganizationId is null) return Forbid(); query = query.Where(x => x.IdOrganizacion == organizationContext.OrganizationId.Value); }
        return Ok(await query.OrderBy(x => x.Nombre).Select(ToOrganizacion).ToListAsync(cancellationToken));
    }

    [HttpPost("organizaciones")]
    public async Task<ActionResult<OrganizacionDto>> CrearOrganizacion([FromBody] OrganizacionAdministracionEntrada request, CancellationToken cancellationToken)
    {
        if (!AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.OrganizacionesAdministrar)) return Forbid();
        if (await db.Organizaciones.AnyAsync(x => x.Nombre.ToUpper() == request.Nombre.Trim().ToUpper(), cancellationToken)) return Conflict("Ya existe una organización con ese nombre.");
        var entity = new Organizacion { Nombre = request.Nombre.Trim(), IdTipoOrganizacion = request.IdTipoOrganizacion, IdPlan = request.IdPlan, Identificacion = request.Identificacion?.Trim(), Correo = request.Correo?.Trim(), Direccion = request.Direccion?.Trim(), Estado = request.Estado is "Activa" or "Inactiva" ? request.Estado : "Activa", CreadoEn = DateTime.UtcNow };
        db.Organizaciones.Add(entity);
        RegistrarAuditoria(null, "Crear", "Organizacion", entity.IdOrganizacion.ToString(), "Exitoso");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToOrganizacion.Compile().Invoke(entity));
    }

    [HttpPut("organizaciones/{organizacionId:int}")]
    public async Task<IActionResult> EditarOrganizacion(int organizacionId, [FromBody] OrganizacionAdministracionEntrada request, CancellationToken cancellationToken)
    {
        if (!AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.OrganizacionesAdministrar)) return Forbid();
        var entity = await db.Organizaciones.SingleOrDefaultAsync(x => x.IdOrganizacion == organizacionId, cancellationToken);
        if (entity is null) return NotFound();
        if (await db.Organizaciones.AnyAsync(x => x.IdOrganizacion != organizacionId && x.Nombre.ToUpper() == request.Nombre.Trim().ToUpper(), cancellationToken)) return Conflict("Ya existe una organización con ese nombre.");
        entity.Nombre = request.Nombre.Trim(); entity.IdTipoOrganizacion = request.IdTipoOrganizacion; entity.IdPlan = request.IdPlan; entity.Identificacion = request.Identificacion?.Trim(); entity.Correo = request.Correo?.Trim(); entity.Direccion = request.Direccion?.Trim(); entity.Estado = request.Estado is "Activa" or "Inactiva" ? request.Estado : entity.Estado;
        RegistrarAuditoria(organizacionId, "Modificar", "Organizacion", organizacionId.ToString(), "Exitoso");
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("organizaciones/{organizacionId:int}/perfiles")]
    public async Task<ActionResult<IEnumerable<PerfilAdministracionDto>>> Perfiles(int organizacionId, CancellationToken cancellationToken)
    {
        if (!PuedeConsultarPerfiles(organizacionId)) return Forbid();
        var rolesQuery = db.Roles.AsQueryable(); var linksQuery = db.RolesPermisos.AsQueryable();
        if (AdministracionAuthorization.IsPlatform(User, "roles.consultar")) { rolesQuery = rolesQuery.IgnoreQueryFilters(); linksQuery = linksQuery.IgnoreQueryFilters(); }
        var roles = await rolesQuery.Where(x => x.IdOrganizacion == organizacionId).OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
        var permisos = await (from link in linksQuery where link.IdOrganizacion == organizacionId join permiso in db.Permisos on link.IdPermiso equals permiso.IdPermiso select new { link.IdRol, permiso }).ToListAsync(cancellationToken);
        return Ok(roles.Select(role => new PerfilAdministracionDto { IdRol = role.IdRol, IdOrganizacion = role.IdOrganizacion, Nombre = role.Nombre, Descripcion = role.Descripcion, Activo = role.Activo, Permisos = permisos.Where(x => x.IdRol == role.IdRol).Select(x => new PermisoDto { IdPermiso = x.permiso.IdPermiso, Codigo = x.permiso.Codigo, Descripcion = x.permiso.Descripcion, Activo = x.permiso.Activo }).ToArray() }));
    }

    [HttpPut("organizaciones/{organizacionId:int}/perfiles/{idRol:int}")]
    public async Task<IActionResult> EditarPerfil(int organizacionId, int idRol, [FromBody] PerfilAdministracionEntrada request, CancellationToken cancellationToken)
    {
        if (!PuedeAdministrarPerfiles(organizacionId)) return Forbid();
        var role = await db.Roles.SingleOrDefaultAsync(x => x.IdOrganizacion == organizacionId && x.IdRol == idRol, cancellationToken);
        if (role is null) return NotFound();
        if (await db.Roles.AnyAsync(x => x.IdOrganizacion == organizacionId && x.IdRol != idRol && x.Nombre.ToUpper() == request.Nombre.Trim().ToUpper(), cancellationToken)) return Conflict("Ya existe un perfil con ese nombre.");
        var permisos = await ValidarPermisos(request.IdPermisos, cancellationToken); if (permisos is null) return Forbid();
        role.Nombre = request.Nombre.Trim(); role.Descripcion = request.Descripcion?.Trim(); role.Activo = request.Activo;
        var links = await db.RolesPermisos.Where(x => x.IdOrganizacion == organizacionId && x.IdRol == idRol).ToListAsync(cancellationToken); db.RolesPermisos.RemoveRange(links);
        foreach (var permiso in permisos) db.RolesPermisos.Add(new RolPermiso { IdOrganizacion = organizacionId, IdRol = idRol, IdPermiso = permiso.IdPermiso });
        RegistrarAuditoria(organizacionId, "Modificar", "Rol", idRol.ToString(), "Exitoso"); await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    [HttpPost("organizaciones/{organizacionId:int}/perfiles")]
    public async Task<ActionResult<PerfilAdministracionDto>> CrearPerfil(int organizacionId, [FromBody] PerfilAdministracionEntrada request, CancellationToken cancellationToken)
    {
        if (!PuedeAdministrarPerfiles(organizacionId)) return Forbid();
        if (await db.Roles.AnyAsync(x => x.IdOrganizacion == organizacionId && x.Nombre.ToUpper() == request.Nombre.Trim().ToUpper(), cancellationToken)) return Conflict("Ya existe un perfil con ese nombre.");
        var permisos = await ValidarPermisos(request.IdPermisos, cancellationToken);
        if (permisos is null) return Forbid();
        var role = new Rol { IdOrganizacion = organizacionId, Nombre = request.Nombre.Trim(), Descripcion = request.Descripcion?.Trim(), Activo = request.Activo };
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var permiso in permisos) db.RolesPermisos.Add(new RolPermiso { IdOrganizacion = organizacionId, IdRol = role.IdRol, IdPermiso = permiso.IdPermiso });
        RegistrarAuditoria(organizacionId, "Crear", "Rol", role.IdRol.ToString(), "Exitoso");
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new PerfilAdministracionDto { IdRol = role.IdRol, IdOrganizacion = role.IdOrganizacion, Nombre = role.Nombre, Descripcion = role.Descripcion, Activo = role.Activo, Permisos = permisos.Select(x => new PermisoDto { IdPermiso = x.IdPermiso, Codigo = x.Codigo, Descripcion = x.Descripcion, Activo = x.Activo }).ToArray() });
    }

    private bool PuedeConsultarUsuarios() => AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.UsuariosConsultar) || AdministracionAuthorization.IsPlatform(User, "usuarios.consultar");
    private bool PuedeCrearUsuarios() => AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.UsuariosCrear) || AdministracionAuthorization.IsPlatform(User, "usuarios.crear");
    private bool PuedeConsultarOrganizaciones() => AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.OrganizacionesConsultar) || AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.ConfiguracionConsultar);
    private bool PuedeConsultarPerfiles(int orgId) => AdministracionAuthorization.IsPlatform(User, "roles.consultar") || (organizationContext.OrganizationId == orgId && AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.RolesConsultar));
    private bool PuedeAdministrarPerfiles(int orgId) => AdministracionAuthorization.IsPlatform(User, "roles.administrar") || (organizationContext.OrganizationId == orgId && AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.RolesAdministrar));
    private int? ResolverOrganizacion(int? requested, string action) => AdministracionAuthorization.IsPlatform(User, action) ? requested : organizationContext.OrganizationId;
    private async Task<List<Rol>?> ValidarRoles(int orgId, IReadOnlyList<int> roleIds, CancellationToken cancellationToken) { var query = db.Roles.AsQueryable(); if (AdministracionAuthorization.IsPlatform(User, "usuarios.crear") || AdministracionAuthorization.IsPlatform(User, "usuarios.modificar")) query = query.IgnoreQueryFilters(); var roles = await query.Where(x => x.IdOrganizacion == orgId && x.Activo && roleIds.Contains(x.IdRol)).ToListAsync(cancellationToken); return roles.Count == roleIds.Distinct().Count() ? roles : null; }
    private async Task<List<Permiso>?> ValidarPermisos(IReadOnlyList<int> ids, CancellationToken cancellationToken) { var permisos = await db.Permisos.Where(x => x.Activo && ids.Contains(x.IdPermiso)).ToListAsync(cancellationToken); return permisos.Count == ids.Distinct().Count() && (AdministracionAuthorization.HasPermission(User, AdministracionAuthorization.PermisosAdministrar) || permisos.All(x => User.HasClaim("permission", x.Codigo))) ? permisos : null; }
    private void RegistrarAuditoria(int? orgId, string accion, string entidad, string registro, string resultado) { var subject = User.FindFirstValue("sub"); db.Auditorias.Add(new Auditoria { IdOrganizacion = orgId, IdUsuario = int.TryParse(subject, out var userId) ? userId : null, TipoActor = "Usuario", Accion = accion, Entidad = entidad, IdRegistro = registro, Resultado = resultado, OcurridoEn = DateTime.UtcNow }); }
    private static string Normalizar(string correo) => correo.Trim().ToUpperInvariant();
    private static readonly System.Linq.Expressions.Expression<Func<Organizacion, OrganizacionDto>> ToOrganizacion = x => new OrganizacionDto { IdOrganizacion = x.IdOrganizacion, IdTipoOrganizacion = x.IdTipoOrganizacion, IdPlan = x.IdPlan, IdArchivoLogo = x.IdArchivoLogo, Nombre = x.Nombre, Identificacion = x.Identificacion, Direccion = x.Direccion, Telefono = x.Telefono, Correo = x.Correo, Estado = x.Estado, CreadoEn = x.CreadoEn };
}

file static class NullableStringExtensions { public static bool HasValue(this string? value) => !string.IsNullOrWhiteSpace(value); }
