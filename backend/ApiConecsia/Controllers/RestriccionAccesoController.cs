using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class RestriccionAccesoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public RestriccionAccesoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RestriccionAcceso>>> Get()
    {
        var restriccionAccesos = await _unitOfWork.RestriccionAccesoRepository.GetAllAsync();

        return Ok(restriccionAccesos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestriccionAcceso>> Get(int id)
    {
        try
        {
            var restriccionAcceso = await _unitOfWork.RestriccionAccesoRepository.GetByIdAsync(id);

            return Ok(restriccionAcceso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"RestriccionAcceso with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RestriccionAcceso>> Post([FromBody] RestriccionAcceso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.RestriccionAccesoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] RestriccionAcceso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.RestriccionAccesoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"RestriccionAcceso with id {id} was not found.");
        }

        _unitOfWork.RestriccionAccesoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        RestriccionAcceso entity;

        try
        {
            entity = await _unitOfWork.RestriccionAccesoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"RestriccionAcceso with id {id} was not found.");
        }

        _unitOfWork.RestriccionAccesoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
