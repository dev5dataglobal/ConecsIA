using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class AutorizacionDispositivoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AutorizacionDispositivoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AutorizacionDispositivo>>> Get()
    {
        var autorizacionDispositivos = await _unitOfWork.AutorizacionDispositivoRepository.GetAllAsync();

        return Ok(autorizacionDispositivos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutorizacionDispositivo>> Get(int id)
    {
        try
        {
            var autorizacionDispositivo = await _unitOfWork.AutorizacionDispositivoRepository.GetByIdAsync(id);

            return Ok(autorizacionDispositivo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionDispositivo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutorizacionDispositivo>> Post([FromBody] AutorizacionDispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AutorizacionDispositivoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] AutorizacionDispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AutorizacionDispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionDispositivo with id {id} was not found.");
        }

        _unitOfWork.AutorizacionDispositivoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        AutorizacionDispositivo entity;

        try
        {
            entity = await _unitOfWork.AutorizacionDispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionDispositivo with id {id} was not found.");
        }

        _unitOfWork.AutorizacionDispositivoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
