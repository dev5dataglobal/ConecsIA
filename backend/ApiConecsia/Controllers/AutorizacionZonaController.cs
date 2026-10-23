using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Autorizaciones)]
public class AutorizacionZonaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AutorizacionZonaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AutorizacionZona>>> Get()
    {
        var autorizacionZonas = await _unitOfWork.AutorizacionZonaRepository.GetAllAsync();

        return Ok(autorizacionZonas);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutorizacionZona>> Get(int id)
    {
        try
        {
            var autorizacionZona = await _unitOfWork.AutorizacionZonaRepository.GetByIdAsync(id);

            return Ok(autorizacionZona);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionZona with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutorizacionZona>> Post([FromBody] AutorizacionZona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AutorizacionZonaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] AutorizacionZona entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AutorizacionZonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionZona with id {id} was not found.");
        }

        _unitOfWork.AutorizacionZonaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        AutorizacionZona entity;

        try
        {
            entity = await _unitOfWork.AutorizacionZonaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionZona with id {id} was not found.");
        }

        _unitOfWork.AutorizacionZonaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
