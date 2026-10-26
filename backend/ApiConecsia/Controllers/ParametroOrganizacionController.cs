using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class ParametroOrganizacionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public ParametroOrganizacionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ParametroOrganizacion>>> Get()
    {
        var parametroOrganizacions = await _unitOfWork.ParametroOrganizacionRepository.GetAllAsync();

        return Ok(parametroOrganizacions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParametroOrganizacion>> Get(int id)
    {
        try
        {
            var parametroOrganizacion = await _unitOfWork.ParametroOrganizacionRepository.GetByIdAsync(id);

            return Ok(parametroOrganizacion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"ParametroOrganizacion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ParametroOrganizacion>> Post([FromBody] ParametroOrganizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.ParametroOrganizacionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] ParametroOrganizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.ParametroOrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"ParametroOrganizacion with id {id} was not found.");
        }

        _unitOfWork.ParametroOrganizacionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        ParametroOrganizacion entity;

        try
        {
            entity = await _unitOfWork.ParametroOrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"ParametroOrganizacion with id {id} was not found.");
        }

        _unitOfWork.ParametroOrganizacionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
