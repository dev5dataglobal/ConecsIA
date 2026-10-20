import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../shared/api/http'
import type { Vocabulario } from '../../shared/i18n/traductor'

export interface Termino {
  singular: string
  plural: string
}

export interface VocabularioOrganizacion {
  claves: string[]
  terminos: Record<string, Termino>
  version: number
}

export function useVocabularioOrganizacion(habilitado: boolean) {
  return useQuery({
    queryKey: ['vocabulario'],
    queryFn: () => apiFetch<VocabularioOrganizacion>('/api/v1/vocabulario'),
    enabled: habilitado,
    retry: false,
  })
}

export function useGuardarVocabulario() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (datos: { terminos: Record<string, Termino>; version: number }) =>
      apiFetch<VocabularioOrganizacion>('/api/v1/vocabulario', {
        method: 'PUT',
        body: JSON.stringify(datos),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['vocabulario'] }),
  })
}

/**
 * HU-012: traduce el vocabulario de la organización a claves de texto de la interfaz. Solo cambia
 * etiquetas visibles; rutas, permisos y contratos de la API siguen con los nombres del núcleo (RN-11).
 */
export function aTextos(vocabulario: VocabularioOrganizacion | undefined): Vocabulario {
  const terminos = vocabulario?.terminos ?? {}
  const textos: Vocabulario = {}
  if (terminos.sede) {
    textos['nav.sedes'] = terminos.sede.plural
    textos['sedes.titulo'] = terminos.sede.plural
    textos['contexto.sede'] = terminos.sede.singular
  }
  if (terminos.organizacion) textos['contexto.organizacion'] = terminos.organizacion.singular
  if (terminos.area) textos['areas.titulo'] = terminos.area.plural
  return textos
}
