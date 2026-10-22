using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class PersonaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PersonaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Persona>>> Get()
    {
        var personas = await _unitOfWork.PersonaRepository.GetAllAsync();

        return Ok(personas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Persona>> Get(int id)
    {
        try
        {
            var persona = await _unitOfWork.PersonaRepository.GetByIdAsync(id);

            return Ok(persona);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Persona with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Persona>> Post([FromBody] Persona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PersonaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Persona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PersonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Persona with id {id} was not found.");
        }

        _unitOfWork.PersonaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Persona entity;

        try
        {
            entity = await _unitOfWork.PersonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Persona with id {id} was not found.");
        }

        _unitOfWork.PersonaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
