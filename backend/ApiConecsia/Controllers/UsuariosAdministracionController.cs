using ApiConecsia.Helpers;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Seguridad;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Controllers;

/// <summary>
/// Usuarios de la organización activa (membresías). Un usuario se identifica globalmente por su correo;
/// activar/inactivar actúa sobre su membresía en esta organización y no afecta a otras.
/// </summary>
[Route("api/administracion/usuarios")]
public sealed class UsuariosAdministracionController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos,
    IPasswordHasher<Usuario> passwordHasher) : AdministracionControllerBase(db, organizationContext, accesos)
{
    [HttpGet]
    [RequiereModulo(CodigosModulo.Usuarios, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<UsuarioAdministracionDto>>> Listar([FromQuery] string? buscar = null, [FromQuery] string? estado = null, [FromQuery] int? idRol = null, CancellationToken ct = default)
    {
        var orgId = OrganizacionActiva;
        var query = Consulta(orgId);
        if (estado == "Activo") query = query.Where(x => x.Usuario.Activo && x.Membresia.Estado == "Activa");
        else if (estado == "Inactivo") query = query.Where(x => !x.Usuario.Activo || x.Membresia.Estado != "Activa");
        if (idRol is int rol) query = query.Where(x => Db.MembresiasRoles.Any(r => r.IdMembresia == x.Membresia.IdMembresia && r.IdRol == rol));
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Usuario.Correo, texto)
                || (x.Persona != null && (EF.Functions.ILike(x.Persona.Nombres + " " + x.Persona.Apellidos, texto) || EF.Functions.ILike(x.Persona.NumeroDocumento, texto))));
        }
        var filas = await query.OrderBy(x => x.Usuario.Correo).Take(500).ToListAsync(ct);
        return Ok(await MapearAsync(filas, ct));
    }

    [HttpGet("{idMembresia:int}")]
    [RequiereModulo(CodigosModulo.Usuarios, AccionModulo.Consultar)]
    public async Task<ActionResult<UsuarioAdministracionDto>> Obtener(int idMembresia, CancellationToken ct)
    {
        var fila = await Consulta(OrganizacionActiva).SingleOrDefaultAsync(x => x.Membresia.IdMembresia == idMembresia, ct);
        return fila is null ? NoEncontrado("El usuario no existe en la organización activa.") : Ok((await MapearAsync([fila], ct))[0]);
    }

    /// <summary>Perfiles activos de la organización activa que el usuario puede asignar (no más acciones que las propias).</summary>
    [HttpGet("perfiles-asignables")]
    public async Task<ActionResult<IEnumerable<OpcionCatalogoDto>>> PerfilesAsignables(CancellationToken ct)
    {
        // Se permite con Añadir o Editar (sin exigir Consultar): el formulario necesita la lista de perfiles.
        if (!HayOrganizacionActiva) return Prohibido("No hay una organización activa.");
        var propios = await AccesosPropiosAsync(ct);
        if (!PuedeGestionar(propios)) return Prohibido("Necesitas permiso para añadir o editar usuarios.");
        var roles = await Db.Roles.Where(x => x.Activo).OrderBy(x => x.Nombre).ToListAsync(ct);
        var asignables = await FiltrarAsignablesAsync(roles, propios, ct);
        return Ok(asignables.Select(x => new OpcionCatalogoDto { Id = x.IdRol, Nombre = x.Nombre }));
    }

    [HttpPost]
    [RequiereModulo(CodigosModulo.Usuarios, AccionModulo.Anadir)]
    public async Task<ActionResult<UsuarioAdministracionDto>> Crear([FromBody] UsuarioAdministracionEntrada request, CancellationToken ct)
    {
        var orgId = OrganizacionActiva;
        var normalizado = Normalizar(request.Correo);
        if (await Db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.CorreoNormalizado == normalizado, ct))
            return Conflicto("El correo ya está registrado.");
        var validacion = await ValidarAsync(request.IdPersona, request.IdRoles, [], ct);
        if (validacion is not null) return validacion;

        var usuario = new Usuario { Correo = request.Correo.Trim(), CorreoNormalizado = normalizado, CreadoEn = DateTime.UtcNow, Activo = true, SelloSeguridad = Guid.NewGuid().ToString("N") };
        usuario.PasswordHash = passwordHasher.HashPassword(usuario, request.Contrasena);
        await using var transaccion = await Db.Database.BeginTransactionAsync(ct);
        Db.Usuarios.Add(usuario);
        if (!await GuardarAsync(ct)) return Conflicto("El correo ya está registrado.");
        var membresia = new MembresiaOrganizacion { IdOrganizacion = orgId, IdUsuario = usuario.IdUsuario, IdPersona = request.IdPersona, Estado = request.Activo ? "Activa" : "Inactiva", CreadoEn = DateTime.UtcNow };
        Db.MembresiasOrganizacion.Add(membresia);
        await Db.SaveChangesAsync(ct);
        foreach (var idRol in request.IdRoles.Distinct()) Db.MembresiasRoles.Add(new MembresiaRol { IdOrganizacion = orgId, IdMembresia = membresia.IdMembresia, IdRol = idRol });
        Auditar(orgId, "Crear", "Usuario", usuario.IdUsuario.ToString(), null, new { usuario.Correo, membresia.Estado, roles = request.IdRoles });
        await Db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        Accesos.InvalidarOrganizacion(orgId);

        var creado = await Consulta(orgId).SingleAsync(x => x.Membresia.IdMembresia == membresia.IdMembresia, ct);
        return CreatedAtAction(nameof(Obtener), new { idMembresia = membresia.IdMembresia }, (await MapearAsync([creado], ct))[0]);
    }

    [HttpPut("{idMembresia:int}")]
    [RequiereModulo(CodigosModulo.Usuarios, AccionModulo.Editar)]
    public async Task<ActionResult<UsuarioAdministracionDto>> Editar(int idMembresia, [FromBody] UsuarioAdministracionEdicion request, CancellationToken ct)
    {
        var orgId = OrganizacionActiva;
        var membresia = await Db.MembresiasOrganizacion.SingleOrDefaultAsync(x => x.IdMembresia == idMembresia, ct);
        if (membresia is null) return NoEncontrado("El usuario no existe en la organización activa.");
        var usuario = await Db.Usuarios.SingleAsync(x => x.IdUsuario == membresia.IdUsuario, ct);
        var esPropio = membresia.IdUsuario == User.IdUsuario();
        if (esPropio && !request.Activo) return Invalido("No puedes inactivar tu propio usuario.");

        var normalizado = Normalizar(request.Correo);
        if (normalizado != usuario.CorreoNormalizado)
        {
            // El correo es global: cambiarlo afectaría las demás organizaciones del usuario.
            if (!EsPlataforma && await Db.MembresiasOrganizacion.IgnoreQueryFilters().AnyAsync(x => x.IdUsuario == usuario.IdUsuario && x.IdOrganizacion != orgId, ct))
                return Conflicto("El usuario pertenece a otras organizaciones; solo el administrador de plataforma puede cambiar su correo.");
            if (await Db.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.IdUsuario != usuario.IdUsuario && x.CorreoNormalizado == normalizado, ct))
                return Conflicto("El correo ya está registrado.");
        }

        var actuales = await Db.MembresiasRoles.Where(x => x.IdMembresia == idMembresia).ToListAsync(ct);
        var validacion = await ValidarAsync(request.IdPersona, request.IdRoles, actuales.Select(x => x.IdRol).ToList(), ct);
        if (validacion is not null) return validacion;

        var anterior = new { usuario.Correo, membresia.Estado, membresia.IdPersona, roles = actuales.Select(x => x.IdRol).OrderBy(x => x) };
        usuario.Correo = request.Correo.Trim(); usuario.CorreoNormalizado = normalizado;
        membresia.IdPersona = request.IdPersona;
        membresia.Estado = request.Activo ? "Activa" : "Inactiva";
        var solicitados = request.IdRoles.Distinct().ToHashSet();
        Db.MembresiasRoles.RemoveRange(actuales.Where(x => !solicitados.Contains(x.IdRol)));
        foreach (var idRol in solicitados.Except(actuales.Select(x => x.IdRol)))
            Db.MembresiasRoles.Add(new MembresiaRol { IdOrganizacion = orgId, IdMembresia = idMembresia, IdRol = idRol });
        Auditar(orgId, "Modificar", "Usuario", usuario.IdUsuario.ToString(), anterior, new { usuario.Correo, membresia.Estado, membresia.IdPersona, roles = solicitados.OrderBy(x => x) });
        if (!await GuardarAsync(ct)) return Conflicto("El correo ya está registrado.");
        Accesos.InvalidarOrganizacion(orgId);
        return await Obtener(idMembresia, ct);
    }

    /// <summary>Activa o inactiva la membresía. Inactivar cierra sus sesiones en esta organización (validación por petición).</summary>
    [HttpPatch("{idMembresia:int}/estado")]
    [RequiereModulo(CodigosModulo.Usuarios, AccionModulo.Editar)]
    public async Task<ActionResult<UsuarioAdministracionDto>> CambiarEstado(int idMembresia, [FromBody] EstadoAdministracionEntrada request, CancellationToken ct)
    {
        var orgId = OrganizacionActiva;
        var membresia = await Db.MembresiasOrganizacion.SingleOrDefaultAsync(x => x.IdMembresia == idMembresia, ct);
        if (membresia is null) return NoEncontrado("El usuario no existe en la organización activa.");
        var activo = request.Activo!.Value;
        if (!activo && membresia.IdUsuario == User.IdUsuario()) return Invalido("No puedes inactivar tu propio usuario.");
        var anterior = membresia.Estado;
        membresia.Estado = activo ? "Activa" : "Inactiva";
        Auditar(orgId, activo ? "Activar" : "Inactivar", "Usuario", membresia.IdUsuario.ToString(), new { Estado = anterior }, new { membresia.Estado });
        await Db.SaveChangesAsync(ct);
        Accesos.InvalidarOrganizacion(orgId);
        return await Obtener(idMembresia, ct);
    }

    // ---------- Auxiliares ----------

    // Clase con inicializadores (no constructor posicional) para que EF pueda traducir filtros sobre sus miembros.
    private sealed class Fila
    {
        public MembresiaOrganizacion Membresia { get; init; } = null!;
        public Usuario Usuario { get; init; } = null!;
        public Organizacion Organizacion { get; init; } = null!;
        public Persona? Persona { get; init; }
    }

    /// <summary>Membresías de la organización activa (filtro explícito además del filtro global).</summary>
    private IQueryable<Fila> Consulta(int orgId) =>
        from membresia in Db.MembresiasOrganizacion
        where membresia.IdOrganizacion == orgId
        join usuario in Db.Usuarios on membresia.IdUsuario equals usuario.IdUsuario
        join organizacion in Db.Organizaciones on membresia.IdOrganizacion equals organizacion.IdOrganizacion
        join persona in Db.Personas on new { membresia.IdOrganizacion, IdPersona = membresia.IdPersona ?? 0 } equals new { persona.IdOrganizacion, persona.IdPersona } into personas
        from persona in personas.DefaultIfEmpty()
        select new Fila { Membresia = membresia, Usuario = usuario, Organizacion = organizacion, Persona = persona };

    private async Task<List<UsuarioAdministracionDto>> MapearAsync(IReadOnlyList<Fila> filas, CancellationToken ct)
    {
        var ids = filas.Select(x => x.Membresia.IdMembresia).ToArray();
        var roles = await (from link in Db.MembresiasRoles
                           join rol in Db.Roles on link.IdRol equals rol.IdRol
                           where ids.Contains(link.IdMembresia)
                           select new { link.IdMembresia, rol.IdRol, rol.Nombre }).ToListAsync(ct);
        return filas.Select(x =>
        {
            var activo = x.Usuario.Activo && x.Membresia.Estado == "Activa";
            var propios = roles.Where(r => r.IdMembresia == x.Membresia.IdMembresia).OrderBy(r => r.Nombre).ToList();
            return new UsuarioAdministracionDto
            {
                IdUsuario = x.Usuario.IdUsuario, IdMembresia = x.Membresia.IdMembresia, IdOrganizacion = x.Membresia.IdOrganizacion,
                Organizacion = x.Organizacion.Nombre, Correo = x.Usuario.Correo, IdPersona = x.Membresia.IdPersona,
                Nombre = x.Persona is null ? null : $"{x.Persona.Nombres} {x.Persona.Apellidos}", Documento = x.Persona?.NumeroDocumento,
                IdRoles = propios.Select(r => r.IdRol).ToArray(), Perfiles = propios.Select(r => r.Nombre).ToArray(),
                Activo = activo, Estado = activo ? "Activo" : "Inactivo",
            };
        }).ToList();
    }

    private static bool PuedeGestionar(IReadOnlyDictionary<string, AccionesModulo> propios) =>
        propios.TryGetValue(CodigosModulo.Usuarios, out var acciones) && (acciones.Anadir || acciones.Editar);

    /// <summary>Persona de la organización activa y perfiles activos asignables (o ya asignados) sin escalar privilegios.</summary>
    private async Task<ActionResult?> ValidarAsync(int? idPersona, IReadOnlyList<int> idRoles, IReadOnlyList<int> yaAsignados, CancellationToken ct)
    {
        if (idPersona is int persona && !await Db.Personas.AnyAsync(x => x.IdPersona == persona, ct))
            return Invalido("La persona no existe en la organización activa.");

        var solicitados = idRoles.Distinct().ToList();
        var roles = await Db.Roles.Where(x => solicitados.Contains(x.IdRol)).ToListAsync(ct);
        if (roles.Count != solicitados.Count) return Invalido("Uno o más perfiles no existen en la organización activa.");
        if (roles.Any(x => !x.Activo && !yaAsignados.Contains(x.IdRol))) return Invalido("No se puede asignar un perfil inactivo.");

        var nuevos = roles.Where(x => !yaAsignados.Contains(x.IdRol)).ToList();
        var asignables = await FiltrarAsignablesAsync(nuevos, await AccesosPropiosAsync(ct), ct);
        return asignables.Count == nuevos.Count ? null : Prohibido("No puedes asignar un perfil con más permisos de los que tienes.");
    }

    /// <summary>Un perfil es asignable si todas sus acciones por módulo están entre las del usuario actual.</summary>
    private async Task<List<Rol>> FiltrarAsignablesAsync(IReadOnlyList<Rol> roles, IReadOnlyDictionary<string, AccionesModulo> propios, CancellationToken ct)
    {
        if (EsPlataforma || roles.Count == 0) return [.. roles];
        var ids = roles.Select(x => x.IdRol).ToArray();
        var asignaciones = await (from mp in Db.ModulosPermisos
                                  join modulo in Db.AppModulos on mp.IdAppModulo equals modulo.IdAppModulo
                                  where ids.Contains(mp.IdRol) && modulo.Activo
                                  select new { mp.IdRol, modulo.Codigo, mp.PuedeConsultar, mp.PuedeAnadir, mp.PuedeEditar }).ToListAsync(ct);
        return roles.Where(rol => asignaciones.Where(a => a.IdRol == rol.IdRol).All(a =>
            propios.TryGetValue(a.Codigo, out var mias) && !new AccionesModulo(a.PuedeConsultar, a.PuedeAnadir, a.PuedeEditar).ExcedeA(mias))).ToList();
    }
}
