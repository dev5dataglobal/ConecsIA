using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class PersonaTelefonoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public PersonaTelefonoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PersonaTelefono>>> Get()
    {
        var personaTelefonos = await _unitOfWork.PersonaTelefonoRepository.GetAllAsync();

        return Ok(personaTelefonos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaTelefono>> Get(int id)
    {
        try
        {
            var personaTelefono = await _unitOfWork.PersonaTelefonoRepository.GetByIdAsync(id);

            return Ok(personaTelefono);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PersonaTelefono with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PersonaTelefono>> Post([FromBody] PersonaTelefono entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.PersonaTelefonoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] PersonaTelefono entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.PersonaTelefonoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PersonaTelefono with id {id} was not found.");
        }

        _unitOfWork.PersonaTelefonoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        PersonaTelefono entity;

        try
        {
            entity = await _unitOfWork.PersonaTelefonoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"PersonaTelefono with id {id} was not found.");
        }

        _unitOfWork.PersonaTelefonoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
