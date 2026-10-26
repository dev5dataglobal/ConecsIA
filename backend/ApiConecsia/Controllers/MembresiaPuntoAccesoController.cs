using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Usuarios)]
public class MembresiaPuntoAccesoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public MembresiaPuntoAccesoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MembresiaPuntoAcceso>>> Get()
    {
        var membresiaPuntoAccesos = await _unitOfWork.MembresiaPuntoAccesoRepository.GetAllAsync();

        return Ok(membresiaPuntoAccesos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembresiaPuntoAcceso>> Get(int id)
    {
        try
        {
            var membresiaPuntoAcceso = await _unitOfWork.MembresiaPuntoAccesoRepository.GetByIdAsync(id);

            return Ok(membresiaPuntoAcceso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaPuntoAcceso with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembresiaPuntoAcceso>> Post([FromBody] MembresiaPuntoAcceso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.MembresiaPuntoAccesoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] MembresiaPuntoAcceso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.MembresiaPuntoAccesoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaPuntoAcceso with id {id} was not found.");
        }

        _unitOfWork.MembresiaPuntoAccesoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        MembresiaPuntoAcceso entity;

        try
        {
            entity = await _unitOfWork.MembresiaPuntoAccesoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaPuntoAcceso with id {id} was not found.");
        }

        _unitOfWork.MembresiaPuntoAccesoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
