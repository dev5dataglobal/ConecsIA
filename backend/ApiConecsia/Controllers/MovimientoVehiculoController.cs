using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Vehiculos)]
public class MovimientoVehiculoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public MovimientoVehiculoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MovimientoVehiculo>>> Get()
    {
        var movimientoVehiculos = await _unitOfWork.MovimientoVehiculoRepository.GetAllAsync();

        return Ok(movimientoVehiculos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoVehiculo>> Get(int id)
    {
        try
        {
            var movimientoVehiculo = await _unitOfWork.MovimientoVehiculoRepository.GetByIdAsync(id);

            return Ok(movimientoVehiculo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoVehiculo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoVehiculo>> Post([FromBody] MovimientoVehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.MovimientoVehiculoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] MovimientoVehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.MovimientoVehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoVehiculo with id {id} was not found.");
        }

        _unitOfWork.MovimientoVehiculoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        MovimientoVehiculo entity;

        try
        {
            entity = await _unitOfWork.MovimientoVehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MovimientoVehiculo with id {id} was not found.");
        }

        _unitOfWork.MovimientoVehiculoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
