using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Autorizaciones)]
public class AutorizacionHorarioController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AutorizacionHorarioController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AutorizacionHorario>>> Get()
    {
        var autorizacionHorarios = await _unitOfWork.AutorizacionHorarioRepository.GetAllAsync();

        return Ok(autorizacionHorarios);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutorizacionHorario>> Get(int id)
    {
        try
        {
            var autorizacionHorario = await _unitOfWork.AutorizacionHorarioRepository.GetByIdAsync(id);

            return Ok(autorizacionHorario);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionHorario with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutorizacionHorario>> Post([FromBody] AutorizacionHorario entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AutorizacionHorarioRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] AutorizacionHorario entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AutorizacionHorarioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionHorario with id {id} was not found.");
        }

        _unitOfWork.AutorizacionHorarioRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        AutorizacionHorario entity;

        try
        {
            entity = await _unitOfWork.AutorizacionHorarioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"AutorizacionHorario with id {id} was not found.");
        }

        _unitOfWork.AutorizacionHorarioRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
