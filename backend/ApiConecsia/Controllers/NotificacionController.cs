using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Alertas)]
public class NotificacionController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificacionController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Notificacion>>> Get()
    {
        var notificacions = await _unitOfWork.NotificacionRepository.GetAllAsync();

        return Ok(notificacions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Notificacion>> Get(int id)
    {
        try
        {
            var notificacion = await _unitOfWork.NotificacionRepository.GetByIdAsync(id);

            return Ok(notificacion);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Notificacion with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Notificacion>> Post([FromBody] Notificacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.NotificacionRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] Notificacion entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.NotificacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Notificacion with id {id} was not found.");
        }

        _unitOfWork.NotificacionRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        Notificacion entity;

        try
        {
            entity = await _unitOfWork.NotificacionRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Notificacion with id {id} was not found.");
        }

        _unitOfWork.NotificacionRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
