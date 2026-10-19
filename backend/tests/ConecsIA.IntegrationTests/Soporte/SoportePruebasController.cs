using ConecsIA.SharedKernel.Errores;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.IntegrationTests.Soporte;

/// <summary>Controlador que existe solo en pruebas para ejercitar el pipeline de errores.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/soporte-pruebas")]
public sealed class SoportePruebasController : ControllerBase
{
    public sealed record SolicitudEjemplo(string? Nombre, int Cantidad);

    public sealed class ValidadorEjemplo : AbstractValidator<SolicitudEjemplo>
    {
        public ValidadorEjemplo()
        {
            RuleFor(s => s.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.");
            RuleFor(s => s.Cantidad).GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
        }
    }

    [HttpPost("validacion")]
    public IActionResult Validar(SolicitudEjemplo solicitud) => Ok(solicitud);

    [HttpGet("regla")]
    public IActionResult Regla() => throw new ReglaNegocioException("SEDE_INACTIVA", "La sede está inactiva.");

    [HttpGet("no-encontrado")]
    public IActionResult NoEncontrado() => throw new RecursoNoEncontradoException("la sede");

    [HttpGet("no-controlada")]
    public IActionResult NoControlada() =>
        throw new InvalidOperationException("SELECT * FROM persona WHERE documento = '1023456789'");
}
