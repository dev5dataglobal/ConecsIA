using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Configuracion)]
public class ModuloController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public ModuloController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Modulo>>> Get()
    {
        var modulos = await _unitOfWork.ModuloRepository.GetAllAsync();

        return Ok(modulos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Modulo>> Get(int id)
    {
        try
        {
            var modulo = await _unitOfWork.ModuloRepository.GetByIdAsync(id);

            return Ok(modulo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Modulo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Modulo>> Post([FromBody] Modulo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.ModuloRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Modulo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.ModuloRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Modulo with id {id} was not found.");
        }

        _unitOfWork.ModuloRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Modulo entity;

        try
        {
            entity = await _unitOfWork.ModuloRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Modulo with id {id} was not found.");
        }

        _unitOfWork.ModuloRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
