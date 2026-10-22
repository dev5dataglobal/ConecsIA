using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class UsuarioController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Usuario>>> Get()
    {
        var usuarios = await _unitOfWork.UsuarioRepository.GetAllAsync();

        return Ok(usuarios);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Usuario>> Get(int id)
    {
        try
        {
            var usuario = await _unitOfWork.UsuarioRepository.GetByIdAsync(id);

            return Ok(usuario);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Usuario with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Usuario>> Post([FromBody] Usuario entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.UsuarioRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Usuario entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.UsuarioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Usuario with id {id} was not found.");
        }

        _unitOfWork.UsuarioRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Usuario entity;

        try
        {
            entity = await _unitOfWork.UsuarioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Usuario with id {id} was not found.");
        }

        _unitOfWork.UsuarioRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
