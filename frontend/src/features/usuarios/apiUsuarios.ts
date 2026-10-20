import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

export interface UsuarioOrganizacion {
  usuarioId: string
  nombre: string
  correo: string
  estado: 'Pendiente' | 'Activo' | 'Inactivo'
  roles: string[]
  alcanceTotalSedes: boolean
  sedes: string[]
}

export interface DatosInvitacion {
  correo: string
  nombre: string
  roles: string[]
  alcanceTotalSedes: boolean
  sedes: string[]
}

export interface Alcance {
  alcanceTotalSedes: boolean
  sedes: string[]
}

const RUTA = '/api/v1/usuarios'

export function useUsuarios() {
  return useQuery({
    queryKey: ['usuarios'],
    queryFn: () => apiFetch<UsuarioOrganizacion[]>(RUTA),
    retry: false,
  })
}

function useInvalidar() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: ['usuarios'] })
}

export function useInvitar() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (datos: DatosInvitacion) =>
      apiFetch<{ usuario: UsuarioOrganizacion; enlaceAceptacion: string | null }>(
        `${RUTA}/invitaciones`,
        { method: 'POST', body: JSON.stringify(datos) },
      ),
    onSuccess: invalidar,
  })
}

export function useAsignarRoles(usuarioId: string) {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (roles: string[]) =>
      apiFetch<UsuarioOrganizacion>(`${RUTA}/${usuarioId}/roles`, {
        method: 'PUT',
        body: JSON.stringify({ roles }),
      }),
    onSuccess: invalidar,
  })
}

export function useAsignarAlcance(usuarioId: string) {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (alcance: Alcance) =>
      apiFetch<UsuarioOrganizacion>(`${RUTA}/${usuarioId}/alcance`, {
        method: 'PUT',
        body: JSON.stringify(alcance),
      }),
    onSuccess: invalidar,
  })
}

export function useCambiarEstadoUsuario() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: ({ usuarioId, accion }: { usuarioId: string; accion: 'inactivar' | 'reactivar' }) =>
      apiFetch<UsuarioOrganizacion>(`${RUTA}/${usuarioId}/${accion}`, { method: 'POST' }),
    onSuccess: invalidar,
  })
}

export function aceptarInvitacion(token: string, contrasena: string) {
  // El token va en el cuerpo: así no queda en trazas ni en registros de rutas (ADR-018).
  return apiFetch<void>('/api/v1/invitaciones/aceptar', {
    method: 'POST',
    body: JSON.stringify({ token, contrasena }),
  })
}
