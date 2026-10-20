import { useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { tienePermiso, useContexto } from '../sesion/useContexto'
import {
  useGuardarVocabulario,
  useVocabularioOrganizacion,
  type Termino,
  type VocabularioOrganizacion,
} from './vocabulario'

function Formulario({ vocabulario }: { vocabulario: VocabularioOrganizacion }) {
  const t = useT()
  const { data: contexto } = useContexto()
  const guardar = useGuardarVocabulario()
  const [terminos, setTerminos] = useState<Record<string, Termino>>(vocabulario.terminos)
  const puedeModificar = tienePermiso(contexto, 'configuracion.modificar')

  function cambiar(clave: string, campo: keyof Termino, valor: string) {
    const actual = terminos[clave] ?? { singular: '', plural: '' }
    setTerminos({ ...terminos, [clave]: { ...actual, [campo]: valor } })
  }

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    // Un término vacío vuelve a la etiqueta por defecto.
    const llenos = Object.fromEntries(
      Object.entries(terminos).filter(([, v]) => v.singular.trim() && v.plural.trim()),
    )
    guardar.mutate({ terminos: llenos, version: vocabulario.version })
  }

  return (
    <form onSubmit={enviar} className="space-y-4">
      {guardar.error instanceof ApiError && <ProblemDetailsAlert problem={guardar.error.problem} />}
      {guardar.isSuccess && <p className="text-sm text-tinta-suave">{t('vocabulario.guardado')}</p>}
      <table className="min-w-full text-sm">
        <caption className="sr-only">{t('vocabulario.tabla')}</caption>
        <thead className="text-left text-[11px] font-semibold uppercase tracking-wide text-tinta-suave">
          <tr>
            <th scope="col" className="py-2">
              {t('vocabulario.col.clave')}
            </th>
            <th scope="col" className="py-2">
              {t('vocabulario.col.singular')}
            </th>
            <th scope="col" className="py-2">
              {t('vocabulario.col.plural')}
            </th>
          </tr>
        </thead>
        <tbody>
          {vocabulario.claves.map((clave) => (
            <tr key={clave}>
              <th scope="row" className="py-1 pr-2 text-left font-mono text-xs">
                {clave}
              </th>
              <td className="py-1 pr-2">
                <input
                  aria-label={t('vocabulario.singularDe', { clave })}
                  maxLength={60}
                  disabled={!puedeModificar}
                  value={terminos[clave]?.singular ?? ''}
                  onChange={(e) => cambiar(clave, 'singular', e.target.value)}
                  className="w-full rounded-lg border border-linea px-2 py-1"
                />
              </td>
              <td className="py-1">
                <input
                  aria-label={t('vocabulario.pluralDe', { clave })}
                  maxLength={60}
                  disabled={!puedeModificar}
                  value={terminos[clave]?.plural ?? ''}
                  onChange={(e) => cambiar(clave, 'plural', e.target.value)}
                  className="w-full rounded-lg border border-linea px-2 py-1"
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {puedeModificar && (
        <button
          type="submit"
          disabled={guardar.isPending}
          className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {t('vocabulario.guardar')}
        </button>
      )}
    </form>
  )
}

/** HU-012: vocabulario de la organización. Las etiquetas definitivas por vertical dependen de D-01. */
export function VocabularioPage() {
  const t = useT()
  const { data, error } = useVocabularioOrganizacion(true)
  return (
    <section className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight">{t('vocabulario.titulo')}</h1>
      <p className="text-sm text-tinta-suave">{t('vocabulario.descripcion')}</p>
      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
      {data && <Formulario key={data.version} vocabulario={data} />}
    </section>
  )
}
