using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class DispositivoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public DispositivoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Dispositivo>>> Get()
    {
        var dispositivos = await _unitOfWork.DispositivoRepository.GetAllAsync();

        return Ok(dispositivos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Dispositivo>> Get(int id)
    {
        try
        {
            var dispositivo = await _unitOfWork.DispositivoRepository.GetByIdAsync(id);

            return Ok(dispositivo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Dispositivo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Dispositivo>> Post([FromBody] Dispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.DispositivoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Dispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.DispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Dispositivo with id {id} was not found.");
        }

        _unitOfWork.DispositivoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Dispositivo entity;

        try
        {
            entity = await _unitOfWork.DispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Dispositivo with id {id} was not found.");
        }

        _unitOfWork.DispositivoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
