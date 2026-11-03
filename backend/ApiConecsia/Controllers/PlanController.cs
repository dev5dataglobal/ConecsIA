using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Configuracion)]
public class PlanController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PlanController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Plan>>> Get()
    {
        var plans = await _unitOfWork.PlanRepository.GetAllAsync();

        return Ok(plans);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Plan>> Get(int id)
    {
        try
        {
            var plan = await _unitOfWork.PlanRepository.GetByIdAsync(id);

            return Ok(plan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Plan with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Plan>> Post([FromBody] Plan entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PlanRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Plan entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PlanRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Plan with id {id} was not found.");
        }

        _unitOfWork.PlanRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Plan entity;

        try
        {
            entity = await _unitOfWork.PlanRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Plan with id {id} was not found.");
        }

        _unitOfWork.PlanRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
