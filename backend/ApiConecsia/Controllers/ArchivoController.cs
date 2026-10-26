using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class ArchivoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public ArchivoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Archivo>>> Get()
    {
        var archivos = await _unitOfWork.ArchivoRepository.GetAllAsync();

        return Ok(archivos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Archivo>> Get(int id)
    {
        try
        {
            var archivo = await _unitOfWork.ArchivoRepository.GetByIdAsync(id);

            return Ok(archivo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Archivo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Archivo>> Post([FromBody] Archivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.ArchivoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Archivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.ArchivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Archivo with id {id} was not found.");
        }

        _unitOfWork.ArchivoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Archivo entity;

        try
        {
            entity = await _unitOfWork.ArchivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Archivo with id {id} was not found.");
        }

        _unitOfWork.ArchivoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
