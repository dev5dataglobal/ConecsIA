using ApiConecsia.Helpers;
using ConecsIA.Application.Seguridad;
using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
[RequiereModulo(CodigosModulo.Autorizaciones)]
public class InvitacionEnvioController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public InvitacionEnvioController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InvitacionEnvio>>> Get()
    {
        var invitacionEnvios = await _unitOfWork.InvitacionEnvioRepository.GetAllAsync();

        return Ok(invitacionEnvios);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvitacionEnvio>> Get(int id)
    {
        try
        {
            var invitacionEnvio = await _unitOfWork.InvitacionEnvioRepository.GetByIdAsync(id);

            return Ok(invitacionEnvio);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"InvitacionEnvio with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvitacionEnvio>> Post([FromBody] InvitacionEnvio entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.InvitacionEnvioRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] InvitacionEnvio entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.InvitacionEnvioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"InvitacionEnvio with id {id} was not found.");
        }

        _unitOfWork.InvitacionEnvioRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        InvitacionEnvio entity;

        try
        {
            entity = await _unitOfWork.InvitacionEnvioRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"InvitacionEnvio with id {id} was not found.");
        }

        _unitOfWork.InvitacionEnvioRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
