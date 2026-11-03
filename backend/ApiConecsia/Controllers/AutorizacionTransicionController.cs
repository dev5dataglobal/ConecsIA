using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Autorizaciones)]
public class AutorizacionTransicionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AutorizacionTransicionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AutorizacionTransicion>>> Get()
    {
        var autorizacionTransicions = await _unitOfWork.AutorizacionTransicionRepository.GetAllAsync();

        return Ok(autorizacionTransicions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutorizacionTransicion>> Get(int id)
    {
        try
        {
            var autorizacionTransicion = await _unitOfWork.AutorizacionTransicionRepository.GetByIdAsync(id);

            return Ok(autorizacionTransicion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionTransicion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutorizacionTransicion>> Post([FromBody] AutorizacionTransicion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AutorizacionTransicionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] AutorizacionTransicion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AutorizacionTransicionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionTransicion with id {id} was not found.");
        }

        _unitOfWork.AutorizacionTransicionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        AutorizacionTransicion entity;

        try
        {
            entity = await _unitOfWork.AutorizacionTransicionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionTransicion with id {id} was not found.");
        }

        _unitOfWork.AutorizacionTransicionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
