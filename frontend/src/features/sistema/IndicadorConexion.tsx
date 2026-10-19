import { useT } from '../../shared/i18n/traductor'
import { useEstadoApi } from './useEstadoApi'

/** Estado de conexión visible y no dependiente solo del color (PRD §9.2). */
export function IndicadorConexion() {
  const t = useT()
  const { isPending, isError, dataUpdatedAt } = useEstadoApi()

  const estado = isPending ? 'verificando' : isError ? 'sinConexion' : 'enLinea'
  const texto =
    estado === 'verificando'
      ? t('conexion.verificando')
      : estado === 'sinConexion'
        ? t('conexion.sinConexion')
        : t('conexion.enLinea')
  const color =
    estado === 'enLinea'
      ? 'bg-emerald-500'
      : estado === 'sinConexion'
        ? 'bg-red-500'
        : 'bg-amber-400'
  const hora = dataUpdatedAt ? new Date(dataUpdatedAt).toLocaleTimeString() : null

  return (
    <div
      role="status"
      aria-live="polite"
      className="flex items-center gap-2 text-xs text-tinta-suave"
    >
      <span aria-hidden="true" className={`inline-block size-2.5 rounded-full ${color}`} />
      <span className="sr-only sm:not-sr-only">{texto}</span>
      {hora && estado === 'enLinea' && (
        <span className="hidden text-gris xl:inline">
          · {t('conexion.ultimaVerificacion', { hora })}
        </span>
      )}
    </div>
  )
}
