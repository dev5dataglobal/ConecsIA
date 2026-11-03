namespace ConecsIA.Domain.Entities;

/// <summary>Franja de ingreso por defecto (ADR-029). <see cref="IdSede"/> nulo = horario de la organización; con sede, lo sobrescribe para esa sede.</summary>
public class HorarioBase : OrganizacionEntity { public int IdHorarioBase { get; set; } public int? IdSede { get; set; } public int DiaSemana { get; set; } public TimeOnly HoraInicio { get; set; } public TimeOnly HoraFin { get; set; } }
