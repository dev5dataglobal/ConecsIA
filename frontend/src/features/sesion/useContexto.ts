import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'
import { leerOrganizacionSeleccionada } from './organizacionSeleccionada'
import { haySesion } from './sesion'
import { leerSesionDesarrollo } from './sesionDesarrollo'

export interface ContextoUsuario {
  usuario: { id: string; nombre: string; correo: string }
  organizacion: { id: string; nombre: string }
  permisos: string[]
  alcanceTotalSedes: boolean
  sedesAsignadas: string[]
}

export interface Membresia {
  organizacionId: string
  nombre: string
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
    queryKey: ['me', leerSesionDesarrollo() ?? 'sesion', leerOrganizacionSeleccionada()],
    queryFn: () => apiFetch<ContextoUsuario>('/api/v1/me'),
    enabled: haySesion(),
    // Al cambiar de organización se conserva la cabecera mientras llega el contexto nuevo.
    placeholderData: keepPreviousData,
    retry: false,
  })
}

/** Membresías vigentes del usuario; responde aunque no haya organización seleccionada. */
export function useMembresias(habilitado = haySesion()) {
  return useQuery({
    queryKey: ['me', 'membresias', leerSesionDesarrollo() ?? 'sesion'],
    queryFn: () => apiFetch<Membresia[]>('/api/v1/me/membresias'),
    enabled: habilitado,
    retry: false,
  })
}

/** Códigos de la API que indican que hay que (volver a) elegir organización. */
export const CODIGOS_ELEGIR_ORGANIZACION = [
  'ORGANIZACION_NO_SELECCIONADA',
  'ORGANIZACION_NO_PERMITIDA',
]

export function tienePermiso(contexto: ContextoUsuario | undefined, permiso: string) {
  return contexto?.permisos.includes(permiso) ?? false
}
