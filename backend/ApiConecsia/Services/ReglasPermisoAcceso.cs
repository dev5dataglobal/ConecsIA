using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

/// <summary>
/// Tipo de permiso de acceso de una persona, <b>derivado</b> de sus autorizaciones (Propuesta, D-46; reemplaza el campo
/// manual retirado en D-38). No se almacena: clasifica, no autoriza; cada ingreso sigue evaluando la autorización.
/// Recurrente en una sede = autorización Recurrente o Permanente, Aprobada y con la fecha local de esa sede dentro de
/// [fecha inicio, fecha fin]. Día de la semana y horario no cuentan (fuera de horario sigue siendo recurrente).
/// </summary>
public static class ReglasPermisoAcceso
{
    public const string Recurrente = "Recurrente", Extemporaneo = "Extemporaneo";

    public static bool ClasificaRecurrente(Autorizacion a, DateOnly hoyLocal) =>
        a.Estado == ReglasVisita.AutorizacionAprobada
        && a.Modalidad is ReglasAutorizacion.Recurrente or ReglasAutorizacion.Permanente
        && a.FechaInicio <= hoyLocal && (a.FechaFin is not DateOnly fin || fin >= hoyLocal);

    /// <summary>
    /// Accesos recurrentes vigentes por persona, limitados a las sedes del alcance (null = todas).
    /// <paramref name="idsPersona"/> null = todas las personas de la organización (filtro de listado).
    /// </summary>
    public static async Task<Dictionary<int, List<AccesoRecurrenteDto>>> AccesosAsync(
        ConecsiaDbContext db, IReadOnlyCollection<int>? idsPersona, IReadOnlyCollection<int>? alcance, int? idSede, DateTime ahoraUtc, CancellationToken ct)
    {
        // Margen de ±1 día en SQL para cubrir cualquier zona horaria; el corte exacto se hace con la hora local de cada sede.
        var desde = DateOnly.FromDateTime(ahoraUtc).AddDays(-1);
        var hasta = DateOnly.FromDateTime(ahoraUtc).AddDays(1);
        var candidatas = await (
            from a in db.Autorizaciones.AsNoTracking()
            join s in db.Sedes on a.IdSede equals s.IdSede
            where a.Estado == ReglasVisita.AutorizacionAprobada
                && (a.Modalidad == ReglasAutorizacion.Recurrente || a.Modalidad == ReglasAutorizacion.Permanente)
                && a.FechaInicio <= hasta && (a.FechaFin == null || a.FechaFin >= desde)
                && (idsPersona == null || idsPersona.Contains(a.IdPersona))
                && (alcance == null || alcance.Contains(a.IdSede))
                && (idSede == null || a.IdSede == idSede)
            select new { Autorizacion = a, Sede = s.Nombre, s.ZonaHoraria }).ToListAsync(ct);

        return candidatas
            .Where(x => ClasificaRecurrente(x.Autorizacion, DateOnly.FromDateTime(ReglasVisita.HoraLocal(ahoraUtc, x.ZonaHoraria))))
            .GroupBy(x => x.Autorizacion.IdPersona)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Sede).ThenBy(x => x.Autorizacion.IdAutorizacion).Select(x => new AccesoRecurrenteDto
            {
                IdAutorizacion = x.Autorizacion.IdAutorizacion, Modalidad = x.Autorizacion.Modalidad, IdSede = x.Autorizacion.IdSede, Sede = x.Sede,
                FechaInicio = x.Autorizacion.FechaInicio, FechaFin = x.Autorizacion.FechaFin,
            }).ToList());
    }
}
