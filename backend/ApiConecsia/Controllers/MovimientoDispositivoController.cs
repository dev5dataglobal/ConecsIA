using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class MovimientoDispositivoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public MovimientoDispositivoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MovimientoDispositivo>>> Get()
    {
        var movimientoDispositivos = await _unitOfWork.MovimientoDispositivoRepository.GetAllAsync();

        return Ok(movimientoDispositivos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoDispositivo>> Get(int id)
    {
        try
        {
            var movimientoDispositivo = await _unitOfWork.MovimientoDispositivoRepository.GetByIdAsync(id);

            return Ok(movimientoDispositivo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoDispositivo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoDispositivo>> Post([FromBody] MovimientoDispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.MovimientoDispositivoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] MovimientoDispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.MovimientoDispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoDispositivo with id {id} was not found.");
        }

        _unitOfWork.MovimientoDispositivoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        MovimientoDispositivo entity;

        try
        {
            entity = await _unitOfWork.MovimientoDispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoDispositivo with id {id} was not found.");
        }

        _unitOfWork.MovimientoDispositivoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
