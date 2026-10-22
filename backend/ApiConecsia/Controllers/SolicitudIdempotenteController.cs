using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class SolicitudIdempotenteController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public SolicitudIdempotenteController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SolicitudIdempotente>>> Get()
    {
        var solicitudIdempotentes = await _unitOfWork.SolicitudIdempotenteRepository.GetAllAsync();

        return Ok(solicitudIdempotentes);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SolicitudIdempotente>> Get(int id)
    {
        try
        {
            var solicitudIdempotente = await _unitOfWork.SolicitudIdempotenteRepository.GetByIdAsync(id);

            return Ok(solicitudIdempotente);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"SolicitudIdempotente with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SolicitudIdempotente>> Post([FromBody] SolicitudIdempotente entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.SolicitudIdempotenteRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] SolicitudIdempotente entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.SolicitudIdempotenteRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"SolicitudIdempotente with id {id} was not found.");
        }

        _unitOfWork.SolicitudIdempotenteRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        SolicitudIdempotente entity;

        try
        {
            entity = await _unitOfWork.SolicitudIdempotenteRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"SolicitudIdempotente with id {id} was not found.");
        }

        _unitOfWork.SolicitudIdempotenteRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
