using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Configuracion)]
public class TipoOrganizacionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public TipoOrganizacionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TipoOrganizacion>>> Get()
    {
        var tipoOrganizacions = await _unitOfWork.TipoOrganizacionRepository.GetAllAsync();

        return Ok(tipoOrganizacions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoOrganizacion>> Get(int id)
    {
        try
        {
            var tipoOrganizacion = await _unitOfWork.TipoOrganizacionRepository.GetByIdAsync(id);

            return Ok(tipoOrganizacion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoOrganizacion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoOrganizacion>> Post([FromBody] TipoOrganizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.TipoOrganizacionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] TipoOrganizacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.TipoOrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoOrganizacion with id {id} was not found.");
        }

        _unitOfWork.TipoOrganizacionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        TipoOrganizacion entity;

        try
        {
            entity = await _unitOfWork.TipoOrganizacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoOrganizacion with id {id} was not found.");
        }

        _unitOfWork.TipoOrganizacionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
