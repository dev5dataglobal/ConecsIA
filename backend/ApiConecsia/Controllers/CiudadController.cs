using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class CiudadController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public CiudadController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Ciudad>>> Get()
    {
        var ciudads = await _unitOfWork.CiudadRepository.GetAllAsync();

        return Ok(ciudads);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Ciudad>> Get(int id)
    {
        try
        {
            var ciudad = await _unitOfWork.CiudadRepository.GetByIdAsync(id);

            return Ok(ciudad);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Ciudad with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Ciudad>> Post([FromBody] Ciudad entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.CiudadRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Ciudad entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.CiudadRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Ciudad with id {id} was not found.");
        }

        _unitOfWork.CiudadRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Ciudad entity;

        try
        {
            entity = await _unitOfWork.CiudadRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Ciudad with id {id} was not found.");
        }

        _unitOfWork.CiudadRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
