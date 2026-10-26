using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class MembresiaRolController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public MembresiaRolController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MembresiaRol>>> Get()
    {
        var membresiaRols = await _unitOfWork.MembresiaRolRepository.GetAllAsync();

        return Ok(membresiaRols);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembresiaRol>> Get(int id)
    {
        try
        {
            var membresiaRol = await _unitOfWork.MembresiaRolRepository.GetByIdAsync(id);

            return Ok(membresiaRol);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaRol with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembresiaRol>> Post([FromBody] MembresiaRol entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.MembresiaRolRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] MembresiaRol entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.MembresiaRolRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaRol with id {id} was not found.");
        }

        _unitOfWork.MembresiaRolRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        MembresiaRol entity;

        try
        {
            entity = await _unitOfWork.MembresiaRolRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaRol with id {id} was not found.");
        }

        _unitOfWork.MembresiaRolRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
