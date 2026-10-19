import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'
import { haySesion } from './sesion'
import { leerSesionDesarrollo } from './sesionDesarrollo'

export interface ContextoUsuario {
  usuario: { id: string; nombre: string; correo: string }
  organizacion: { id: string; nombre: string }
  permisos: string[]
  alcanceTotalSedes: boolean
  sedesAsignadas: string[]
}

export interface UsuarioDesarrollo {
  id: string
  nombre: string
  correo: string
  organizaciones: string[]
}

/**
 * Contexto derivado por el servidor (organización, permisos, alcance). La UI lo usa solo para
 * presentar y ocultar acciones; el backend vuelve a verificar todo en cada operación.
 */
export function useContexto() {
  return useQuery({
    queryKey: ['me', leerSesionDesarrollo() ?? 'sesion'],
    queryFn: () => apiFetch<ContextoUsuario>('/api/v1/me'),
    enabled: haySesion(),
    retry: false,
  })
}

export function tienePermiso(contexto: ContextoUsuario | undefined, permiso: string) {
  return contexto?.permisos.includes(permiso) ?? false
}
