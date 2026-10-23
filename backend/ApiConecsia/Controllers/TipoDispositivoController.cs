using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Dispositivos)]
public class TipoDispositivoController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public TipoDispositivoController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TipoDispositivo>>> Get()
    {
        var tipoDispositivos = await _unitOfWork.TipoDispositivoRepository.GetAllAsync();

        return Ok(tipoDispositivos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoDispositivo>> Get(int id)
    {
        try
        {
            var tipoDispositivo = await _unitOfWork.TipoDispositivoRepository.GetByIdAsync(id);

            return Ok(tipoDispositivo);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoDispositivo with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TipoDispositivo>> Post([FromBody] TipoDispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.TipoDispositivoRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] TipoDispositivo entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.TipoDispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoDispositivo with id {id} was not found.");
        }

        _unitOfWork.TipoDispositivoRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        TipoDispositivo entity;

        try
        {
            entity = await _unitOfWork.TipoDispositivoRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TipoDispositivo with id {id} was not found.");
        }

        _unitOfWork.TipoDispositivoRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
