import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

/** Módulo del catálogo (app_modulos). `codigo` es el identificador estable; `ruta` null = sin pantalla aún. */
export interface AppModulo {
  idAppModulo: number
  codigo: string
  nombre: string
  descripcion: string | null
  ruta: string | null
  icono: string | null
  orden: number
  activo: boolean
}

export const CLAVE_MIS_MODULOS = ['mis-modulos'] as const

export const consultarMisModulos = () => apiFetch<AppModulo[]>('/api/app-modulos/mios')
export const consultarCatalogoModulos = () => apiFetch<AppModulo[]>('/api/app-modulos')
export const consultarModulosDeRol = (idRol: number) => apiFetch<AppModulo[]>(`/api/roles/${idRol}/modulos`)
export const reemplazarModulosDeRol = (idRol: number, idsModulos: number[]) =>
  apiFetch<AppModulo[]>(`/api/roles/${idRol}/modulos`, { method: 'PUT', body: JSON.stringify({ idsModulos }) })

/**
 * Módulos que el servidor autoriza al usuario en su organización actual.
 * Solo sirve para presentar el menú y los estados de acceso: el backend valida cada solicitud.
 */
export function useMisModulos() {
  return useQuery({ queryKey: CLAVE_MIS_MODULOS, queryFn: consultarMisModulos, staleTime: 60_000 })
}
