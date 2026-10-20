import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { apiFetch } from '../../shared/api/http'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Dialogo } from '../../shared/ui/Dialogo'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import type { Organizacion } from './apiSaas'

export interface EstadoModulo {
  modulo: string
  nombre: string
  habilitado: boolean
  cambiadoEn: string | null
  cambiadoPor: string | null
}

/** HU-011: habilitar o deshabilitar módulos opcionales. Deshabilitar no borra datos. */
export function DialogoModulos({
  organizacion,
  onCerrar,
}: {
  organizacion: Organizacion
  onCerrar: () => void
}) {
  const t = useT()
  const ruta = `/api/v1/saas/organizaciones/${organizacion.id}/modulos`
  const queryClient = useQueryClient()
  const { data: modulos } = useQuery({
    queryKey: ['saas', 'modulos', organizacion.id],
    queryFn: () => apiFetch<EstadoModulo[]>(ruta),
    retry: false,
  })
  const [elegidos, setElegidos] = useState<Set<string> | null>(null)
  const actuales =
    elegidos ?? new Set((modulos ?? []).filter((m) => m.habilitado).map((m) => m.modulo))
  const guardar = useMutation({
    mutationFn: () =>
      apiFetch<EstadoModulo[]>(ruta, {
        method: 'PUT',
        body: JSON.stringify({ habilitados: [...actuales] }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saas', 'modulos'] }),
  })

  function alternar(modulo: string) {
    const nuevos = new Set(actuales)
    if (nuevos.has(modulo)) nuevos.delete(modulo)
    else nuevos.add(modulo)
    setElegidos(nuevos)
  }

  return (
    <Dialogo titulo={t('saas.modulos.titulo', { nombre: organizacion.nombre })} onCerrar={onCerrar}>
      <p className="text-sm text-tinta-suave">{t('saas.modulos.aviso')}</p>
      {guardar.error instanceof ApiError && <ProblemDetailsAlert problem={guardar.error.problem} />}
      <ul className="space-y-2">
        {modulos?.map((m) => (
          <li key={m.modulo}>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={actuales.has(m.modulo)}
                onChange={() => alternar(m.modulo)}
              />
              {m.nombre}
            </label>
          </li>
        ))}
      </ul>
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onCerrar}
          className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
        >
          {t('saas.form.cancelar')}
        </button>
        <button
          type="button"
          disabled={guardar.isPending || !modulos}
          onClick={() => guardar.mutate(undefined, { onSuccess: onCerrar })}
          className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura disabled:opacity-60"
        >
          {t('saas.form.guardar')}
        </button>
      </div>
    </Dialogo>
  )
}
