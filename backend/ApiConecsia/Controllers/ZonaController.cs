using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class ZonaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public ZonaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Zona>>> Get()
    {
        var zonas = await _unitOfWork.ZonaRepository.GetAllAsync();

        return Ok(zonas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Zona>> Get(int id)
    {
        try
        {
            var zona = await _unitOfWork.ZonaRepository.GetByIdAsync(id);

            return Ok(zona);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Zona with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Zona>> Post([FromBody] Zona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.ZonaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Zona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.ZonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Zona with id {id} was not found.");
        }

        _unitOfWork.ZonaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Zona entity;

        try
        {
            entity = await _unitOfWork.ZonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Zona with id {id} was not found.");
        }

        _unitOfWork.ZonaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
