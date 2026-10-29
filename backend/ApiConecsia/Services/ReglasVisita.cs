using ConecsIA.Domain.Entities;

namespace ApiConecsia.Services;

/// <summary>
/// Reglas del ciclo de una visita (PRD §6.7, ADR-009 en Propuesta) y evaluación de vigencia de la autorización que la
/// habilita. Funciones puras: el controlador aporta los datos leídos bajo bloqueo y el instante de la operación.
/// Estados persistidos: Pendiente → Autorizada → Ingreso → Finalizada; Pendiente/Autorizada → Rechazada | Cancelada | Vencida.
/// «Aprobada» no es «vigente»: se exige además fecha, días/horario, sede, destino, zonas y cupo.
/// Provisional hasta D-06: horario [inicio, fin) en la hora local de la sede, días ISO (1 = lunes … 7 = domingo),
/// sin franjas nocturnas (una franja con fin ≤ inicio no habilita) ni festivos.
/// </summary>
public static class ReglasVisita
{
    public const string Pendiente = "Pendiente", Autorizada = "Autorizada", Ingreso = "Ingreso", Finalizada = "Finalizada";
    public const string Rechazada = "Rechazada", Cancelada = "Cancelada", Vencida = "Vencida";

    public static readonly string[] Estados = [Pendiente, Autorizada, Ingreso, Finalizada, Rechazada, Cancelada, Vencida];

    /// <summary>Parámetro de organización que permite visitas sin autorización previa. Ausente = no permitido (D-04).</summary>
    public const string ParametroSinAutorizacion = "visitas.permite_sin_autorizacion";

    /// <summary>Estados de la autorización de la persona (PRD §6.6). Solo Aprobada habilita, y solo si además está vigente.</summary>
    public const string AutorizacionPendiente = "Pendiente", AutorizacionAprobada = "Aprobada";

    public static bool EsTerminal(string estado) => estado is Finalizada or Rechazada or Cancelada or Vencida;

    /// <summary>Se puede cancelar antes del ingreso; después, la permanencia sigue abierta hasta su salida.</summary>
    public static bool Cancelable(string estado) => estado is Pendiente or Autorizada;

    public static DateTime HoraLocal(DateTime instanteUtc, string zonaHoraria)
    {
        var zona = TimeZoneInfo.TryFindSystemTimeZoneById(zonaHoraria, out var encontrada) ? encontrada : TimeZoneInfo.Utc;
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc), zona);
    }

    public static int DiaIso(DateTime local) => local.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)local.DayOfWeek;

    /// <summary>Cupo efectivo: la modalidad Única admite un solo ingreso aunque no declare cupo (regla base).</summary>
    public static int? CupoEfectivo(Autorizacion a) => a.CupoIngresos ?? (a.Modalidad == "Unica" ? 1 : null);

    /// <summary>
    /// Estado terminal al que debe pasar la visita según su autorización o su fecha esperada (null = ninguno).
    /// Solo se aplica a visitas que aún no ingresaron.
    /// </summary>
    public static string? EstadoDerivado(Visita visita, Autorizacion? autorizacion, DateOnly hoyLocal)
    {
        if (!Cancelable(visita.Estado)) return null;
        if (autorizacion is not null)
        {
            if (autorizacion.Estado == "Rechazada") return Rechazada;
            if (autorizacion.Estado is "Cancelada" or "Revocada") return Cancelada;
            if (autorizacion.Estado is "Vencida" or "Agotada") return Vencida;
            if (autorizacion.FechaFin is DateOnly fin && fin < hoyLocal) return Vencida;
        }
        if (visita.FechaEsperada is DateOnly esperada && esperada < hoyLocal) return Vencida;
        return null;
    }

    /// <summary>
    /// Motivos por los que la autorización no habilita un ingreso ahora (vacío = vigente).
    /// <paramref name="zonasPunto"/>: zona del punto de acceso y sus ancestros (null = aún no se eligió el punto).
    /// </summary>
    public static List<string> MotivosAutorizacion(
        Autorizacion a, IReadOnlyCollection<AutorizacionHorario> horarios, IReadOnlyCollection<int> zonasPermitidas,
        Visita visita, IReadOnlyCollection<int>? zonasPunto, DateTime local)
    {
        var motivos = new List<string>();
        if (a.Estado == AutorizacionPendiente) motivos.Add("La autorización está pendiente de decisión.");
        else if (a.Estado != AutorizacionAprobada) motivos.Add($"La autorización está {a.Estado.ToLowerInvariant()} y no habilita ingresos.");
        if (a.IdPersona != visita.IdPersona) motivos.Add("La autorización corresponde a otra persona.");
        if (a.IdSede != visita.IdSede) motivos.Add("La autorización no es para esta sede.");
        if (a.IdArea != visita.IdArea) motivos.Add("La autorización no es para este destino.");

        var hoy = DateOnly.FromDateTime(local);
        if (a.FechaInicio > hoy) motivos.Add($"La autorización empieza a regir el {a.FechaInicio:yyyy-MM-dd}.");
        if (a.FechaFin is DateOnly fin && fin < hoy) motivos.Add($"La autorización venció el {fin:yyyy-MM-dd}.");
        motivos.AddRange(ReglasAutorizacion.MotivosFranja(a, local));

        if (horarios.Count > 0)
        {
            var dia = DiaIso(local);
            var ahora = TimeOnly.FromDateTime(local);
            var delDia = horarios.Where(h => h.DiaSemana == dia).ToList();
            if (delDia.Count == 0) motivos.Add("La autorización no habilita ingresos este día de la semana.");
            else if (!delDia.Any(h => h.HoraFin > h.HoraInicio && ahora >= h.HoraInicio && ahora < h.HoraFin))
                motivos.Add($"Fuera del horario autorizado ({string.Join(", ", delDia.Select(h => $"{h.HoraInicio:HH\\:mm}–{h.HoraFin:HH\\:mm}"))}).");
        }

        if (zonasPunto is not null && zonasPermitidas.Count > 0 && !zonasPunto.Any(zonasPermitidas.Contains))
            motivos.Add("El punto de acceso no está en las zonas autorizadas.");

        if (CupoEfectivo(a) is int cupo && a.IngresosConsumidos >= cupo)
            motivos.Add(cupo == 1 ? "La autorización de ingreso único ya fue utilizada." : "La autorización agotó su cupo de ingresos.");
        return motivos;
    }

    /// <summary>Motivos de la propia visita (fecha esperada y política sin autorización).</summary>
    public static List<string> MotivosVisita(Visita visita, bool tieneAutorizacion, bool politicaSinAutorizacionVigente, DateOnly hoyLocal)
    {
        var motivos = new List<string>();
        if (visita.FechaEsperada is DateOnly esperada)
        {
            if (esperada > hoyLocal) motivos.Add($"La visita está programada para el {esperada:yyyy-MM-dd}.");
            else if (esperada < hoyLocal) motivos.Add($"La visita venció: estaba prevista para el {esperada:yyyy-MM-dd}.");
        }
        if (!tieneAutorizacion)
        {
            if (visita.PoliticaAplicada is null) motivos.Add("La visita está pendiente de autorización.");
            else if (!politicaSinAutorizacionVigente) motivos.Add("La organización ya no permite ingresos sin autorización previa.");
        }
        return motivos;
    }
}
