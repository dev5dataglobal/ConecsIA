using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class AreaResponsableController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AreaResponsableController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AreaResponsable>>> Get()
    {
        var areaResponsables = await _unitOfWork.AreaResponsableRepository.GetAllAsync();

        return Ok(areaResponsables);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AreaResponsable>> Get(int id)
    {
        try
        {
            var areaResponsable = await _unitOfWork.AreaResponsableRepository.GetByIdAsync(id);

            return Ok(areaResponsable);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AreaResponsable with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AreaResponsable>> Post([FromBody] AreaResponsable entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AreaResponsableRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] AreaResponsable entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AreaResponsableRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AreaResponsable with id {id} was not found.");
        }

        _unitOfWork.AreaResponsableRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        AreaResponsable entity;

        try
        {
            entity = await _unitOfWork.AreaResponsableRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AreaResponsable with id {id} was not found.");
        }

        _unitOfWork.AreaResponsableRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
