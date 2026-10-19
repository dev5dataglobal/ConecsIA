import { useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { guardarOrganizacionSeleccionada } from './organizacionSeleccionada'
import { useMembresias } from './useContexto'

/** Pantalla para elegir entre varias membresías (HU-007). El servidor verifica la elección. */
export function ElegirOrganizacion() {
  const t = useT()
  const queryClient = useQueryClient()
  const { data: membresias, error, isPending } = useMembresias(true)

  function elegir(organizacionId: string) {
    guardarOrganizacionSeleccionada(organizacionId)
    void queryClient.invalidateQueries()
  }

  return (
    <section className="mx-auto max-w-md space-y-4 py-10">
      <h1 className="text-xl font-semibold">{t('organizacion.elegirTitulo')}</h1>
      <p className="text-sm text-tinta-suave">{t('organizacion.elegirAviso')}</p>
      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
      {isPending && <p className="text-sm text-tinta-suave">{t('organizacion.cargando')}</p>}
      <ul className="space-y-2">
        {membresias?.map((m) => (
          <li key={m.organizacionId}>
            <button
              type="button"
              onClick={() => elegir(m.organizacionId)}
              className="w-full rounded-lg border border-linea bg-fondo px-4 py-3 text-left text-sm font-semibold hover:border-marca focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-acento"
            >
              {m.nombre}
            </button>
          </li>
        ))}
      </ul>
    </section>
  )
}
