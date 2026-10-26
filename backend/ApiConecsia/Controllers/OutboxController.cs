using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class OutboxController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public OutboxController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Outbox>>> Get()
    {
        var outboxs = await _unitOfWork.OutboxRepository.GetAllAsync();

        return Ok(outboxs);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Outbox>> Get(int id)
    {
        try
        {
            var outbox = await _unitOfWork.OutboxRepository.GetByIdAsync(id);

            return Ok(outbox);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Outbox with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Outbox>> Post([FromBody] Outbox entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.OutboxRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Outbox entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.OutboxRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Outbox with id {id} was not found.");
        }

        _unitOfWork.OutboxRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Outbox entity;

        try
        {
            entity = await _unitOfWork.OutboxRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Outbox with id {id} was not found.");
        }

        _unitOfWork.OutboxRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
