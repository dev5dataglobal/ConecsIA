using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Perfiles)]
public class PlantillaRolPermisoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PlantillaRolPermisoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlantillaRolPermiso>>> Get()
    {
        var plantillaRolPermisos = await _unitOfWork.PlantillaRolPermisoRepository.GetAllAsync();

        return Ok(plantillaRolPermisos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantillaRolPermiso>> Get(int id)
    {
        try
        {
            var plantillaRolPermiso = await _unitOfWork.PlantillaRolPermisoRepository.GetByIdAsync(id);

            return Ok(plantillaRolPermiso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaRolPermiso with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlantillaRolPermiso>> Post([FromBody] PlantillaRolPermiso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PlantillaRolPermisoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] PlantillaRolPermiso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PlantillaRolPermisoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaRolPermiso with id {id} was not found.");
        }

        _unitOfWork.PlantillaRolPermisoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        PlantillaRolPermiso entity;

        try
        {
            entity = await _unitOfWork.PlantillaRolPermisoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaRolPermiso with id {id} was not found.");
        }

        _unitOfWork.PlantillaRolPermisoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
