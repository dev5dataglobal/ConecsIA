using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiConecsia.Controllers;

[Route("api/[controller]")]
public class TokenQrController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public TokenQrController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TokenQr>>> Get()
    {
        var tokenQrs = await _unitOfWork.TokenQrRepository.GetAllAsync();

        return Ok(tokenQrs);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TokenQr>> Get(int id)
    {
        try
        {
            var tokenQr = await _unitOfWork.TokenQrRepository.GetByIdAsync(id);

            return Ok(tokenQr);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TokenQr with id {id} was not found.");
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TokenQr>> Post([FromBody] TokenQr entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        _unitOfWork.TokenQrRepository.Add(entity);
        await _unitOfWork.SaveAsync();

        return CreatedAtAction(nameof(Get), new { id = 0 }, entity);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(int id, [FromBody] TokenQr entity)
    {
        if (entity == null)
        {
            return BadRequest();
        }

        try
        {
            await _unitOfWork.TokenQrRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TokenQr with id {id} was not found.");
        }

        _unitOfWork.TokenQrRepository.Update(entity);
        await _unitOfWork.SaveAsync();

        return Ok(entity);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        TokenQr entity;

        try
        {
            entity = await _unitOfWork.TokenQrRepository.GetByIdAsync(id);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"TokenQr with id {id} was not found.");
        }

        _unitOfWork.TokenQrRepository.Remove(entity);
        await _unitOfWork.SaveAsync();

        return NoContent();
    }
}
