import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

export interface Area {
  id: string
  sedeId: string
  nombre: string
  tipo: string | null
  estado: 'Activa' | 'Inactiva'
  version: number
  responsables: string[]
  autorizadores: string[]
}

export type Funcion = 'responsables' | 'autorizadores'

export function useAreas(sedeId: string) {
  return useQuery({
    queryKey: ['areas', sedeId],
    queryFn: () => apiFetch<Area[]>(`/api/v1/sedes/${sedeId}/areas`),
    retry: false,
  })
}

function useInvalidar() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: ['areas'] })
}

export function useCrearArea(sedeId: string) {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (datos: { nombre: string; tipo: string }) =>
      apiFetch<Area>(`/api/v1/sedes/${sedeId}/areas`, {
        method: 'POST',
        body: JSON.stringify(datos),
      }),
    onSuccess: invalidar,
  })
}

export function useInactivarArea() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (area: Area) =>
      apiFetch<Area>(`/api/v1/areas/${area.id}/inactivar`, { method: 'POST' }),
    onSuccess: invalidar,
  })
}

export function useAsignar(area: Area, funcion: Funcion) {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (usuarios: string[]) =>
      apiFetch<Area>(`/api/v1/areas/${area.id}/${funcion}`, {
        method: 'PUT',
        body: JSON.stringify({ usuarios }),
      }),
    onSuccess: invalidar,
  })
}
