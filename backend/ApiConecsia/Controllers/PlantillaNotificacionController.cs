using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Configuracion)]
public class PlantillaNotificacionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PlantillaNotificacionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlantillaNotificacion>>> Get()
    {
        var plantillaNotificacions = await _unitOfWork.PlantillaNotificacionRepository.GetAllAsync();

        return Ok(plantillaNotificacions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantillaNotificacion>> Get(int id)
    {
        try
        {
            var plantillaNotificacion = await _unitOfWork.PlantillaNotificacionRepository.GetByIdAsync(id);

            return Ok(plantillaNotificacion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaNotificacion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlantillaNotificacion>> Post([FromBody] PlantillaNotificacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PlantillaNotificacionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] PlantillaNotificacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PlantillaNotificacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaNotificacion with id {id} was not found.");
        }

        _unitOfWork.PlantillaNotificacionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        PlantillaNotificacion entity;

        try
        {
            entity = await _unitOfWork.PlantillaNotificacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlantillaNotificacion with id {id} was not found.");
        }

        _unitOfWork.PlantillaNotificacionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
