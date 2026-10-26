using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class OrganizacionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public OrganizacionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Organizacion>>> Get()
    {
        var organizacions = await _unitOfWork.OrganizacionRepository.GetAllAsync();

        return Ok(organizacions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Organizacion>> Get(int id)
    {
        try
        {
            var organizacion = await _unitOfWork.OrganizacionRepository.GetByIdAsync(id);

            return Ok(organizacion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Organizacion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Organizacion>> Post([FromBody] Organizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.OrganizacionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Organizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.OrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Organizacion with id {id} was not found.");
        }

        _unitOfWork.OrganizacionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Organizacion entity;

        try
        {
            entity = await _unitOfWork.OrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Organizacion with id {id} was not found.");
        }

        _unitOfWork.OrganizacionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
