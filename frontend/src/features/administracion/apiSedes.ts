import { apiFetch } from '../../shared/api/http'

/*
 * Sedes y áreas de la organización activa. El servidor toma la organización del token
 * (o la seleccionada por el administrador de plataforma); el cliente nunca la envía.
 */

export interface CiudadOpcion { idCiudad: number; nombre: string; departamento: string | null; pais: string; zonaHoraria: string; activo: boolean }

export interface SedeAdmin {
  idSede: number
  idCiudad: number
  idZonaEmpresarial: number | null
  nombre: string
  direccion: string | null
  telefono: string | null
  correo: string | null
  zonaHoraria: string
  activo: boolean
}

export interface AreaAdmin {
  idArea: number
  idSede: number
  idAreaPadre: number | null
  nombre: string
  descripcion: string | null
  esComunZonaEmpresarial: boolean
  activo: boolean
}

export type SedeEntrada = Pick<SedeAdmin, 'idCiudad' | 'idZonaEmpresarial' | 'nombre' | 'direccion' | 'telefono' | 'correo'>
export type AreaEntrada = Pick<AreaAdmin, 'idSede' | 'idAreaPadre' | 'nombre' | 'descripcion' | 'esComunZonaEmpresarial'>

const json = (method: string, data: unknown): RequestInit => ({ method, body: JSON.stringify(data) })

export const consultarCiudadesSede = () => apiFetch<CiudadOpcion[]>('/api/sedes/ciudades')
export const consultarSedes = () => apiFetch<SedeAdmin[]>('/api/sedes')
export const crearSede = (data: SedeEntrada) => apiFetch<SedeAdmin>('/api/sedes', json('POST', data))
export const editarSede = (id: number, data: SedeEntrada) => apiFetch<SedeAdmin>(`/api/sedes/${id}`, json('PUT', data))
export const cambiarEstadoSede = (id: number, activo: boolean) =>
  apiFetch<SedeAdmin>(`/api/sedes/${id}/${activo ? 'activar' : 'inactivar'}`, { method: 'POST' })

export const consultarAreas = () => apiFetch<AreaAdmin[]>('/api/areas')
export const crearArea = (data: AreaEntrada) => apiFetch<AreaAdmin>('/api/areas', json('POST', data))
export const editarArea = (id: number, data: AreaEntrada) => apiFetch<AreaAdmin>(`/api/areas/${id}`, json('PUT', data))
export const cambiarEstadoArea = (id: number, activo: boolean) =>
  apiFetch<AreaAdmin>(`/api/areas/${id}/${activo ? 'activar' : 'inactivar'}`, { method: 'POST' })
