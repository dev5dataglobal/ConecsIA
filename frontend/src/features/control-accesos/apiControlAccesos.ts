import { apiFetch } from '../../shared/api/http'

/*
 * Solicitudes de autorización de ingreso de personas (módulo Control de accesos). La organización la resuelve el
 * servidor; permisos, vigencia, cupo y validez del ingreso también: aquí solo se presentan.
 */

export type Modalidad = 'Unica' | 'Diaria' | 'Temporal' | 'Recurrente' | 'Permanente'
export type EstadoAutorizacionAcceso = 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Cancelada' | 'Revocada' | 'Vencida' | 'Agotada'
export type VigenciaAcceso = 'Vigente' | 'Programada' | 'Vencida' | 'Consumida' | 'NoAplica'

export const MODALIDADES: { valor: Modalidad; texto: string; descripcion: string }[] = [
  { valor: 'Unica', texto: 'Única', descripcion: 'Un solo ingreso en un día. Al registrarse la entrada queda consumida.' },
  { valor: 'Diaria', texto: 'Diaria', descripcion: 'Válida durante un día específico.' },
  { valor: 'Temporal', texto: 'Temporal', descripcion: 'Entre una fecha y hora de inicio y una de finalización.' },
  { valor: 'Recurrente', texto: 'Recurrente', descripcion: 'Días de la semana y horario que se repiten, con periodo opcional.' },
  { valor: 'Permanente', texto: 'Permanente', descripcion: 'Sin fecha final. Solo si la política de la organización lo permite.' },
]
export const textoModalidad = (m: string) => MODALIDADES.find((x) => x.valor === m)?.texto ?? m

/** Días ISO: 1 = lunes … 7 = domingo. */
export const DIAS: { valor: number; corto: string; texto: string }[] = [
  { valor: 1, corto: 'L', texto: 'Lunes' }, { valor: 2, corto: 'M', texto: 'Martes' }, { valor: 3, corto: 'X', texto: 'Miércoles' },
  { valor: 4, corto: 'J', texto: 'Jueves' }, { valor: 5, corto: 'V', texto: 'Viernes' }, { valor: 6, corto: 'S', texto: 'Sábado' }, { valor: 7, corto: 'D', texto: 'Domingo' },
]

export interface HorarioAcceso { diaSemana: number; horaInicio: string; horaFin: string }
export interface TransicionAcceso { idTransicion: number; estadoAnterior: string | null; estadoNuevo: string; motivo: string | null; actor: string | null; ocurridoEn: string }

export interface AutorizacionAcceso {
  idAutorizacion: number
  idPersona: number
  persona: string
  documentoPersona: string
  personaActiva: boolean
  idSede: number
  sede: string
  idArea: number
  area: string
  idMembresiaResponsable: number
  responsable: string | null
  modalidad: Modalidad
  fechaInicio: string
  fechaFin: string | null
  horaInicio: string | null
  horaFin: string | null
  cupoIngresos: number | null
  ingresosConsumidos: number
  motivo: string | null
  observacion: string | null
  estado: EstadoAutorizacionAcceso
  vigencia: VigenciaAcceso
  solicitadoPor: string | null
  solicitadoEn: string
  decididoPor: string | null
  decididoEn: string | null
  motivoDecision: string | null
  horarios: HorarioAcceso[]
  historial: TransicionAcceso[]
  zonaHoraria: string
}

export interface AutorizacionAccesoEntrada {
  idPersona: number
  idSede: number
  idArea: number
  idMembresiaResponsable: number
  modalidad: Modalidad
  fechaInicio: string
  fechaFin: string | null
  horaInicio: string | null
  horaFin: string | null
  diasSemana: number[]
  motivo: string
  observacion: string | null
}

export interface OpcionesAutorizacionAcceso {
  sedes: { id: number; nombre: string }[]
  areas: { id: number; idSede: number; nombre: string }[]
  responsables: { idMembresia: number; nombre: string; areas: number[] }[]
  permitePermanente: boolean
  idMembresiaPropia: number | null
}

export interface FiltrosAutorizacionAcceso {
  buscar?: string
  estado?: string
  modalidad?: string
  idSede?: string
  idPersona?: number
}

const json = (method: string, body: unknown): RequestInit => ({ method, body: JSON.stringify(body) })
const consulta = (params: Record<string, string | number | undefined>) =>
  new URLSearchParams(Object.entries(params).filter(([, v]) => v !== undefined && v !== '').map(([k, v]) => [k, String(v)])).toString()

export const consultarAutorizacionesAcceso = (filtros: FiltrosAutorizacionAcceso = {}) =>
  apiFetch<AutorizacionAcceso[]>(`/api/autorizaciones-acceso?${consulta({ ...filtros })}`)
export const consultarAutorizacionAcceso = (id: number) => apiFetch<AutorizacionAcceso>(`/api/autorizaciones-acceso/${id}`)
export const consultarOpcionesAcceso = () => apiFetch<OpcionesAutorizacionAcceso>('/api/autorizaciones-acceso/opciones')
export const solicitarAutorizacionAcceso = (data: AutorizacionAccesoEntrada) =>
  apiFetch<AutorizacionAcceso>('/api/autorizaciones-acceso', json('POST', data))
export const decidirAutorizacionAcceso = (id: number, accion: 'aprobar' | 'rechazar', motivo: string | null) =>
  apiFetch<AutorizacionAcceso>(`/api/autorizaciones-acceso/${id}/${accion}`, json('POST', { motivo }))
