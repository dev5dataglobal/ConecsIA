import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

export type Modo = 'Entrada' | 'Salida' | 'Ambos'

export interface Zona {
  id: string
  sedeId: string
  nombre: string
  descripcion: string | null
  esPuntoAcceso: boolean
  modo: Modo | null
  estado: 'Activa' | 'Inactiva'
  version: number
}

export function useZonas(sedeId: string, habilitado = true) {
  return useQuery({
    queryKey: ['zonas', sedeId],
    queryFn: () => apiFetch<Zona[]>(`/api/v1/sedes/${sedeId}/zonas`),
    enabled: habilitado,
    retry: false,
  })
}
