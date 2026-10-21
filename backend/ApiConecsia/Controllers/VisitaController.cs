using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class VisitaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public VisitaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Visita>>> Get()
    {
        var visitas = await _unitOfWork.VisitaRepository.GetAllAsync();

        return Ok(visitas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Visita>> Get(int id)
    {
        try
        {
            var visita = await _unitOfWork.VisitaRepository.GetByIdAsync(id);

            return Ok(visita);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Visita with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Visita>> Post([FromBody] Visita entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.VisitaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Visita entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.VisitaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Visita with id {id} was not found.");
        }

        _unitOfWork.VisitaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Visita entity;

        try
        {
            entity = await _unitOfWork.VisitaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Visita with id {id} was not found.");
        }

        _unitOfWork.VisitaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
