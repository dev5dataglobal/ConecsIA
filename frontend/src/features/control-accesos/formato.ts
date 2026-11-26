import { ApiError } from '../../shared/api/problemDetails'
import { mensajeError } from '../administracion/utilidades'
import { DIAS, type EstadoAcceso, type Franja, type RegistroAcceso, type VigenciaAcceso } from './apiControlAccesos'

/** Instante en la zona horaria de la sede (los instantes viajan en UTC). */
export function enSede(iso: string, zona: string, opciones: Intl.DateTimeFormatOptions = { dateStyle: 'medium', timeStyle: 'short' }) {
  try { return new Date(iso).toLocaleString('es-CO', { ...opciones, timeZone: zona }) } catch { return new Date(iso).toLocaleString('es-CO', opciones) }
}
export const fecha = (iso: string) => new Date(`${iso}T00:00:00`).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' })
export const instante = (iso: string) => new Date(iso).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' })
export const hora = (valor: string | null) => valor?.slice(0, 5) ?? ''
export const hoyIso = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}
export function duracion(segundos: number) {
  const s = Math.max(0, Math.floor(segundos))
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  return h > 0 ? `${h} h ${m} min` : `${m} min`
}

/** Motivos de una denegación (lista del servidor) o el mensaje general. */
export function motivosError(error: unknown): string[] {
  if (error instanceof ApiError && error.problem.motivos?.length) return error.problem.motivos
  return [mensajeError(error)]
}

export const TEXTO_ESTADO: Record<EstadoAcceso, string> = {
  Pendiente: 'Pendiente de aprobación', Aprobada: 'Aprobado', Rechazada: 'Rechazado', Cancelada: 'Cancelado', Revocada: 'Revocado', Vencida: 'Vencido', Agotada: 'Agotado',
}
export const TEXTO_VIGENCIA: Record<VigenciaAcceso, string> = {
  Vigente: 'Vigente', Programada: 'Programado', FueraDeHorario: 'Fuera de horario', Vencida: 'Vencido', Consumida: 'Utilizado', NoAplica: '',
}

/** Franjas agrupadas por día: «L 07:00–12:00, 13:00–17:00 · M …». */
export function textoHorario(franjas: Franja[]) {
  return DIAS.filter((d) => franjas.some((f) => f.diaSemana === d.valor))
    .map((d) => `${d.corto} ${franjas.filter((f) => f.diaSemana === d.valor).map((f) => `${hora(f.horaInicio)}–${hora(f.horaFin)}`).join(', ')}`)
    .join(' · ')
}

/** Resumen legible de las condiciones según el tipo de permanencia. */
export function condiciones(a: Pick<RegistroAcceso, 'modalidad' | 'fechaInicio' | 'fechaFin' | 'horaInicio' | 'horaFin' | 'horario' | 'usaHorarioBase'>) {
  const franja = a.horaInicio ? ` · ${hora(a.horaInicio)} a ${hora(a.horaFin)}` : ''
  const horario = a.horario.length ? ` · ${textoHorario(a.horario)}` : a.usaHorarioBase ? ' · horario base (sin definir)' : ''
  switch (a.modalidad) {
    case 'Unica':
    case 'Diaria':
      return `${fecha(a.fechaInicio)}${franja || horario || ' · todo el día'}`
    case 'Temporal':
      return `${fecha(a.fechaInicio)} ${hora(a.horaInicio)} → ${a.fechaFin ? fecha(a.fechaFin) : ''} ${hora(a.horaFin)}`
    case 'Recurrente':
      return `Desde ${fecha(a.fechaInicio)}${a.fechaFin ? ` hasta ${fecha(a.fechaFin)}` : ''}${horario}`
    default:
      return `Desde ${fecha(a.fechaInicio)}, sin fecha final${horario || ' · cualquier hora'}`
  }
}
