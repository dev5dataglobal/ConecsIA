import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

export interface Rol {
  id: string
  nombre: string
  descripcion: string | null
  permisos: string[]
  plantilla: string | null
  estado: 'Activo' | 'Inactivo'
  version: number
  creadoEn: string
  actualizadoEn: string
  inactivadoEn: string | null
}

export interface DefinicionPermiso {
  clave: string
  recurso: string
  accion: string
  descripcion: string
}

export interface PlantillaRol {
  clave: string
  nombre: string
  descripcion: string
  permisos: string[]
}

export interface DatosRol {
  nombre: string
  descripcion: string
  permisos: string[]
  plantilla: string | null
}

export function useRoles() {
  return useQuery({
    queryKey: ['roles'],
    queryFn: () => apiFetch<Rol[]>('/api/v1/roles'),
    retry: false,
  })
}

export function useCatalogoPermisos() {
  return useQuery({
    queryKey: ['permisos'],
    queryFn: () => apiFetch<DefinicionPermiso[]>('/api/v1/permisos'),
    staleTime: Infinity,
    retry: false,
  })
}

export function usePlantillasRol() {
  return useQuery({
    queryKey: ['roles', 'plantillas'],
    queryFn: () => apiFetch<PlantillaRol[]>('/api/v1/roles/plantillas'),
    staleTime: Infinity,
    retry: false,
  })
}

/** Al cambiar roles cambian los permisos efectivos: se recarga también el contexto (/me). */
function useInvalidarRoles() {
  const queryClient = useQueryClient()
  return () =>
    queryClient.invalidateQueries({
      predicate: (q) => ['roles', 'me'].includes(String(q.queryKey[0])),
    })
}

export function useGuardarRol(rol: Rol | null) {
  const invalidar = useInvalidarRoles()
  return useMutation({
    mutationFn: (datos: DatosRol) =>
      rol
        ? apiFetch<Rol>(`/api/v1/roles/${rol.id}`, {
            method: 'PUT',
            body: JSON.stringify({
              nombre: datos.nombre,
              descripcion: datos.descripcion,
              permisos: datos.permisos,
              version: rol.version,
            }),
          })
        : apiFetch<Rol>('/api/v1/roles', { method: 'POST', body: JSON.stringify(datos) }),
    onSuccess: invalidar,
  })
}

export function useInactivarRol() {
  const invalidar = useInvalidarRoles()
  return useMutation({
    mutationFn: (rol: Rol) =>
      apiFetch<Rol>(`/api/v1/roles/${rol.id}/inactivar`, { method: 'POST' }),
    onSuccess: invalidar,
  })
}

/** Agrupa el catálogo por recurso, en el orden en que lo envía el servidor. */
export function agruparPorRecurso(catalogo: DefinicionPermiso[]) {
  const grupos = new Map<string, DefinicionPermiso[]>()
  for (const permiso of catalogo) {
    grupos.set(permiso.recurso, [...(grupos.get(permiso.recurso) ?? []), permiso])
  }
  return [...grupos.entries()]
}
