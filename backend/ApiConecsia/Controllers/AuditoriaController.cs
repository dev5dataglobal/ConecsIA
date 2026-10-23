using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Auditoria)]
public class AuditoriaController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditoriaController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Auditoria>>> Get()
    {
        var auditorias = await _unitOfWork.AuditoriaRepository.GetAllAsync();

        return Ok(auditorias);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Auditoria>> Get(int id)
    {
        try
        {
            var auditoria = await _unitOfWork.AuditoriaRepository.GetByIdAsync(id);

            return Ok(auditoria);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Auditoria with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Auditoria>> Post([FromBody] Auditoria entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.AuditoriaRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Auditoria entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.AuditoriaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Auditoria with id {id} was not found.");
        }

        _unitOfWork.AuditoriaRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Auditoria entity;

        try
        {
            entity = await _unitOfWork.AuditoriaRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Auditoria with id {id} was not found.");
        }

        _unitOfWork.AuditoriaRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
