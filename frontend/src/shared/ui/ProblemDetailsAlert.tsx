import { esClaveTexto, useT } from '../i18n/traductor'
import type { ProblemDetails } from '../api/problemDetails'

/** Presenta los errores de la API con mensajes comprensibles para la persona usuaria. */
export function ProblemDetailsAlert({ problem }: { problem: ProblemDetails }) {
  const t = useT()
  const esErrorServidor = problem.status === undefined || problem.status >= 500
  const titulo =
    problem.title && esClaveTexto(problem.title)
      ? t(problem.title)
      : (problem.title ?? t('error.titulo'))
  const tieneCampos = problem.errors && Object.keys(problem.errors).length > 0
  const mensajeEstado =
    problem.status === 403
      ? 'No tienes permisos para realizar esta acción.'
      : undefined
  const mostrarReferencia = Boolean(problem.traceId) && esErrorServidor

  return (
    <div
      role="alert"
      className="rounded-md border border-red-300 bg-red-50 p-4 text-sm text-red-900"
    >
      <p className="font-semibold">
        {esErrorServidor && problem.status !== 0 ? t('error.titulo') : titulo}
      </p>
      {!esErrorServidor && problem.detail && <p className="mt-1">{problem.detail}</p>}
      {mensajeEstado && <p className="mt-1">{mensajeEstado}</p>}
      {tieneCampos && <p className="mt-1">{t('error.revisarCampos')}</p>}
      {problem.status === 0 && <p className="mt-1">{t('error.sinConexion')}</p>}
      {mostrarReferencia && (
        <p className="mt-2 font-mono text-xs text-red-800">
          {t('error.referencia', { traceId: problem.traceId ?? '' })}
        </p>
      )}
    </div>
  )
}
