import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

export interface EstadoSalud {
  status: string
  checks?: { name: string; status: string }[]
}

/** Consulta periódica de disponibilidad. La UI muestra el estado; nunca simula éxito. */
export function useEstadoApi() {
  return useQuery({
    queryKey: ['sistema', 'salud'],
    queryFn: () => apiFetch<EstadoSalud>('/api/v1/health/ready'),
    refetchInterval: 30_000,
    retry: false,
  })
}
