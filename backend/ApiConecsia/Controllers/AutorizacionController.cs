using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ApiConecsia.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Autorizaciones)]
public class AutorizacionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ConecsiaDbContext _db;
    public AutorizacionController(IUnitOfWork unitOfWork, ConecsiaDbContext db)
    {
        _unitOfWork = unitOfWork;
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Autorizacion>>> Get()
    {
        var autorizacions = await _unitOfWork.AutorizacionRepository.GetAllAsync();

        return Ok(autorizacions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Autorizacion>> Get(int id)
    {
        try
        {
            var autorizacion = await _unitOfWork.AutorizacionRepository.GetByIdAsync(id);

            return Ok(autorizacion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Autorizacion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] Autorizacion entity)
    {
        if (!User.HasClaim("permission", AdministracionAuthorization.AutorizacionesAdministrar)) return Forbid();
        if (entity == null)
        {
            return BadRequest();
        }

        var validation = await ValidateScope(entity);
        if (validation is not null) return validation;

        _unitOfWork.AutorizacionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Autorizacion entity)
    {
        if (!User.HasClaim("permission", AdministracionAuthorization.AutorizacionesAdministrar)) return Forbid();
        if (entity == null)
        {
            return BadRequest();
        }

        var validation = await ValidateScope(entity);
        if (validation is not null) return validation;

        try
        {
            await _unitOfWork.AutorizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Autorizacion with id {id} was not found.");
        }

        _unitOfWork.AutorizacionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Autorizacion entity;

        try
        {
            entity = await _unitOfWork.AutorizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Autorizacion with id {id} was not found.");
        }

        _unitOfWork.AutorizacionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }

    private async Task<IActionResult?> ValidateScope(Autorizacion entity)
    {
        if (entity.TipoEmisor is not ("Empresa" or "ZonaEmpresarial") || entity.Alcance is not ("Privado" or "Comun"))
            return BadRequest("El emisor o alcance de la autorización no es válido.");
        var sede = await _db.Sedes.SingleOrDefaultAsync(x => x.IdOrganizacion == entity.IdOrganizacion && x.IdSede == entity.IdSede);
        var area = await _db.Areas.SingleOrDefaultAsync(x => x.IdOrganizacion == entity.IdOrganizacion && x.IdArea == entity.IdArea && x.IdSede == entity.IdSede);
        if (sede is null || area is null) return BadRequest("La sede y el área deben pertenecer a la organización y estar relacionadas.");
        if (entity.TipoEmisor == "Empresa" && (entity.IdZonaEmpresarialEmisora is not null || entity.Alcance != "Privado" || area.EsComunZonaEmpresarial))
            return BadRequest("Una autorización de empresa solo puede habilitar áreas privadas de su organización.");
        if (entity.TipoEmisor == "ZonaEmpresarial" && (entity.IdZonaEmpresarialEmisora is null || entity.Alcance != "Comun" || !area.EsComunZonaEmpresarial || sede.IdZonaEmpresarial != entity.IdZonaEmpresarialEmisora))
            return BadRequest("Una autorización de zona solo puede habilitar áreas comunes de su propia zona empresarial.");
        if (entity.TipoEmisor == "ZonaEmpresarial" && !await _db.ZonasEmpresariales.AnyAsync(x => x.IdZonaEmpresarial == entity.IdZonaEmpresarialEmisora && x.Activa))
            return BadRequest("La zona empresarial no existe o está inactiva.");
        if (entity.TipoEmisor == "ZonaEmpresarial" && !await _db.ZonasEmpresarialesOrganizaciones.AnyAsync(x => x.IdOrganizacion == entity.IdOrganizacion && x.IdZonaEmpresarial == entity.IdZonaEmpresarialEmisora && x.EsAdministradora && x.Activa))
            return Forbid();
        return null;
    }
}
