import { esClaveTexto, useT } from '../i18n/traductor'
import type { ProblemDetails } from '../api/problemDetails'

/**
 * Presenta un Problem Details sin exponer detalles internos.
 * Los 500 muestran solo la referencia (traceId) para soporte (HU-004).
 */
export function ProblemDetailsAlert({ problem }: { problem: ProblemDetails }) {
  const t = useT()
  const esErrorServidor = problem.status === undefined || problem.status >= 500
  const titulo =
    problem.title && esClaveTexto(problem.title)
      ? t(problem.title)
      : (problem.title ?? t('error.titulo'))
  const tieneCampos = problem.errors && Object.keys(problem.errors).length > 0

  return (
    <div
      role="alert"
      className="rounded-md border border-red-300 bg-red-50 p-4 text-sm text-red-900"
    >
      <p className="font-semibold">
        {esErrorServidor && problem.status !== 0 ? t('error.titulo') : titulo}
      </p>
      {!esErrorServidor && problem.detail && <p className="mt-1">{problem.detail}</p>}
      {tieneCampos && <p className="mt-1">{t('error.revisarCampos')}</p>}
      {problem.status === 0 && <p className="mt-1">{t('error.sinConexion')}</p>}
      {problem.traceId && (
        <p className="mt-2 font-mono text-xs text-red-800">
          {t('error.referencia', { traceId: problem.traceId })}
        </p>
      )}
    </div>
  )
}
