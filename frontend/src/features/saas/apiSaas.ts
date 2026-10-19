import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'

export interface Organizacion {
  id: string
  nombre: string
  tipo: string
  identificacionLegal: string | null
  zonaHoraria: string | null
  contactoAdministrativo: string | null
  estado: 'Activa' | 'Inactiva'
  version: number
  creadaEn: string
  actualizadaEn: string
  inactivadaEn: string | null
}

export interface TipoOrganizacion {
  clave: string
  nombre: string
}

interface Pagina<T> {
  elementos: T[]
  total: number
  pagina: number
  tamano: number
}

export interface DatosOrganizacion {
  nombre: string
  tipo: string
  identificacionLegal: string
  zonaHoraria: string
  contactoAdministrativo: string
}

export interface DatosAlta extends DatosOrganizacion {
  administrador: { correo: string; nombre: string }
}

const RUTA = '/api/v1/saas/organizaciones'

export function useOrganizacionesSaas(pagina: number) {
  return useQuery({
    queryKey: ['saas', 'organizaciones', pagina],
    queryFn: () => apiFetch<Pagina<Organizacion>>(`${RUTA}?pagina=${pagina}&tamano=20`),
    retry: false,
  })
}

export function useTiposOrganizacion() {
  return useQuery({
    queryKey: ['saas', 'tipos'],
    queryFn: () => apiFetch<TipoOrganizacion[]>(`${RUTA}/tipos`),
    staleTime: Infinity,
    retry: false,
  })
}

function useInvalidar() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: ['saas'] })
}

export function useGuardarOrganizacion(organizacion: Organizacion | null) {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: (datos: DatosAlta | DatosOrganizacion) =>
      organizacion
        ? apiFetch<Organizacion>(`${RUTA}/${organizacion.id}`, {
            method: 'PUT',
            body: JSON.stringify({ ...datos, version: organizacion.version }),
          })
        : apiFetch<Organizacion>(RUTA, { method: 'POST', body: JSON.stringify(datos) }),
    onSuccess: invalidar,
  })
}

export function useCambiarEstadoOrganizacion() {
  const invalidar = useInvalidar()
  return useMutation({
    mutationFn: ({
      organizacion,
      accion,
    }: {
      organizacion: Organizacion
      accion: 'inactivar' | 'reactivar'
    }) => apiFetch<Organizacion>(`${RUTA}/${organizacion.id}/${accion}`, { method: 'POST' }),
    onSuccess: invalidar,
  })
}
