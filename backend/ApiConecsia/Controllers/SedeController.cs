using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Application.Interfaces;
using ConecsIA.Application.DTOs;
using ApiConecsia.Helpers;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Sedes)]
public class SedeController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ConecsiaDbContext _db;
    private readonly IOrganizationContext _organizationContext;
    public SedeController(IUnitOfWork unitOfWork, ConecsiaDbContext db, IOrganizationContext organizationContext)
    {
        _unitOfWork = unitOfWork;
        _db = db; _organizationContext = organizationContext;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SedeDto>>> Get()
    {
        if (!User.HasClaim("permission", AdministracionAuthorization.SedesConsultar) && !User.HasClaim("permission", AdministracionAuthorization.SedesAdministrar)) return Forbid();
        var sedes = await _db.Sedes.Select(x => new SedeDto { IdSede = x.IdSede, IdCiudad = x.IdCiudad, IdZonaEmpresarial = x.IdZonaEmpresarial, Nombre = x.Nombre, Direccion = x.Direccion, Telefono = x.Telefono, Correo = x.Correo, ZonaHoraria = x.ZonaHoraria, IdMembresiaResponsable = x.IdMembresiaResponsable, Activo = x.Activo }).ToListAsync();
        return Ok(sedes);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Sede>> Get(int id)
    {
        try
        {
            var sede = await _unitOfWork.SedeRepository.GetByIdAsync(id);

            return Ok(sede);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Sede with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Sede>> Post([FromBody] Sede entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        if (!User.HasClaim("permission", AdministracionAuthorization.SedesAdministrar) || _organizationContext.OrganizationId is not int orgId) return Forbid();
        entity.IdOrganizacion = orgId;
        if (entity.IdZonaEmpresarial is int zoneId && !await _db.ZonasEmpresariales.AnyAsync(x => x.IdZonaEmpresarial == zoneId && x.Activa)) return BadRequest("La zona empresarial no existe o está inactiva.");
        if (entity.IdZonaEmpresarial is int assignedZone && !await _db.ZonasEmpresarialesOrganizaciones.AnyAsync(x => x.IdOrganizacion == orgId && x.IdZonaEmpresarial == assignedZone && x.Activa && x.EsAdministradora)) return Forbid();
        _unitOfWork.SedeRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Sede entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.SedeRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Sede with id {id} was not found.");
        }

        if (!User.HasClaim("permission", AdministracionAuthorization.SedesAdministrar)) return Forbid();
        if (entity.IdZonaEmpresarial is int zoneId && !await _db.ZonasEmpresariales.AnyAsync(x => x.IdZonaEmpresarial == zoneId && x.Activa)) return BadRequest("La zona empresarial no existe o está inactiva.");
        if (entity.IdZonaEmpresarial is int assignedZone && _organizationContext.OrganizationId is int orgId && !await _db.ZonasEmpresarialesOrganizaciones.AnyAsync(x => x.IdOrganizacion == orgId && x.IdZonaEmpresarial == assignedZone && x.Activa && x.EsAdministradora)) return Forbid();
        entity.IdOrganizacion = _organizationContext.OrganizationId ?? entity.IdOrganizacion;
        _unitOfWork.SedeRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Sede entity;

        try
        {
            entity = await _unitOfWork.SedeRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Sede with id {id} was not found.");
        }

        _unitOfWork.SedeRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
