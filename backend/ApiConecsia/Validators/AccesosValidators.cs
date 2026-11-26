using ApiConecsia.Services;
using ConecsIA.Application.DTOs;
using FluentValidation;

namespace ApiConecsia.Validators;

/// <summary>
/// Validaciones estructurales de las entradas del módulo Control de accesos (ADR-003, FluentValidation). Las reglas que
/// dependen de la base o de la hora local de la sede (vigencia, alcance, categoría, incompatibles) las aplica el controlador.
/// </summary>
public sealed class FranjaHorariaValidator : AbstractValidator<FranjaHorariaDto>
{
    public FranjaHorariaValidator()
    {
        RuleFor(x => x.DiaSemana).InclusiveBetween(1, 7).WithMessage("Los días de la semana van de 1 (lunes) a 7 (domingo).");
        RuleFor(x => x.HoraFin).GreaterThan(x => x.HoraInicio).WithMessage("En cada franja la hora de fin debe ser posterior a la de inicio.");
    }
}

public sealed class RegistroAccesoValidator : AbstractValidator<RegistroAccesoEntrada>
{
    public RegistroAccesoValidator()
    {
        RuleFor(x => x.IdPersona).GreaterThan(0).WithMessage("Seleccione la persona.");
        RuleFor(x => x.IdCategoriaAcceso).GreaterThan(0).WithMessage("Seleccione la categoría de acceso.");
        RuleFor(x => x.IdSede).GreaterThan(0).WithMessage("Seleccione la sede.");
        RuleFor(x => x.IdArea).GreaterThan(0).WithMessage("Seleccione el destino.");
        RuleFor(x => x.IdMembresiaResponsable).GreaterThan(0).WithMessage("Seleccione el anfitrión o responsable.");
        RuleFor(x => x.Modalidad).Must(m => m is not null && ReglasAutorizacion.Modalidades.Contains(m)).WithMessage("El tipo de permanencia no es válido.");
        RuleFor(x => x.FechaInicio).NotNull().WithMessage("Indique la fecha de inicio.");
        RuleFor(x => x.Motivo).MaximumLength(500);
        RuleFor(x => x.Observacion).MaximumLength(1000);
        RuleForEach(x => x.Franjas).SetValidator(new FranjaHorariaValidator());
        RuleFor(x => x.Franjas).Must(f => ReglasAcceso.ValidarFranjas(Convertir(f)) is null)
            .WithMessage(x => ReglasAcceso.ValidarFranjas(Convertir(x.Franjas)) ?? "");
        RuleFor(x => x).Must(x => !(x.UsaHorarioBase && x.Franjas.Count > 0))
            .WithName("Franjas").WithMessage("Use el horario base o franjas propias, no ambos.");
        RuleForEach(x => x.Objetos).ChildRules(o =>
        {
            o.RuleFor(x => x.TipoObjeto).Must(t => t is "Vehiculo" or "Dispositivo").WithMessage("El objeto debe ser Vehiculo o Dispositivo.");
            o.RuleFor(x => x.IdObjeto).NotNull().GreaterThan(0).WithMessage("Seleccione el vehículo o dispositivo.");
        });
        RuleFor(x => x.Objetos).Must(o => o.Select(x => (x.TipoObjeto, x.IdObjeto)).Distinct().Count() == o.Count)
            .WithMessage("Un vehículo o dispositivo está repetido.");
    }

    internal static List<ReglasAcceso.Franja> Convertir(IEnumerable<FranjaHorariaDto> franjas) =>
        franjas.Select(f => new ReglasAcceso.Franja(f.DiaSemana, f.HoraInicio, f.HoraFin)).ToList();
}

public sealed class HorarioAccesoValidator : AbstractValidator<HorarioAccesoEntrada>
{
    public HorarioAccesoValidator()
    {
        RuleForEach(x => x.Franjas).SetValidator(new FranjaHorariaValidator());
        RuleFor(x => x.Franjas).Must(f => ReglasAcceso.ValidarFranjas(RegistroAccesoValidator.Convertir(f)) is null)
            .WithMessage(x => ReglasAcceso.ValidarFranjas(RegistroAccesoValidator.Convertir(x.Franjas)) ?? "");
        RuleFor(x => x).Must(x => x.UsaHorarioBase != (x.Franjas.Count > 0))
            .WithName("Franjas").WithMessage("Indique franjas propias o use el horario base.");
        RuleFor(x => x.Motivo).NotEmpty().WithMessage("Indique el motivo del cambio de horario.").MaximumLength(500);
    }
}

public sealed class HorarioBaseValidator : AbstractValidator<HorarioBaseEntrada>
{
    public HorarioBaseValidator()
    {
        RuleForEach(x => x.Franjas).SetValidator(new FranjaHorariaValidator());
        RuleFor(x => x.Franjas).Must(f => ReglasAcceso.ValidarFranjas(RegistroAccesoValidator.Convertir(f)) is null)
            .WithMessage(x => ReglasAcceso.ValidarFranjas(RegistroAccesoValidator.Convertir(x.Franjas)) ?? "");
    }
}

public sealed class MovimientoAccesoValidator : AbstractValidator<MovimientoAccesoEntrada>
{
    public MovimientoAccesoValidator()
    {
        RuleFor(x => x.IdPuntoAcceso).GreaterThan(0).WithMessage("Seleccione el punto de acceso.");
        RuleFor(x => x.Observacion).MaximumLength(500);
    }
}

public sealed class ObjetoAccesoValidator : AbstractValidator<ObjetoAccesoEntrada>
{
    public ObjetoAccesoValidator()
    {
        RuleFor(x => x.TipoObjeto).Must(t => t is "Vehiculo" or "Dispositivo").WithMessage("El objeto debe ser Vehiculo o Dispositivo.");
        RuleFor(x => x.IdObjeto).NotNull().GreaterThan(0).WithMessage("Seleccione el vehículo o dispositivo.");
    }
}

public sealed class DecisionAccesoValidator : AbstractValidator<DecisionAccesoEntrada>
{
    public DecisionAccesoValidator() => RuleFor(x => x.Motivo).MaximumLength(500);
}

public sealed class CategoriaAccesoValidator : AbstractValidator<CategoriaAccesoEntrada>
{
    public CategoriaAccesoValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(40).Matches("^[A-Za-z0-9_]+$").WithMessage("El código admite letras, números y guion bajo.");
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("Indique el nombre visible de la categoría.").MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(500);
        RuleFor(x => x.FlujoAprobacion).Must(f => f is not null && ReglasAcceso.Flujos.Contains(f)).WithMessage("El flujo debe ser Automatico, Anfitrion o Seguridad.");
        RuleFor(x => x.ModalidadesPermitidas).NotEmpty().WithMessage("Seleccione al menos un tipo de permanencia.")
            .Must(m => m.All(ReglasAutorizacion.Modalidades.Contains)).WithMessage("Hay un tipo de permanencia no válido.");
    }
}
