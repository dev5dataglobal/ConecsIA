import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

export type EstadoSede = 'Activa' | 'Inactiva'

export interface Sede {
  id: string
  nombre: string
  direccion: string | null
  ciudad: string | null
  pais: string | null
  zonaHoraria: string
  estado: EstadoSede
  version: number
  creadaEn: string
  actualizadaEn: string
  inactivadaEn: string | null
}

export interface Pagina<T> {
  elementos: T[]
  total: number
  pagina: number
  tamano: number
}

export interface DatosSede {
  nombre: string
  direccion: string
  ciudad: string
  pais: string
  zonaHoraria: string
}

export const TAMANO_PAGINA = 20

export function useSedes(estado: EstadoSede | '', pagina: number) {
  const parametros = new URLSearchParams({ pagina: String(pagina), tamano: String(TAMANO_PAGINA) })
  if (estado) parametros.set('estado', estado)
  return useQuery({
    queryKey: ['sedes', estado, pagina],
    queryFn: () => apiFetch<Pagina<Sede>>(`/api/v1/sedes?${parametros}`),
    retry: false,
  })
}

/** Totales para las tarjetas de resumen: una consulta mínima por estado (el servidor aplica el alcance). */
export function useTotalSedes(estado: EstadoSede | '') {
  const parametros = new URLSearchParams({ pagina: '1', tamano: '1' })
  if (estado) parametros.set('estado', estado)
  return useQuery({
    queryKey: ['sedes', 'total', estado],
    queryFn: () => apiFetch<Pagina<Sede>>(`/api/v1/sedes?${parametros}`),
    select: (pagina) => pagina.total,
    retry: false,
  })
}

function useInvalidarSedes() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: ['sedes'] })
}

/** La organización nunca se envía: el servidor la deriva de la membresía (ADR-005). */
export function useGuardarSede(sede: Sede | null) {
  const invalidar = useInvalidarSedes()
  return useMutation({
    mutationFn: (datos: DatosSede) =>
      sede
        ? apiFetch<Sede>(`/api/v1/sedes/${sede.id}`, {
            method: 'PUT',
            body: JSON.stringify({ ...datos, version: sede.version }),
          })
        : apiFetch<Sede>('/api/v1/sedes', { method: 'POST', body: JSON.stringify(datos) }),
    onSuccess: invalidar,
  })
}

export function useInactivarSede() {
  const invalidar = useInvalidarSedes()
  return useMutation({
    mutationFn: (sede: Sede) =>
      apiFetch<Sede>(`/api/v1/sedes/${sede.id}/inactivar`, { method: 'POST' }),
    onSuccess: invalidar,
  })
}
