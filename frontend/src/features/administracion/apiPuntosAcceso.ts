import { apiFetch } from '../../shared/api/http'

/*
 * Puntos de acceso de la organización activa (RF-05, ADR-028): Sede → Punto de acceso. Ya no hay zonas de acceso.
 * El servidor toma la organización del token (o la seleccionada por el administrador de plataforma); el cliente nunca la envía.
 */

export type ModoAcceso = 'Entrada' | 'Salida' | 'Ambos'

export interface PuntoAcceso {
  idPuntoAcceso: number
  idSede: number
  sede: string
  sedeActiva: boolean
  /** Zona empresarial de la sede (se resuelve desde la sede), si tiene. */
  zonaEmpresarial: string | null
  esComunZonaEmpresarial: boolean
  nombre: string
  descripcion: string | null
  modoAcceso: ModoAcceso
  activo: boolean
  /** Con movimientos u operadores asignados: no se elimina, solo se inactiva. */
  tieneHistorial: boolean
  creadoEn: string
  creadoPor: string | null
  actualizadoEn: string | null
  actualizadoPor: string | null
}

export interface PuntoAccesoEntrada {
  idSede: number
  nombre: string
  descripcion: string | null
  modoAcceso: ModoAcceso
  esComunZonaEmpresarial: boolean
}

const json = (method: string, data: unknown): RequestInit => ({ method, body: JSON.stringify(data) })

export const consultarPuntosAcceso = () => apiFetch<PuntoAcceso[]>('/api/puntos-acceso')
export const crearPuntoAcceso = (data: PuntoAccesoEntrada) => apiFetch<PuntoAcceso>('/api/puntos-acceso', json('POST', data))
export const editarPuntoAcceso = (id: number, data: PuntoAccesoEntrada) => apiFetch<PuntoAcceso>(`/api/puntos-acceso/${id}`, json('PUT', data))
export const cambiarEstadoPuntoAcceso = (id: number, activo: boolean) =>
  apiFetch<PuntoAcceso>(`/api/puntos-acceso/${id}/${activo ? 'activar' : 'inactivar'}`, { method: 'POST' })
export const eliminarPuntoAcceso = (id: number) => apiFetch<void>(`/api/puntos-acceso/${id}`, { method: 'DELETE' })
