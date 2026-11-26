using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

/// <summary>
/// Reglas del registro de acceso unificado (ADR-029). Funciones puras: el controlador aporta los datos leídos bajo bloqueo
/// y la hora local de la sede. Estados persistidos: Pendiente, Aprobada, Rechazada, Revocada, Cancelada (Vencida y Agotada
/// solo históricos). Vigencia derivada en cada intento: NoAplica | Programada | Vigente | FueraDeHorario | Vencida | Consumida.
/// Horario efectivo: franjas propias del registro; si no tiene y usa horario base, las de la sede o, en su defecto, las de
/// la organización. Provisional hasta D-06: franjas [inicio, fin) sin cruce de medianoche ni festivos.
/// </summary>
public static class ReglasAcceso
{
    public const string Pendiente = "Pendiente", Aprobada = "Aprobada", Rechazada = "Rechazada", Revocada = "Revocada", Cancelada = "Cancelada";
    public const string FlujoAutomatico = "Automatico", FlujoAnfitrion = "Anfitrion", FlujoSeguridad = "Seguridad";
    public static readonly string[] Flujos = [FlujoAutomatico, FlujoAnfitrion, FlujoSeguridad];

    /// <summary>Fuera de horario: Bloquear (por defecto, también si falta) o Alertar (ingresa y queda auditado). Propuesta D-51.</summary>
    public const string ParametroFueraHorario = "accesos.fuera_horario";
    public const string ParametroPermitePermanente = "accesos.permite_permanente";

