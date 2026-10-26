using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class SedeController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public SedeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Sede>>> Get()
    {
        var sedes = await _unitOfWork.SedeRepository.GetAllAsync();

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
