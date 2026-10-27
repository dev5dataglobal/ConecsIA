import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

/**
 * Módulo del catálogo (app_modulos). `codigo` es el identificador estable; `ruta` null = sin pantalla aún.
 * `consultar`/`anadir`/`editar`: acciones concedidas (en /mios, las del usuario; en /roles/{id}/modulos, las del rol).
 */
export interface AppModulo {
  idAppModulo: number
  codigo: string
  nombre: string
  descripcion: string | null
  ruta: string | null
  icono: string | null
  orden: number
  activo: boolean
  consultar?: boolean
  anadir?: boolean
  editar?: boolean
}

export type Accion = 'consultar' | 'anadir' | 'editar'

export interface AsignacionModulo {
  idAppModulo: number
  consultar: boolean
  anadir: boolean
  editar: boolean
}

export const CLAVE_MIS_MODULOS = ['mis-modulos'] as const

export const consultarMisModulos = () => apiFetch<AppModulo[]>('/api/app-modulos/mios')
export const consultarCatalogoModulos = () => apiFetch<AppModulo[]>('/api/app-modulos')
export const consultarModulosDeRol = (idRol: number) => apiFetch<AppModulo[]>(`/api/roles/${idRol}/modulos`)
export const reemplazarModulosDeRol = (idRol: number, asignaciones: AsignacionModulo[]) =>
  apiFetch<AppModulo[]>(`/api/roles/${idRol}/modulos`, { method: 'PUT', body: JSON.stringify({ asignaciones }) })

/**
 * Módulos que el servidor autoriza al usuario en su organización actual.
 * Solo sirve para presentar el menú y los estados de acceso: el backend valida cada solicitud.
 */
export function useMisModulos() {
  return useQuery({ queryKey: CLAVE_MIS_MODULOS, queryFn: consultarMisModulos, staleTime: 60_000 })
}

/** Acciones del usuario sobre un módulo (presentación; cada acción la valida también el servidor). */
export function useAcciones(codigo: string): Record<Accion, boolean> {
  const modulo = useMisModulos().data?.find((m) => m.codigo === codigo)
  return { consultar: Boolean(modulo?.consultar), anadir: Boolean(modulo?.anadir), editar: Boolean(modulo?.editar) }
}
