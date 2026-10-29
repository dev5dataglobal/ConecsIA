using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;

namespace ApiConecsia.Services;

/// <summary>
/// Reglas de las modalidades de autorización de ingreso de personas (PRD §6.6, RN de vigencia). Funciones puras:
/// el controlador aporta la hora local de la sede (ADR-008).
/// Única: un día, un solo ingreso (cupo 1), franja horaria opcional. Diaria: un día, franja opcional.
/// Temporal: desde fecha+hora de inicio hasta fecha+hora de fin. Recurrente: días de la semana y franja, desde una fecha
/// y, si aplica, hasta otra. Permanente: sin fecha final, solo si la política de la organización lo permite (D-18).
/// Provisional hasta D-06: franjas [inicio, fin) sin cruce de medianoche, sin festivos.
/// </summary>
public static class ReglasAutorizacion
{
    public const string Unica = "Unica", Diaria = "Diaria", Temporal = "Temporal", Recurrente = "Recurrente", Permanente = "Permanente";
    public static readonly string[] Modalidades = [Unica, Diaria, Temporal, Recurrente, Permanente];

    public const string Pendiente = "Pendiente", Aprobada = "Aprobada", Rechazada = "Rechazada";
    public static readonly string[] Estados = [Pendiente, Aprobada, Rechazada, "Cancelada", "Revocada", "Vencida", "Agotada"];

    /// <summary>Parámetro de organización que permite solicitar la modalidad Permanente. Ausente = no permitido (D-18).</summary>
    public const string ParametroPermitePermanente = "autorizaciones.permite_permanente";

    /// <summary>Parámetro de organización que exige restringir cada autorización a una o más zonas (RF-05). Ausente = opcional.</summary>
    public const string ParametroExigeZonas = "autorizaciones.exige_zonas";

    public static string TextoModalidad(string modalidad) => modalidad switch
    {
        Unica => "única", Diaria => "diaria", Temporal => "temporal", Recurrente => "recurrente", Permanente => "permanente", _ => modalidad,
    };

    public static DateTime HoraLocal(DateTime instanteUtc, string zonaHoraria) => ReglasVisita.HoraLocal(instanteUtc, zonaHoraria);

    /// <summary>Condiciones normalizadas de una solicitud: solo los campos que aplican a la modalidad.</summary>
    public sealed record Condiciones(
        string Modalidad, DateOnly FechaInicio, DateOnly? FechaFin, TimeOnly? HoraInicio, TimeOnly? HoraFin,
        IReadOnlyList<int> Dias, TimeOnly? HorarioInicio, TimeOnly? HorarioFin);

    /// <summary>Valida la coherencia de fechas, horas y días según la modalidad. <paramref name="ahoraLocal"/>: hora local de la sede.</summary>
    public static (Condiciones? Condiciones, string? Error) Normalizar(AutorizacionAccesoEntrada r, DateTime ahoraLocal)
    {
        if (r.Modalidad is null || !Modalidades.Contains(r.Modalidad)) return (null, "El tipo de autorización no es válido.");
        var hoy = DateOnly.FromDateTime(ahoraLocal);
        var ahora = TimeOnly.FromDateTime(ahoraLocal);
        if (r.FechaInicio is not DateOnly inicio)
            return (null, r.Modalidad is Unica or Diaria ? "Indique el día de la autorización." : "Indique la fecha de inicio.");

        switch (r.Modalidad)
        {
            case Unica or Diaria:
            {
                if (inicio < hoy) return (null, "El día de la autorización no puede ser anterior a hoy.");
                if ((r.HoraInicio is null) != (r.HoraFin is null)) return (null, "Indique ambas horas de la franja o ninguna.");
                if (r.HoraInicio is TimeOnly hi && r.HoraFin is TimeOnly hf)
                {
                    if (hf <= hi) return (null, "La hora de fin debe ser posterior a la de inicio.");
                    if (inicio == hoy && hf <= ahora) return (null, "La franja horaria de hoy ya terminó.");
                }
                return (new(r.Modalidad, inicio, inicio, r.HoraInicio, r.HoraFin, [], null, null), null);
            }
            case Temporal:
            {
                if (r.HoraInicio is not TimeOnly hi) return (null, "Indique la hora de inicio.");
                if (r.FechaFin is not DateOnly fin) return (null, "Indique la fecha de finalización.");
                if (r.HoraFin is not TimeOnly hf) return (null, "Indique la hora de finalización.");
                if (fin.ToDateTime(hf) <= inicio.ToDateTime(hi)) return (null, "La fecha y hora de finalización deben ser posteriores a las de inicio.");
                if (fin.ToDateTime(hf) <= ahoraLocal) return (null, "La fecha y hora de finalización ya pasaron.");
                return (new(Temporal, inicio, fin, hi, hf, [], null, null), null);
            }
            case Recurrente:
            {
                if (r.FechaFin is DateOnly fin)
                {
                    if (fin < inicio) return (null, "La fecha final del periodo no puede ser anterior a la inicial.");
                    if (fin < hoy) return (null, "El periodo ya terminó: la fecha final es anterior a hoy.");
                }
                var dias = (r.DiasSemana ?? []).Distinct().Order().ToList();
                if (dias.Count == 0) return (null, "Seleccione al menos un día de la semana.");
                if (dias.Any(d => d is < 1 or > 7)) return (null, "Los días de la semana van de 1 (lunes) a 7 (domingo).");
                if (r.HoraInicio is not TimeOnly hi || r.HoraFin is not TimeOnly hf) return (null, "Indique la hora de inicio y de fin del horario.");
                if (hf <= hi) return (null, "La hora de fin debe ser posterior a la de inicio (sin franjas nocturnas por ahora).");
                if (r.FechaFin is DateOnly f && !Enumerable.Range(0, Math.Min(7, f.DayNumber - inicio.DayNumber + 1))
                        .Any(i => dias.Contains(DiaIso(inicio.AddDays(i)))))
                    return (null, "Ningún día seleccionado cae dentro del periodo.");
                return (new(Recurrente, inicio, r.FechaFin, null, null, dias, hi, hf), null);
            }
            default: // Permanente
                if (r.FechaFin is not null) return (null, "La autorización permanente no tiene fecha final.");
                return (new(Permanente, inicio, null, null, null, [], null, null), null);
        }
    }

