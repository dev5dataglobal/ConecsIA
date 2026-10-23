using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Administracion)]
public class MembresiaSedeController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public MembresiaSedeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MembresiaSede>>> Get()
    {
        var membresiaSedes = await _unitOfWork.MembresiaSedeRepository.GetAllAsync();

        return Ok(membresiaSedes);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembresiaSede>> Get(int id)
    {
        try
        {
            var membresiaSede = await _unitOfWork.MembresiaSedeRepository.GetByIdAsync(id);

            return Ok(membresiaSede);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaSede with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembresiaSede>> Post([FromBody] MembresiaSede entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.MembresiaSedeRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] MembresiaSede entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.MembresiaSedeRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaSede with id {id} was not found.");
        }

        _unitOfWork.MembresiaSedeRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        MembresiaSede entity;

        try
        {
            entity = await _unitOfWork.MembresiaSedeRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaSede with id {id} was not found.");
        }

        _unitOfWork.MembresiaSedeRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
