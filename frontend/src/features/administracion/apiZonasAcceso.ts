import { apiFetch } from '../../shared/api/http'

/*
 * Zonas y puntos de acceso de la organización activa (RF-05): Sede → Zona → Punto de acceso.
 * El servidor toma la organización del token (o la seleccionada por el administrador de plataforma); el cliente nunca la envía.
 */

export type ModoAcceso = 'Entrada' | 'Salida' | 'Ambos'

interface Trazabilidad {
  creadoEn: string
  creadoPor: string | null
  actualizadoEn: string | null
  actualizadoPor: string | null
}

export interface ZonaAcceso extends Trazabilidad {
  idZona: number
  idSede: number
  sede: string
  sedeActiva: boolean
  nombre: string
  descripcion: string | null
  activo: boolean
  puntosAcceso: number
  /** Con puntos, autorizaciones o movimientos: no se elimina, solo se inactiva. */
  tieneHistorial: boolean
}

export interface PuntoAcceso extends Trazabilidad {
  idPuntoAcceso: number
  idSede: number
  sede: string
  idZona: number
  zona: string
  zonaActiva: boolean
  nombre: string
  descripcion: string | null
  modoAcceso: ModoAcceso
  activo: boolean
  /** Con movimientos u operadores asignados: no se elimina, solo se inactiva. */
  tieneHistorial: boolean
}

export interface ZonaEntrada { idSede: number; nombre: string; descripcion: string | null }
export interface PuntoAccesoEntrada { idZona: number; nombre: string; descripcion: string | null; modoAcceso: ModoAcceso }

const json = (method: string, data: unknown): RequestInit => ({ method, body: JSON.stringify(data) })

export const consultarZonasAcceso = () => apiFetch<ZonaAcceso[]>('/api/zonas')
export const crearZonaAcceso = (data: ZonaEntrada) => apiFetch<ZonaAcceso>('/api/zonas', json('POST', data))
export const editarZonaAcceso = (id: number, data: ZonaEntrada) => apiFetch<ZonaAcceso>(`/api/zonas/${id}`, json('PUT', data))
export const cambiarEstadoZonaAcceso = (id: number, activo: boolean) =>
  apiFetch<ZonaAcceso>(`/api/zonas/${id}/${activo ? 'activar' : 'inactivar'}`, { method: 'POST' })
export const eliminarZonaAcceso = (id: number) => apiFetch<void>(`/api/zonas/${id}`, { method: 'DELETE' })

export const consultarPuntosAcceso = () => apiFetch<PuntoAcceso[]>('/api/puntos-acceso')
export const crearPuntoAcceso = (data: PuntoAccesoEntrada) => apiFetch<PuntoAcceso>('/api/puntos-acceso', json('POST', data))
export const editarPuntoAcceso = (id: number, data: PuntoAccesoEntrada) => apiFetch<PuntoAcceso>(`/api/puntos-acceso/${id}`, json('PUT', data))
export const cambiarEstadoPuntoAcceso = (id: number, activo: boolean) =>
  apiFetch<PuntoAcceso>(`/api/puntos-acceso/${id}/${activo ? 'activar' : 'inactivar'}`, { method: 'POST' })
export const eliminarPuntoAcceso = (id: number) => apiFetch<void>(`/api/puntos-acceso/${id}`, { method: 'DELETE' })
