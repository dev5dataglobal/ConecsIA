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
/// Responsables y autorizadores designados por destino (RF-04, HU-014). Con designados activos, solo ellos pueden ser anfitriones
/// de los registros de acceso de ese destino (D-36); sin designados, cualquier usuario activo. La marca de autorizador queda
/// registrada pero <b>no otorga por sí permiso de decidir</b>: eso sigue en Autorizaciones·Editar o en el flujo de la categoría (D-04, D-56).
/// Retirar una designación la inactiva (no se borra) y no cambia registros ni visitas existentes. Todo cambio se audita.
/// Permisos: consultar = Sedes·Consultar; asignar, cambiar y retirar = Sedes·Editar; alcance por sede de la membresía.
/// </summary>
[Route("api/areas")]
[Produces("application/json")]
public sealed class AreaResponsablesController(
    ConecsiaDbContext db,
    IOrganizationContext organizationContext,
    IModuloAccesoService accesos) : AdministracionControllerBase(db, organizationContext, accesos)
{
    private const string Entidad = "AreaResponsable";

    /// <summary>Responsables del destino, incluidos los retirados (para conservar la trazabilidad).</summary>
    [HttpGet("{idArea:int}/responsables")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<AreaResponsableDto>>> Listar(int idArea, CancellationToken ct)
    {
        if (await AreaEnAlcanceAsync(idArea, ct) is null) return NoEncontrado("El destino no existe o no está en tu alcance.");
        return Ok(await ProyectarAsync(idArea, null, ct));
    }

    /// <summary>Usuarios activos de la organización que pueden designarse.</summary>
    [HttpGet("miembros")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public async Task<ActionResult<IEnumerable<MiembroOrganizacionDto>>> Miembros(CancellationToken ct) =>
        Ok(await (from m in Db.MembresiasOrganizacion.AsNoTracking()
                  join u in Db.Usuarios on m.IdUsuario equals u.IdUsuario
                  where (m.Estado == "Activa" || m.Estado == "Aceptada") && u.Activo
                  orderby u.Correo
                  select new MiembroOrganizacionDto { IdMembresia = m.IdMembresia, Usuario = u.Correo }).ToListAsync(ct));

    /// <summary>Designa un responsable (o reactiva uno retirado) y lo marca opcionalmente como autorizador.</summary>
    /// <response code="409">Ya está designado, el destino está inactivo u otro usuario lo designó al mismo tiempo.</response>
    [HttpPost("{idArea:int}/responsables")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<ActionResult<AreaResponsableDto>> Asignar(int idArea, [FromBody] AsignarResponsableAreaEntrada request, CancellationToken ct)
    {
        var area = await AreaEnAlcanceAsync(idArea, ct);
        if (area is null) return NoEncontrado("El destino no existe o no está en tu alcance.");
        if (!area.Activo) return Conflicto("El destino está inactivo: actívalo antes de designar responsables.");
        if (!await MembresiaActivaAsync(request.IdMembresia, ct)) return Invalido("El usuario no está activo en tu organización.");

        var ahora = DateTime.UtcNow;
        var idUsuario = User.IdUsuario()!.Value;
        var existente = await Db.AreasResponsables.SingleOrDefaultAsync(x => x.IdArea == idArea && x.IdMembresia == request.IdMembresia, ct);
        if (existente is { Activo: true }) return Conflicto("El usuario ya está designado en este destino.");
        if (existente is not null)
        {
            var anterior = Resumen(existente);
            existente.Activo = true;
            existente.EsAutorizador = request.EsAutorizador;
            existente.ActualizadoEn = ahora;
            existente.IdUsuarioActualiza = idUsuario;
            Auditar(OrganizacionActiva, "ReactivarResponsable", Entidad, Clave(existente), anterior, Resumen(existente));
        }
        else
        {
            var nuevo = new AreaResponsable
            {
                IdOrganizacion = OrganizacionActiva, IdArea = idArea, IdMembresia = request.IdMembresia, EsAutorizador = request.EsAutorizador,
                Activo = true, AsignadoEn = ahora, IdUsuarioAsigna = idUsuario,
            };
            Db.AreasResponsables.Add(nuevo);
            Auditar(OrganizacionActiva, "AsignarResponsable", Entidad, Clave(nuevo), null, Resumen(nuevo));
        }
        if (!await GuardarAsync(ct)) return Conflicto("Otro usuario designó a este responsable al mismo tiempo. Consulta la lista actual.");
        return Ok((await ProyectarAsync(idArea, request.IdMembresia, ct)).Single());
    }

    /// <summary>Cambia la marca de autorizador de un responsable activo.</summary>
    [HttpPut("{idArea:int}/responsables/{idMembresia:int}")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<ActionResult<AreaResponsableDto>> Cambiar(int idArea, int idMembresia, [FromBody] AsignarResponsableAreaEntrada request, CancellationToken ct)
    {
        if (await AreaEnAlcanceAsync(idArea, ct) is null) return NoEncontrado("El destino no existe o no está en tu alcance.");
        var responsable = await Db.AreasResponsables.SingleOrDefaultAsync(x => x.IdArea == idArea && x.IdMembresia == idMembresia, ct);
        if (responsable is null) return NoEncontrado("El usuario no está designado en este destino.");
        if (!responsable.Activo) return Conflicto("La designación está retirada: vuelve a designarlo.");
        if (responsable.EsAutorizador == request.EsAutorizador) return Ok((await ProyectarAsync(idArea, idMembresia, ct)).Single());
        var anterior = Resumen(responsable);
        responsable.EsAutorizador = request.EsAutorizador;
        responsable.ActualizadoEn = DateTime.UtcNow;
        responsable.IdUsuarioActualiza = User.IdUsuario()!.Value;
        Auditar(OrganizacionActiva, "CambiarResponsable", Entidad, Clave(responsable), anterior, Resumen(responsable));
        await Db.SaveChangesAsync(ct);
        return Ok((await ProyectarAsync(idArea, idMembresia, ct)).Single());
    }

    /// <summary>Retira la designación (la inactiva; el historial se conserva). No modifica registros ni visitas existentes.</summary>
    [HttpPost("{idArea:int}/responsables/{idMembresia:int}/retirar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public async Task<ActionResult<AreaResponsableDto>> Retirar(int idArea, int idMembresia, CancellationToken ct)
    {
        if (await AreaEnAlcanceAsync(idArea, ct) is null) return NoEncontrado("El destino no existe o no está en tu alcance.");
        var responsable = await Db.AreasResponsables.SingleOrDefaultAsync(x => x.IdArea == idArea && x.IdMembresia == idMembresia, ct);
        if (responsable is null) return NoEncontrado("El usuario no está designado en este destino.");
        if (!responsable.Activo) return Conflicto("La designación ya está retirada.");
        var anterior = Resumen(responsable);
        responsable.Activo = false;
        responsable.ActualizadoEn = DateTime.UtcNow;
        responsable.IdUsuarioActualiza = User.IdUsuario()!.Value;
        Auditar(OrganizacionActiva, "RetirarResponsable", Entidad, Clave(responsable), anterior, Resumen(responsable));
        await Db.SaveChangesAsync(ct);
        return Ok((await ProyectarAsync(idArea, idMembresia, ct)).Single());
    }

    private async Task<Area?> AreaEnAlcanceAsync(int idArea, CancellationToken ct)
    {
        var area = await Db.Areas.AsNoTracking().SingleOrDefaultAsync(x => x.IdArea == idArea, ct);
        return area is not null && EnAlcance(await SedesAlcanceAsync(ct), area.IdSede) ? area : null;
    }

    private Task<bool> MembresiaActivaAsync(int idMembresia, CancellationToken ct) =>
        Db.MembresiasOrganizacion.AnyAsync(x => x.IdMembresia == idMembresia && (x.Estado == "Activa" || x.Estado == "Aceptada")
            && Db.Usuarios.Any(u => u.IdUsuario == x.IdUsuario && u.Activo), ct);

    private Task<List<AreaResponsableDto>> ProyectarAsync(int idArea, int? idMembresia, CancellationToken ct) =>
        (from r in Db.AreasResponsables.AsNoTracking()
         join m in Db.MembresiasOrganizacion on r.IdMembresia equals m.IdMembresia
         join u in Db.Usuarios on m.IdUsuario equals u.IdUsuario
         where r.IdArea == idArea && (idMembresia == null || r.IdMembresia == idMembresia)
         orderby r.Activo descending, u.Correo
         select new AreaResponsableDto
         {
             IdArea = r.IdArea, IdMembresia = r.IdMembresia, Usuario = u.Correo, UsuarioActivo = u.Activo && (m.Estado == "Activa" || m.Estado == "Aceptada"),
             EsAutorizador = r.EsAutorizador, Activo = r.Activo, AsignadoEn = r.AsignadoEn, ActualizadoEn = r.ActualizadoEn,
             AsignadoPor = Db.Usuarios.Where(x => x.IdUsuario == r.IdUsuarioAsigna).Select(x => x.Correo).FirstOrDefault(),
             ActualizadoPor = Db.Usuarios.Where(x => x.IdUsuario == r.IdUsuarioActualiza).Select(x => x.Correo).FirstOrDefault(),
         }).ToListAsync(ct);

    private static string Clave(AreaResponsable x) => $"{x.IdArea}:{x.IdMembresia}";

    private static object Resumen(AreaResponsable x) => new { x.IdArea, x.IdMembresia, x.EsAutorizador, x.Activo };
}