    public static int DiaIso(DateOnly fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)fecha.DayOfWeek;

    /// <summary>El periodo de la autorización ya terminó en <paramref name="ahoraLocal"/>.</summary>
    public static bool Vencio(Autorizacion a, DateTime ahoraLocal) =>
        Vencio(a.Modalidad, a.FechaFin, a.HoraFin, ahoraLocal);

    private static bool Vencio(string modalidad, DateOnly? fechaFin, TimeOnly? horaFin, DateTime ahoraLocal)
    {
        if (fechaFin is not DateOnly fin) return false;
        var hoy = DateOnly.FromDateTime(ahoraLocal);
        if (fin < hoy) return true;
        // Temporal termina en un instante; Única y Diaria con franja terminan al acabar la franja del día.
        return fin == hoy && horaFin is TimeOnly hf && modalidad is Temporal or Unica or Diaria && TimeOnly.FromDateTime(ahoraLocal) >= hf;
    }

    /// <summary>Vigencia calculada (no persistida) de una autorización aprobada: Vigente | Programada | Vencida | Consumida.</summary>
    public static string Vigencia(string estado, string modalidad, DateOnly fechaInicio, DateOnly? fechaFin, TimeOnly? horaInicio, TimeOnly? horaFin,
        int? cupo, int consumidos, DateTime ahoraLocal)
    {
        if (estado != Aprobada) return "NoAplica";
        if (cupo is int c && consumidos >= c) return "Consumida";
        if (Vencio(modalidad, fechaFin, horaFin, ahoraLocal)) return "Vencida";
        var hoy = DateOnly.FromDateTime(ahoraLocal);
        if (fechaInicio > hoy || (modalidad == Temporal && fechaInicio == hoy && horaInicio is TimeOnly hi && TimeOnly.FromDateTime(ahoraLocal) < hi))
            return "Programada";
        return "Vigente";
    }

    /// <summary>
    /// Motivos de la franja propia de la autorización (Temporal: instantes de inicio y fin; Única/Diaria: franja del día).
    /// Los días y horarios de Recurrente/Permanente se evalúan con autorizacion_horario.
    /// </summary>
    public static List<string> MotivosFranja(Autorizacion a, DateTime ahoraLocal)
    {
        var motivos = new List<string>();
        if (a.HoraInicio is not TimeOnly hi || a.HoraFin is not TimeOnly hf) return motivos;
        var hoy = DateOnly.FromDateTime(ahoraLocal);
        var ahora = TimeOnly.FromDateTime(ahoraLocal);
        if (a.Modalidad == Temporal)
        {
            if (hoy == a.FechaInicio && ahora < hi) motivos.Add($"La autorización empieza a regir el {a.FechaInicio:yyyy-MM-dd} a las {hi:HH\\:mm}.");
            if (hoy == a.FechaFin && ahora >= hf) motivos.Add($"La autorización venció el {a.FechaFin:yyyy-MM-dd} a las {hf:HH\\:mm}.");
        }
        else if (ahora < hi || ahora >= hf)
            motivos.Add($"Fuera de la franja autorizada ({hi:HH\\:mm}–{hf:HH\\:mm}).");
        return motivos;
    }
}
