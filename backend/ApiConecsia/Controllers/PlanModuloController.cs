using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Configuracion)]
public class PlanModuloController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PlanModuloController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PlanModulo>>> Get()
    {
        var planModulos = await _unitOfWork.PlanModuloRepository.GetAllAsync();

        return Ok(planModulos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanModulo>> Get(int id)
    {
        try
        {
            var planModulo = await _unitOfWork.PlanModuloRepository.GetByIdAsync(id);

            return Ok(planModulo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlanModulo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanModulo>> Post([FromBody] PlanModulo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PlanModuloRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] PlanModulo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PlanModuloRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlanModulo with id {id} was not found.");
        }

        _unitOfWork.PlanModuloRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        PlanModulo entity;

        try
        {
            entity = await _unitOfWork.PlanModuloRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PlanModulo with id {id} was not found.");
        }

        _unitOfWork.PlanModuloRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
