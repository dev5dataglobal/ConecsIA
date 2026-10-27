using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.DTOs;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[RequiereModulo(CodigosModulo.Sedes)]
[Route("api/sedes")]
public sealed class SedeController(IUnitOfWork unitOfWork, IOrganizationContext organizationContext) : BaseApiController
{
    [HttpGet]
    public ActionResult<IEnumerable<SedeDto>> Get([FromQuery] bool? activo = null) => Ok(unitOfWork.SedeRepository.Find(x => !activo.HasValue || x.Activo == activo.Value).OrderBy(x => x.Nombre).Select(ToDto));

    [HttpGet("{id:int}")]
    public ActionResult<SedeDto> Get(int id) { var sede = unitOfWork.SedeRepository.Find(x => x.IdSede == id).SingleOrDefault(); return sede is null ? NotFound() : Ok(ToDto(sede)); }

    [HttpPost]
    public async Task<ActionResult<SedeDto>> Post([FromBody] SedeDto request)
    {
        if (!TryValidate(request, out var error)) return BadRequest(error);
        var orgId = RequireOrganization(); var ciudad = unitOfWork.CiudadRepository.Find(x => x.IdCiudad == request.IdCiudad && x.Activo).SingleOrDefault();
        if (ciudad is null) return BadRequest("La ciudad seleccionada no existe o está inactiva.");
        if (request.IdZonaEmpresarial is not null && !unitOfWork.ZonaEmpresarialOrganizacionRepository.Find(x => x.IdOrganizacion == orgId && x.IdZonaEmpresarial == request.IdZonaEmpresarial && x.Activa).Any()) return BadRequest("La zona empresarial no está asociada a la organización.");
        if (unitOfWork.SedeRepository.Find(x => x.Nombre == request.Nombre.Trim()).Any()) return Conflict("Ya existe una sede con ese nombre.");
        var sede = new Sede { IdOrganizacion = orgId, IdCiudad = ciudad.IdCiudad, IdZonaEmpresarial = request.IdZonaEmpresarial, Nombre = request.Nombre.Trim(), Direccion = request.Direccion?.Trim(), Telefono = request.Telefono?.Trim(), Correo = request.Correo?.Trim(), ZonaHoraria = ciudad.ZonaHoraria, Activo = true };
        unitOfWork.SedeRepository.Add(sede); await unitOfWork.SaveAsync(); await Audit("sede.crear", sede.IdSede, null, ToDto(sede)); return CreatedAtAction(nameof(Get), new { id = sede.IdSede }, ToDto(sede));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SedeDto>> Put(int id, [FromBody] SedeDto request)
    {
        if (!TryValidate(request, out var error)) return BadRequest(error);
        var sede = unitOfWork.SedeRepository.Find(x => x.IdSede == id).SingleOrDefault(); if (sede is null) return NotFound();
        var ciudad = unitOfWork.CiudadRepository.Find(x => x.IdCiudad == request.IdCiudad && x.Activo).SingleOrDefault(); if (ciudad is null) return BadRequest("La ciudad seleccionada no existe o está inactiva.");
        if (request.IdZonaEmpresarial is not null && !unitOfWork.ZonaEmpresarialOrganizacionRepository.Find(x => x.IdOrganizacion == organizationContext.OrganizationId && x.IdZonaEmpresarial == request.IdZonaEmpresarial && x.Activa).Any()) return BadRequest("La zona empresarial no está asociada a la organización.");
        if (unitOfWork.SedeRepository.Find(x => x.IdSede != id && x.Nombre == request.Nombre.Trim()).Any()) return Conflict("Ya existe una sede con ese nombre.");
        var anterior = ToDto(sede); sede.IdCiudad = ciudad.IdCiudad; sede.Nombre = request.Nombre.Trim(); sede.Direccion = request.Direccion?.Trim(); sede.Telefono = request.Telefono?.Trim(); sede.Correo = request.Correo?.Trim(); sede.ZonaHoraria = ciudad.ZonaHoraria; sede.IdZonaEmpresarial = request.IdZonaEmpresarial; unitOfWork.SedeRepository.Update(sede); await unitOfWork.SaveAsync(); await Audit("sede.modificar", id, anterior, ToDto(sede)); return Ok(ToDto(sede));
    }

    /// <summary>Catálogo de ciudades activas para el formulario de sedes (no requiere el módulo de configuración).</summary>
    [HttpGet("ciudades")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Consultar)]
    public ActionResult<IEnumerable<CiudadDto>> Ciudades() => Ok(unitOfWork.CiudadRepository.Find(x => x.Activo).OrderBy(x => x.Nombre)
        .Select(x => new CiudadDto { IdCiudad = x.IdCiudad, CodigoDane = x.CodigoDane, Nombre = x.Nombre, Departamento = x.Departamento, Pais = x.Pais, ZonaHoraria = x.ZonaHoraria, Activo = x.Activo }));

    [HttpPost("{id:int}/inactivar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<IActionResult> Inactivar(int id) => CambiarEstado(id, false);

    [HttpPost("{id:int}/activar")]
    [RequiereModulo(CodigosModulo.Sedes, AccionModulo.Editar)]
    public Task<IActionResult> Activar(int id) => CambiarEstado(id, true);

    private async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var sede = unitOfWork.SedeRepository.Find(x => x.IdSede == id).SingleOrDefault(); if (sede is null) return NotFound();
        if (sede.Activo == activo) return UnprocessableEntity(activo ? "La sede ya está activa." : "La sede ya está inactiva.");
        var anterior = ToDto(sede); sede.Activo = activo; unitOfWork.SedeRepository.Update(sede); await unitOfWork.SaveAsync();
        await Audit(activo ? "sede.activar" : "sede.inactivar", id, anterior, ToDto(sede)); return Ok(ToDto(sede));
    }

    private int RequireOrganization() => organizationContext.OrganizationId ?? throw new UnauthorizedAccessException("No hay una organización autenticada.");
    private static bool TryValidate(SedeDto x, out string error) { if (x.IdCiudad <= 0 || string.IsNullOrWhiteSpace(x.Nombre)) { error = "Ciudad y nombre son obligatorios."; return false; } if (!string.IsNullOrWhiteSpace(x.Correo) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(x.Correo)) { error = "El correo no tiene un formato válido."; return false; } error = ""; return true; }
    private static SedeDto ToDto(Sede x) => new() { IdSede = x.IdSede, IdCiudad = x.IdCiudad, IdZonaEmpresarial = x.IdZonaEmpresarial, Nombre = x.Nombre, Direccion = x.Direccion, Telefono = x.Telefono, Correo = x.Correo, ZonaHoraria = x.ZonaHoraria, IdMembresiaResponsable = x.IdMembresiaResponsable, Activo = x.Activo };
    private async Task Audit(string action, int id, object? before, object after) { unitOfWork.AuditoriaRepository.Add(new Auditoria { IdOrganizacion = organizationContext.OrganizationId, TipoActor = "Usuario", IdUsuario = organizationContext.UserId, Accion = action, Entidad = "Sede", IdRegistro = id.ToString(), Resultado = "Exitoso", ValorAnterior = before is null ? null : System.Text.Json.JsonSerializer.Serialize(before), ValorNuevo = System.Text.Json.JsonSerializer.Serialize(after), OcurridoEn = DateTime.UtcNow }); await unitOfWork.SaveAsync(); }
}
