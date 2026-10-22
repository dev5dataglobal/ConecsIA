using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class RolPermisoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public RolPermisoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RolPermiso>>> Get()
    {
        var rolPermisos = await _unitOfWork.RolPermisoRepository.GetAllAsync();

        return Ok(rolPermisos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolPermiso>> Get(int id)
    {
        try
        {
            var rolPermiso = await _unitOfWork.RolPermisoRepository.GetByIdAsync(id);

            return Ok(rolPermiso);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"RolPermiso with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RolPermiso>> Post([FromBody] RolPermiso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.RolPermisoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] RolPermiso entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.RolPermisoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"RolPermiso with id {id} was not found.");
        }

        _unitOfWork.RolPermisoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        RolPermiso entity;

        try
        {
            entity = await _unitOfWork.RolPermisoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"RolPermiso with id {id} was not found.");
        }

        _unitOfWork.RolPermisoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
