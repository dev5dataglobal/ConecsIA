using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Administracion)]
public class PlantillaRolController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PlantillaRolController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlantillaRol>>> Get()
    {
        var plantillaRols = await _unitOfWork.PlantillaRolRepository.GetAllAsync();

        return Ok(plantillaRols);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantillaRol>> Get(int id)
    {
        try
        {
            var plantillaRol = await _unitOfWork.PlantillaRolRepository.GetByIdAsync(id);

            return Ok(plantillaRol);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaRol with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlantillaRol>> Post([FromBody] PlantillaRol entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PlantillaRolRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] PlantillaRol entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PlantillaRolRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaRol with id {id} was not found.");
        }

        _unitOfWork.PlantillaRolRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        PlantillaRol entity;

        try
        {
            entity = await _unitOfWork.PlantillaRolRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaRol with id {id} was not found.");
        }

        _unitOfWork.PlantillaRolRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
