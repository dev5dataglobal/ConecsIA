using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.ControlAccesos)]
public class MovimientoAccesoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public MovimientoAccesoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MovimientoAcceso>>> Get()
    {
        var movimientoAccesos = await _unitOfWork.MovimientoAccesoRepository.GetAllAsync();

        return Ok(movimientoAccesos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoAcceso>> Get(int id)
    {
        try
        {
            var movimientoAcceso = await _unitOfWork.MovimientoAccesoRepository.GetByIdAsync(id);

            return Ok(movimientoAcceso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoAcceso with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoAcceso>> Post([FromBody] MovimientoAcceso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.MovimientoAccesoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] MovimientoAcceso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.MovimientoAccesoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoAcceso with id {id} was not found.");
        }

        _unitOfWork.MovimientoAccesoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        MovimientoAcceso entity;

        try
        {
            entity = await _unitOfWork.MovimientoAccesoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoAcceso with id {id} was not found.");
        }

        _unitOfWork.MovimientoAccesoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
