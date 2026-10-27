using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Vehiculos)]
public class VehiculoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public VehiculoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Vehiculo>>> Get()
    {
        var vehiculos = await _unitOfWork.VehiculoRepository.GetAllAsync();

        return Ok(vehiculos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Vehiculo>> Get(int id)
    {
        try
        {
            var vehiculo = await _unitOfWork.VehiculoRepository.GetByIdAsync(id);

            return Ok(vehiculo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Vehiculo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Vehiculo>> Post([FromBody] Vehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.VehiculoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Vehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.VehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Vehiculo with id {id} was not found.");
        }

        _unitOfWork.VehiculoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Vehiculo entity;

        try
        {
            entity = await _unitOfWork.VehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Vehiculo with id {id} was not found.");
        }

        _unitOfWork.VehiculoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
