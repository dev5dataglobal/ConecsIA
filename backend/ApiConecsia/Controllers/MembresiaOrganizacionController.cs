using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class MembresiaOrganizacionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public MembresiaOrganizacionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MembresiaOrganizacion>>> Get()
    {
        var membresiaOrganizacions = await _unitOfWork.MembresiaOrganizacionRepository.GetAllAsync();

        return Ok(membresiaOrganizacions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembresiaOrganizacion>> Get(int id)
    {
        try
        {
            var membresiaOrganizacion = await _unitOfWork.MembresiaOrganizacionRepository.GetByIdAsync(id);

            return Ok(membresiaOrganizacion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaOrganizacion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MembresiaOrganizacion>> Post([FromBody] MembresiaOrganizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.MembresiaOrganizacionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] MembresiaOrganizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.MembresiaOrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaOrganizacion with id {id} was not found.");
        }

        _unitOfWork.MembresiaOrganizacionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        MembresiaOrganizacion entity;

        try
        {
            entity = await _unitOfWork.MembresiaOrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"MembresiaOrganizacion with id {id} was not found.");
        }

        _unitOfWork.MembresiaOrganizacionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
