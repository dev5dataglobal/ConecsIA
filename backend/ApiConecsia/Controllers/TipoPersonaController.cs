using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class TipoPersonaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public TipoPersonaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TipoPersona>>> Get()
    {
        var tipoPersonas = await _unitOfWork.TipoPersonaRepository.GetAllAsync();

        return Ok(tipoPersonas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoPersona>> Get(int id)
    {
        try
        {
            var tipoPersona = await _unitOfWork.TipoPersonaRepository.GetByIdAsync(id);

            return Ok(tipoPersona);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoPersona with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoPersona>> Post([FromBody] TipoPersona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.TipoPersonaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] TipoPersona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.TipoPersonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoPersona with id {id} was not found.");
        }

        _unitOfWork.TipoPersonaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        TipoPersona entity;

        try
        {
            entity = await _unitOfWork.TipoPersonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoPersona with id {id} was not found.");
        }

        _unitOfWork.TipoPersonaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
