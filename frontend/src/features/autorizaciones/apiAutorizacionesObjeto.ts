import { apiFetch } from '../../shared/api/http'

/*
 * Autorizaciones de ingreso de vehículos y dispositivos. Ninguna petición envía organización, solicitante ni
 * aprobador: el servidor los toma del contexto y de la identidad autenticada. Aprobar un objeto no autoriza a la persona.
 */

export type TipoObjeto = 'Vehiculo' | 'Dispositivo'
export type EstadoAutorizacion = 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Revocada'
export type Vigencia = 'Vigente' | 'Programada' | 'Vencida' | 'NoAplica'

export interface TransicionAutorizacion {
  idTransicion: number
  estadoAnterior: EstadoAutorizacion | null
  estadoNuevo: EstadoAutorizacion
  motivo: string | null
  actor: string | null
  ocurridoEn: string
}

export interface AutorizacionObjeto {
  idAutorizacionObjeto: number
  tipoObjeto: TipoObjeto
  idVehiculo: number | null
  idDispositivo: number | null
  objeto: string | null
  objetoActivo: boolean
  idPersona: number
  persona: string | null
  documentoPersona: string | null
  idSede: number
  sede: string | null
  idArea: number | null
  area: string | null
  idAutorizacion: number | null
  fechaInicio: string
  fechaFin: string
  horaInicio: string | null
  horaFin: string | null
  motivo: string | null
  estado: EstadoAutorizacion
  vigencia: Vigencia
  motivoDecision: string | null
  solicitadoPor: string | null
  solicitadoEn: string
  decididoPor: string | null
  decididoEn: string | null
  historial: TransicionAutorizacion[]
}

export interface AutorizacionObjetoEntrada {
  idPersona: number
  tipoObjeto: TipoObjeto
  idVehiculo: number | null
  idDispositivo: number | null
  idSede: number
  idArea: number | null
  fechaInicio: string
  fechaFin: string
  horaInicio: string | null
  horaFin: string | null
  motivo: string | null
}

export interface OpcionesAutorizacion {
  sedes: { id: number; nombre: string }[]
  areas: { id: number; idSede: number; nombre: string }[]
}

export interface FiltrosAutorizacion {
  buscar?: string
  estado?: string
  tipoObjeto?: string
  idSede?: string
  desde?: string
  hasta?: string
}

const json = (method: string, body: unknown): RequestInit => ({ method, body: JSON.stringify(body) })
const consulta = (params: Record<string, string | number | undefined>) =>
  new URLSearchParams(Object.entries(params).filter(([, v]) => v !== undefined && v !== '').map(([k, v]) => [k, String(v)])).toString()

export const consultarAutorizacionesObjeto = (filtros: FiltrosAutorizacion = {}) =>
  apiFetch<AutorizacionObjeto[]>(`/api/autorizaciones-objeto?${consulta({ ...filtros })}`)
export const consultarAutorizacionObjeto = (id: number) => apiFetch<AutorizacionObjeto>(`/api/autorizaciones-objeto/${id}`)
export const consultarOpcionesAutorizacion = () => apiFetch<OpcionesAutorizacion>('/api/autorizaciones-objeto/opciones')
export const solicitarAutorizacionObjeto = (data: AutorizacionObjetoEntrada) =>
  apiFetch<AutorizacionObjeto>('/api/autorizaciones-objeto', json('POST', data))
export const decidirAutorizacionObjeto = (id: number, accion: 'aprobar' | 'rechazar' | 'revocar', motivo: string | null) =>
  apiFetch<AutorizacionObjeto>(`/api/autorizaciones-objeto/${id}/${accion}`, json('POST', { motivo }))
