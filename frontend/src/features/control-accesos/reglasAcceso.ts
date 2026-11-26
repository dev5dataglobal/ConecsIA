import { DIAS, type CategoriaAcceso, type RegistroAccesoEntrada } from './apiControlAccesos'
import { hoyIso } from './formato'

/* Validación de cortesía del registro de acceso (el servidor vuelve a validar todo) y cuerpo enviado. */

export type DatosAcceso = Omit<RegistroAccesoEntrada, 'idPersona'>
export type ErroresAcceso = Partial<Record<'persona' | 'categoria' | 'sede' | 'area' | 'anfitrion' | 'modalidad' | 'fechaInicio' | 'fechaFin' | 'horas' | 'franjas' | 'motivo', string>>

/** Valida solo lo que aplica al tipo de permanencia elegido y a la categoría. */
export function validarRegistroAcceso(idPersona: number, d: DatosAcceso, categoria: CategoriaAcceso | undefined, permitePermanente: boolean, hoy = hoyIso()): ErroresAcceso {
  const e: ErroresAcceso = {}
  if (!idPersona) e.persona = 'Selecciona o registra la persona.'
  if (!categoria) e.categoria = 'Selecciona la categoría de acceso.'
  else if (!categoria.modalidadesPermitidas.includes(d.modalidad)) e.modalidad = `La categoría «${categoria.nombre}» no admite este tipo de permanencia.`
  if (!d.idSede) e.sede = 'Selecciona la sede.'
  if (!d.idArea) e.area = 'Selecciona el destino.'
  if (!d.idMembresiaResponsable) e.anfitrion = 'Selecciona el anfitrión o responsable.'
  if ((categoria?.exigeMotivo ?? true) && !d.motivo?.trim()) e.motivo = 'Indica el motivo del ingreso.'
  if (d.modalidad === 'Permanente' && !permitePermanente) e.modalidad = 'La política de la organización no permite accesos permanentes.'
  if (!d.fechaInicio) e.fechaInicio = 'Indica la fecha de inicio.'
  switch (d.modalidad) {
    case 'Unica':
    case 'Diaria':
      if (d.fechaInicio && d.fechaInicio < hoy) e.fechaInicio = 'El día no puede ser anterior a hoy.'
      if (Boolean(d.horaInicio) !== Boolean(d.horaFin)) e.horas = 'Indica ambas horas de la franja o ninguna.'
      else if (d.horaInicio && d.horaFin && d.horaFin <= d.horaInicio) e.horas = 'La hora de fin debe ser posterior a la de inicio.'
      break
    case 'Temporal':
      if (!d.horaInicio || !d.horaFin) e.horas = 'Indica la hora de inicio y la de finalización.'
      if (!d.fechaFin) e.fechaFin = 'Indica la fecha de finalización.'
      else if (d.fechaInicio && d.horaInicio && d.horaFin && `${d.fechaFin}T${d.horaFin}` <= `${d.fechaInicio}T${d.horaInicio}`) e.fechaFin = 'La finalización debe ser posterior al inicio.'
      break
    default:
      if (d.fechaFin && d.fechaInicio && d.fechaFin < d.fechaInicio) e.fechaFin = 'El fin del periodo no puede ser anterior al inicio.'
      if (d.modalidad === 'Recurrente' && !d.usaHorarioBase && d.franjas.length === 0) e.franjas = 'Agrega al menos una franja o usa el horario base.'
      if (!d.usaHorarioBase) {
        if (d.franjas.some((f) => !f.horaInicio || !f.horaFin || f.horaFin <= f.horaInicio)) e.franjas = 'En cada franja la hora de fin debe ser posterior a la de inicio.'
        else if (DIAS.some((dia) => {
          const delDia = d.franjas.filter((f) => f.diaSemana === dia.valor).sort((a, b) => a.horaInicio.localeCompare(b.horaInicio))
          return delDia.some((f, i) => i > 0 && f.horaInicio < delDia[i - 1]!.horaFin)
        })) e.franjas = 'Hay franjas que se solapan en el mismo día.'
      }
  }
  return e
}

/** Solo los campos del tipo elegido viajan al servidor. */
export function cuerpoAcceso(idPersona: number, d: DatosAcceso): RegistroAccesoEntrada {
  const base = { ...d, idPersona, motivo: d.motivo?.trim() || null, observacion: d.observacion?.trim() || null }
  const franjas = d.usaHorarioBase ? [] : d.franjas.map((f) => ({ ...f, horaInicio: segundos(f.horaInicio), horaFin: segundos(f.horaFin) }))
  switch (d.modalidad) {
    case 'Unica':
    case 'Diaria': return { ...base, fechaFin: null, franjas: [], horaInicio: opcional(d.horaInicio), horaFin: opcional(d.horaFin) }
    case 'Temporal': return { ...base, franjas: [], usaHorarioBase: false, horaInicio: opcional(d.horaInicio), horaFin: opcional(d.horaFin) }
    case 'Recurrente': return { ...base, horaInicio: null, horaFin: null, franjas }
    default: return { ...base, fechaFin: null, horaInicio: null, horaFin: null, franjas }
  }
}
const segundos = (h: string) => (h.length === 5 ? `${h}:00` : h)
const opcional = (h: string | null) => (h ? segundos(h) : null)

