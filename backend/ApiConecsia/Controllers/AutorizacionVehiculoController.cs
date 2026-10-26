using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class AutorizacionVehiculoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AutorizacionVehiculoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AutorizacionVehiculo>>> Get()
    {
        var autorizacionVehiculos = await _unitOfWork.AutorizacionVehiculoRepository.GetAllAsync();

        return Ok(autorizacionVehiculos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutorizacionVehiculo>> Get(int id)
    {
        try
        {
            var autorizacionVehiculo = await _unitOfWork.AutorizacionVehiculoRepository.GetByIdAsync(id);

            return Ok(autorizacionVehiculo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionVehiculo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutorizacionVehiculo>> Post([FromBody] AutorizacionVehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AutorizacionVehiculoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] AutorizacionVehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AutorizacionVehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionVehiculo with id {id} was not found.");
        }

        _unitOfWork.AutorizacionVehiculoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        AutorizacionVehiculo entity;

        try
        {
            entity = await _unitOfWork.AutorizacionVehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionVehiculo with id {id} was not found.");
        }

        _unitOfWork.AutorizacionVehiculoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
