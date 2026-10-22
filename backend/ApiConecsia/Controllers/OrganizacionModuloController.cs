using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class OrganizacionModuloController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public OrganizacionModuloController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrganizacionModulo>>> Get()
    {
        var organizacionModulos = await _unitOfWork.OrganizacionModuloRepository.GetAllAsync();

        return Ok(organizacionModulos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizacionModulo>> Get(int id)
    {
        try
        {
            var organizacionModulo = await _unitOfWork.OrganizacionModuloRepository.GetByIdAsync(id);

            return Ok(organizacionModulo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"OrganizacionModulo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrganizacionModulo>> Post([FromBody] OrganizacionModulo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.OrganizacionModuloRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] OrganizacionModulo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.OrganizacionModuloRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"OrganizacionModulo with id {id} was not found.");
        }

        _unitOfWork.OrganizacionModuloRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        OrganizacionModulo entity;

        try
        {
            entity = await _unitOfWork.OrganizacionModuloRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"OrganizacionModulo with id {id} was not found.");
        }

        _unitOfWork.OrganizacionModuloRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