    public sealed record Franja(int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

    /// <summary>Valida franjas: día 1..7, fin posterior al inicio y sin solapes en el mismo día. Devuelve el error o null.</summary>
    public static string? ValidarFranjas(IReadOnlyCollection<Franja> franjas)
    {
        if (franjas.Any(f => f.DiaSemana is < 1 or > 7)) return "Los días de la semana van de 1 (lunes) a 7 (domingo).";
        if (franjas.Any(f => f.HoraFin <= f.HoraInicio)) return "En cada franja la hora de fin debe ser posterior a la de inicio (sin franjas nocturnas por ahora).";
        foreach (var dia in franjas.GroupBy(f => f.DiaSemana))
        {
            var orden = dia.OrderBy(f => f.HoraInicio).ToList();
            for (var i = 1; i < orden.Count; i++)
                if (orden[i].HoraInicio < orden[i - 1].HoraFin) return $"Las franjas del día {dia.Key} se solapan.";
        }
        return null;
    }

    /// <summary>Condiciones normalizadas de un registro: fechas, horas propias (Temporal/Única/Diaria) y franjas.</summary>
    public sealed record Condiciones(string Modalidad, DateOnly FechaInicio, DateOnly? FechaFin, TimeOnly? HoraInicio, TimeOnly? HoraFin, IReadOnlyList<Franja> Franjas);

    /// <summary>
    /// Valida el tipo de permanencia. Única/Diaria/Temporal reutilizan <see cref="ReglasAutorizacion.Normalizar"/> y no
    /// llevan franjas por día (su franja es hora de inicio/fin). Recurrente exige franjas propias o el horario base y algún
    /// día dentro del periodo. Permanente no tiene fecha final; las franjas son opcionales (sin franjas = cualquier hora).
    /// </summary>
    public static (Condiciones? Condiciones, string? Error) Normalizar(
        string modalidad, DateOnly? fechaInicio, DateOnly? fechaFin, TimeOnly? horaInicio, TimeOnly? horaFin,
        IReadOnlyList<Franja> franjas, bool usaHorarioBase, DateTime ahoraLocal)
    {
        if (modalidad is ReglasAutorizacion.Unica or ReglasAutorizacion.Diaria or ReglasAutorizacion.Temporal or ReglasAutorizacion.Permanente)
        {
            if (franjas.Count > 0 && modalidad != ReglasAutorizacion.Permanente)
                return (null, "Este tipo de permanencia usa hora de inicio y fin, no franjas por día.");
            var (c, error) = ReglasAutorizacion.Normalizar(new ConecsIA.Application.DTOs.AutorizacionAccesoEntrada
            {
                Modalidad = modalidad, FechaInicio = fechaInicio, FechaFin = fechaFin,
                HoraInicio = modalidad == ReglasAutorizacion.Permanente ? null : horaInicio,
                HoraFin = modalidad == ReglasAutorizacion.Permanente ? null : horaFin,
            }, ahoraLocal);
            if (error is not null) return (null, error);
            return (new(c!.Modalidad, c.FechaInicio, c.FechaFin, c.HoraInicio, c.HoraFin, franjas), null);
        }
        if (modalidad != ReglasAutorizacion.Recurrente) return (null, "El tipo de permanencia no es válido.");
        if (fechaInicio is not DateOnly inicio) return (null, "Indique la fecha de inicio.");
        var hoy = DateOnly.FromDateTime(ahoraLocal);
        if (fechaFin is DateOnly fin)
        {
            if (fin < inicio) return (null, "La fecha final del periodo no puede ser anterior a la inicial.");
            if (fin < hoy) return (null, "El periodo ya terminó: la fecha final es anterior a hoy.");
        }
        if (franjas.Count == 0 && !usaHorarioBase) return (null, "Indique las franjas de ingreso por día o use el horario base.");
        if (franjas.Count > 0 && fechaFin is DateOnly f)
        {
            var dias = franjas.Select(x => x.DiaSemana).ToHashSet();
            if (!Enumerable.Range(0, Math.Min(7, f.DayNumber - inicio.DayNumber + 1)).Any(i => dias.Contains(ReglasAutorizacion.DiaIso(inicio.AddDays(i)))))
                return (null, "Ningún día con franja cae dentro del periodo.");
        }
        return (new(modalidad, inicio, fechaFin, null, null, franjas), null);
    }

    /// <summary>Horario efectivo del registro: propio; si no hay y usa horario base, el de la sede o el de la organización.</summary>
    public static IReadOnlyList<Franja> HorarioEfectivo(Autorizacion a, IEnumerable<Franja> propias, IEnumerable<HorarioBase> baseOrgYSede)
    {
        var lista = propias.ToList();
        if (lista.Count > 0 || !a.UsaHorarioBase) return lista;
        var bases = baseOrgYSede.ToList();
        var deSede = bases.Where(x => x.IdSede == a.IdSede).ToList();
        return (deSede.Count > 0 ? deSede : bases.Where(x => x.IdSede == null))
            .Select(x => new Franja(x.DiaSemana, x.HoraInicio, x.HoraFin)).ToList();
    }

    /// <summary>Resultado de evaluar un intento de ingreso: motivos que bloquean siempre y motivos de horario (bloquean o alertan).</summary>
    public sealed record Evaluacion(List<string> Bloqueos, List<string> Horario)
    {
        public bool Habilita => Bloqueos.Count == 0 && Horario.Count == 0;
    }

    /// <summary>Evalúa el registro en <paramref name="local"/> (hora local de la sede). No consulta la base.</summary>
    public static Evaluacion Evaluar(Autorizacion a, IReadOnlyList<Franja> horario, DateTime local)
    {
        var bloqueos = new List<string>();
        var fueraHorario = new List<string>();
        if (a.Estado == Pendiente) bloqueos.Add("El registro de acceso está pendiente de aprobación.");
        else if (a.Estado != Aprobada) bloqueos.Add($"El registro de acceso está {a.Estado.ToLowerInvariant()} y no habilita ingresos.");

        var hoy = DateOnly.FromDateTime(local);
        if (a.FechaInicio > hoy) bloqueos.Add($"El acceso empieza a regir el {a.FechaInicio:yyyy-MM-dd}.");
        if (a.FechaFin is DateOnly fin && fin < hoy) bloqueos.Add($"El acceso venció el {fin:yyyy-MM-dd}.");
        if (ReglasVisita.CupoEfectivo(a) is int cupo && a.IngresosConsumidos >= cupo)
            bloqueos.Add(cupo == 1 ? "El acceso de ingreso único ya fue utilizado." : "El acceso agotó su cupo de ingresos.");

        // Temporal: los instantes de inicio y fin son vigencia; Única/Diaria: su franja del día es horario.
        var franjaPropia = ReglasAutorizacion.MotivosFranja(a, local);
        if (a.Modalidad == ReglasAutorizacion.Temporal) bloqueos.AddRange(franjaPropia);
        else fueraHorario.AddRange(franjaPropia);

        if (horario.Count > 0)
        {
            var dia = ReglasVisita.DiaIso(local);
            var ahora = TimeOnly.FromDateTime(local);
            var delDia = horario.Where(h => h.DiaSemana == dia).ToList();
            if (delDia.Count == 0) fueraHorario.Add("El acceso no habilita ingresos este día de la semana.");
            else if (!delDia.Any(h => ahora >= h.HoraInicio && ahora < h.HoraFin))
                fueraHorario.Add($"Fuera del horario autorizado ({string.Join(", ", delDia.Select(h => $"{h.HoraInicio:HH\\:mm}–{h.HoraFin:HH\\:mm}"))}).");
        }
        return new(bloqueos, fueraHorario);
    }

    /// <summary>Vigencia mostrada en listados y detalle, con el mismo cálculo que la entrada.</summary>
    public static string Vigencia(Autorizacion a, IReadOnlyList<Franja> horario, DateTime local)
    {
        if (a.Estado != Aprobada) return "NoAplica";
        if (ReglasVisita.CupoEfectivo(a) is int cupo && a.IngresosConsumidos >= cupo) return "Consumida";
        if (ReglasAutorizacion.Vencio(a, local)) return "Vencida";
        var hoy = DateOnly.FromDateTime(local);
        if (a.FechaInicio > hoy) return "Programada";
        var evaluacion = Evaluar(a, horario, local);
        // Estado, fechas y cupo ya se evaluaron: el único bloqueo posible es una Temporal que aún no empieza su hora.
        if (evaluacion.Bloqueos.Count > 0) return "Programada";
        return evaluacion.Horario.Count > 0 ? "FueraDeHorario" : "Vigente";
    }

    /// <summary>Transiciones permitidas del registro: aprobar/rechazar desde Pendiente; revocar desde Aprobada; cancelar desde ambas.</summary>
    public static string? ValidarTransicion(string estado, string accion) => accion switch
    {
        "Aprobar" or "Rechazar" when estado != Pendiente => $"Solo se deciden registros pendientes; este está {estado.ToLowerInvariant()}.",
        "Revocar" when estado != Aprobada => $"Solo se revocan registros aprobados; este está {estado.ToLowerInvariant()}.",
        "Cancelar" when estado is not (Pendiente or Aprobada) => $"No se puede cancelar un registro {estado.ToLowerInvariant()}.",
        _ => null,
    };

    public static string EstadoDestino(string accion) => accion switch
    {
        "Aprobar" => Aprobada, "Rechazar" => Rechazada, "Revocar" => Revocada, "Cancelar" => Cancelada,
        _ => throw new ArgumentOutOfRangeException(nameof(accion)),
    };

    public static bool MotivoObligatorio(string accion) => accion is "Rechazar" or "Revocar" or "Cancelar";

    /// <summary>
    /// Copia a la organización las categorías de su tipo que falten, más VISITANTE y RECURRENTE si su tipo no las trae
    /// (la persiste quien llama). Misma regla que la migración RegistroAccesoUnificado.
    /// </summary>
    public static async Task SembrarCategoriasAsync(ConecsiaDbContext db, int idOrganizacion, int idTipoOrganizacion, CancellationToken ct)
    {
        var existentes = await db.CategoriasAcceso.IgnoreQueryFilters().Where(x => x.IdOrganizacion == idOrganizacion).Select(x => x.Codigo).ToListAsync(ct);
        var plantillas = await db.PlantillasCategoriaAcceso.AsNoTracking().Where(x => x.IdTipoOrganizacion == idTipoOrganizacion && x.Activo).ToListAsync(ct);
        var nuevas = plantillas.Select(p => new CategoriaAcceso
        {
            Codigo = p.Codigo, Nombre = p.Nombre, Descripcion = p.Descripcion, FlujoAprobacion = p.FlujoAprobacion, ModalidadesPermitidas = p.ModalidadesPermitidas,
            ExigeAnfitrion = p.ExigeAnfitrion, PermiteVehiculos = p.PermiteVehiculos, PermiteDispositivos = p.PermiteDispositivos, Orden = p.Orden,
        }).ToList();
        nuevas.Add(new CategoriaAcceso { Codigo = "VISITANTE", Nombre = "Visitante", FlujoAprobacion = FlujoAnfitrion, ModalidadesPermitidas = ["Unica", "Diaria", "Temporal"], ExigeAnfitrion = true, Orden = 90 });
        nuevas.Add(new CategoriaAcceso { Codigo = "RECURRENTE", Nombre = "Acceso recurrente", FlujoAprobacion = FlujoSeguridad, ModalidadesPermitidas = ["Recurrente", "Permanente"], ExigeAnfitrion = true, Orden = 91 });
        foreach (var c in nuevas.DistinctBy(x => x.Codigo).Where(x => !existentes.Contains(x.Codigo)))
        {
            c.IdOrganizacion = idOrganizacion;
            c.Activo = true;
            c.CreadoEn = DateTime.UtcNow;
            db.CategoriasAcceso.Add(c);
        }
    }
}
