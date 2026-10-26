using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class PermisoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PermisoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Permiso>>> Get()
    {
        var permisos = await _unitOfWork.PermisoRepository.GetAllAsync();

        return Ok(permisos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Permiso>> Get(int id)
    {
        try
        {
            var permiso = await _unitOfWork.PermisoRepository.GetByIdAsync(id);

            return Ok(permiso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Permiso with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Permiso>> Post([FromBody] Permiso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PermisoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Permiso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PermisoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Permiso with id {id} was not found.");
        }

        _unitOfWork.PermisoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Permiso entity;

        try
        {
            entity = await _unitOfWork.PermisoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Permiso with id {id} was not found.");
        }

        _unitOfWork.PermisoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
