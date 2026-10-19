import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { useEstadoApi } from '../sistema/useEstadoApi'

export function InicioPage() {
  const t = useT()
  const { data, error, isPending } = useEstadoApi()

  return (
    <section className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t('inicio.titulo')}</h1>
        <p className="mt-1 max-w-prose text-tinta-suave">{t('inicio.descripcion')}</p>
      </header>

      <div className="grid gap-4 sm:grid-cols-2">
        <article className="rounded-lg border border-linea bg-fondo p-4 ">
          <h2 className="text-sm font-medium text-tinta-suave">{t('inicio.estadoApi')}</h2>
          <p className="mt-2 text-lg font-semibold">
            {isPending ? t('conexion.verificando') : (data?.status ?? '—')}
          </p>
        </article>
        <article className="rounded-lg border border-linea bg-fondo p-4 ">
          <h2 className="text-sm font-medium text-tinta-suave">{t('inicio.estadoBase')}</h2>
          <p className="mt-2 text-lg font-semibold">
            {data?.checks?.find((c) => c.name === 'postgresql')?.status ?? '—'}
          </p>
        </article>
      </div>

      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
    </section>
  )
}
