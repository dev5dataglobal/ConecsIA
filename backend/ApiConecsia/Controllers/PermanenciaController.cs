using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class PermanenciaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PermanenciaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Permanencia>>> Get()
    {
        var permanencias = await _unitOfWork.PermanenciaRepository.GetAllAsync();

        return Ok(permanencias);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Permanencia>> Get(int id)
    {
        try
        {
            var permanencia = await _unitOfWork.PermanenciaRepository.GetByIdAsync(id);

            return Ok(permanencia);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Permanencia with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Permanencia>> Post([FromBody] Permanencia entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PermanenciaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Permanencia entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PermanenciaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Permanencia with id {id} was not found.");
        }

        _unitOfWork.PermanenciaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Permanencia entity;

        try
        {
            entity = await _unitOfWork.PermanenciaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Permanencia with id {id} was not found.");
        }

        _unitOfWork.PermanenciaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
