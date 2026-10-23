using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Personas)]
public class PersonaVehiculoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PersonaVehiculoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonaVehiculo>>> Get()
    {
        var personaVehiculos = await _unitOfWork.PersonaVehiculoRepository.GetAllAsync();

        return Ok(personaVehiculos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaVehiculo>> Get(int id)
    {
        try
        {
            var personaVehiculo = await _unitOfWork.PersonaVehiculoRepository.GetByIdAsync(id);

            return Ok(personaVehiculo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PersonaVehiculo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PersonaVehiculo>> Post([FromBody] PersonaVehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PersonaVehiculoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] PersonaVehiculo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PersonaVehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PersonaVehiculo with id {id} was not found.");
        }

        _unitOfWork.PersonaVehiculoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        PersonaVehiculo entity;

        try
        {
            entity = await _unitOfWork.PersonaVehiculoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PersonaVehiculo with id {id} was not found.");
        }

        _unitOfWork.PersonaVehiculoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
