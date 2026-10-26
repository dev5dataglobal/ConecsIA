using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class AlertaTransicionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AlertaTransicionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlertaTransicion>>> Get()
    {
        var alertaTransicions = await _unitOfWork.AlertaTransicionRepository.GetAllAsync();

        return Ok(alertaTransicions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertaTransicion>> Get(int id)
    {
        try
        {
            var alertaTransicion = await _unitOfWork.AlertaTransicionRepository.GetByIdAsync(id);

            return Ok(alertaTransicion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AlertaTransicion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlertaTransicion>> Post([FromBody] AlertaTransicion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AlertaTransicionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] AlertaTransicion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AlertaTransicionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AlertaTransicion with id {id} was not found.");
        }

        _unitOfWork.AlertaTransicionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        AlertaTransicion entity;

        try
        {
            entity = await _unitOfWork.AlertaTransicionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AlertaTransicion with id {id} was not found.");
        }

        _unitOfWork.AlertaTransicionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
