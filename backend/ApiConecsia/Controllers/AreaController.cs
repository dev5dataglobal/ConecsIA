using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[RequiereModulo(CodigosModulo.Sedes)]
[Route("api/areas")]
public sealed class AreaController(IUnitOfWork unitOfWork, IOrganizationContext organizationContext) : BaseApiController
{
    [HttpGet]
    public ActionResult<IEnumerable<AreaDto>> Get([FromQuery] int? idSede, [FromQuery] bool? activo = null) => Ok(unitOfWork.AreaRepository.Find(x => (!idSede.HasValue || x.IdSede == idSede) && (!activo.HasValue || x.Activo == activo.Value)).OrderBy(x => x.Nombre).Select(ToDto));

    [HttpGet("{id:int}")]
    public ActionResult<AreaDto> Get(int id) { var area = unitOfWork.AreaRepository.Find(x => x.IdArea == id).SingleOrDefault(); return area is null ? NotFound() : Ok(ToDto(area)); }

    [HttpPost]
    public async Task<ActionResult<AreaDto>> Post([FromBody] AreaDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.IdSede <= 0) return BadRequest("Sede y nombre son obligatorios.");
        var orgId = RequireOrganization(); var sede = unitOfWork.SedeRepository.Find(x => x.IdSede == request.IdSede && x.Activo).SingleOrDefault(); if (sede is null) return BadRequest("La sede no existe, está inactiva o no pertenece a la organización.");
        if (request.EsComunZonaEmpresarial && sede.IdZonaEmpresarial is null) return BadRequest("La sede no pertenece a una zona empresarial.");
        if (unitOfWork.AreaRepository.Find(x => x.IdSede == sede.IdSede && x.Nombre == request.Nombre.Trim()).Any()) return Conflict("Ya existe un área con ese nombre en la sede.");
        var area = new Area { IdOrganizacion = orgId, IdSede = sede.IdSede, IdAreaPadre = request.IdAreaPadre, Nombre = request.Nombre.Trim(), Descripcion = request.Descripcion?.Trim(), EsComunZonaEmpresarial = request.EsComunZonaEmpresarial, Activo = true }; unitOfWork.AreaRepository.Add(area); await unitOfWork.SaveAsync(); await Audit("area.crear", area.IdArea, null, ToDto(area)); return CreatedAtAction(nameof(Get), new { id = area.IdArea }, ToDto(area));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AreaDto>> Put(int id, [FromBody] AreaDto request)
    {
        var area = unitOfWork.AreaRepository.Find(x => x.IdArea == id).SingleOrDefault(); if (area is null) return NotFound(); if (string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest("El nombre es obligatorio.");
        var sede = unitOfWork.SedeRepository.Find(x => x.IdSede == request.IdSede && x.Activo).SingleOrDefault(); if (sede is null) return BadRequest("La sede no existe o no pertenece a la organización."); if (request.EsComunZonaEmpresarial && sede.IdZonaEmpresarial is null) return BadRequest("La sede no pertenece a una zona empresarial.");
        if (unitOfWork.AreaRepository.Find(x => x.IdArea != id && x.IdSede == sede.IdSede && x.Nombre == request.Nombre.Trim()).Any()) return Conflict("Ya existe un área con ese nombre en la sede.");
        var anterior = ToDto(area); area.IdSede = sede.IdSede; area.Nombre = request.Nombre.Trim(); area.Descripcion = request.Descripcion?.Trim(); area.IdAreaPadre = request.IdAreaPadre; area.EsComunZonaEmpresarial = request.EsComunZonaEmpresarial; unitOfWork.AreaRepository.Update(area); await unitOfWork.SaveAsync(); await Audit("area.modificar", id, anterior, ToDto(area)); return Ok(ToDto(area));
    }

    [HttpPost("{id:int}/inactivar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<IActionResult> Inactivar(int id) => CambiarEstado(id, false);

    [HttpPost("{id:int}/activar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<IActionResult> Activar(int id) => CambiarEstado(id, true);

    private async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var area = unitOfWork.AreaRepository.Find(x => x.IdArea == id).SingleOrDefault(); if (area is null) return NotFound();
        if (area.Activo == activo) return UnprocessableEntity(activo ? "El área ya está activa." : "El área ya está inactiva.");
        if (activo && !unitOfWork.SedeRepository.Find(x => x.IdSede == area.IdSede && x.Activo).Any()) return UnprocessableEntity("Activa primero la sede del área.");
        var anterior = ToDto(area); area.Activo = activo; unitOfWork.AreaRepository.Update(area); await unitOfWork.SaveAsync();
        await Audit(activo ? "area.activar" : "area.inactivar", id, anterior, ToDto(area)); return Ok(ToDto(area));
    }

    private int RequireOrganization() => organizationContext.OrganizationId ?? throw new UnauthorizedAccessException("No hay una organización autenticada.");
    private static AreaDto ToDto(Area x) => new() { IdArea = x.IdArea, IdSede = x.IdSede, IdAreaPadre = x.IdAreaPadre, Nombre = x.Nombre, Descripcion = x.Descripcion, EsComunZonaEmpresarial = x.EsComunZonaEmpresarial, Activo = x.Activo };
    private async Task Audit(string action, int id, object? before, object after) { unitOfWork.AuditoriaRepository.Add(new Auditoria { IdOrganizacion = organizationContext.OrganizationId, TipoActor = "Usuario", IdUsuario = organizationContext.UserId, Accion = action, Entidad = "Area", IdRegistro = id.ToString(), Resultado = "Exitoso", ValorAnterior = before is null ? null : System.Text.Json.JsonSerializer.Serialize(before), ValorNuevo = System.Text.Json.JsonSerializer.Serialize(after), OcurridoEn = DateTime.UtcNow }); await unitOfWork.SaveAsync(); }
}
