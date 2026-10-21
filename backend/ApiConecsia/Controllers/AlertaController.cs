using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class AlertaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AlertaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Alerta>>> Get()
    {
        var alertas = await _unitOfWork.AlertaRepository.GetAllAsync();

        return Ok(alertas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Alerta>> Get(int id)
    {
        try
        {
            var alerta = await _unitOfWork.AlertaRepository.GetByIdAsync(id);

            return Ok(alerta);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Alerta with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Alerta>> Post([FromBody] Alerta entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AlertaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Alerta entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AlertaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Alerta with id {id} was not found.");
        }

        _unitOfWork.AlertaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Alerta entity;

        try
        {
            entity = await _unitOfWork.AlertaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Alerta with id {id} was not found.");
        }

        _unitOfWork.AlertaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
