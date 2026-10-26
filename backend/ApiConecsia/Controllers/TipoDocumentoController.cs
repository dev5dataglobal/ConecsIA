using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class TipoDocumentoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public TipoDocumentoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TipoDocumento>>> Get()
    {
        var tipoDocumentos = await _unitOfWork.TipoDocumentoRepository.GetAllAsync();

        return Ok(tipoDocumentos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoDocumento>> Get(int id)
    {
        try
        {
            var tipoDocumento = await _unitOfWork.TipoDocumentoRepository.GetByIdAsync(id);

            return Ok(tipoDocumento);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoDocumento with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoDocumento>> Post([FromBody] TipoDocumento entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.TipoDocumentoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] TipoDocumento entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.TipoDocumentoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoDocumento with id {id} was not found.");
        }

        _unitOfWork.TipoDocumentoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        TipoDocumento entity;

        try
        {
            entity = await _unitOfWork.TipoDocumentoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoDocumento with id {id} was not found.");
        }

        _unitOfWork.TipoDocumentoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
