import { useState } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import type { ClaveTexto } from '../../shared/i18n/es'
import { useT } from '../../shared/i18n/traductor'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { tienePermiso, useContexto } from '../sesion/useContexto'
import {
  TAMANO_PAGINA,
  useInactivarSede,
  useSedes,
  useTotalSedes,
  type EstadoSede,
  type Sede,
} from './apiSedes'
import { Dialogo } from '../../shared/ui/Dialogo'
import { FormularioSede } from './FormularioSede'

type Edicion = { modo: 'crear' } | { modo: 'editar'; sede: Sede } | null

const kpis: { estado: EstadoSede | ''; clave: ClaveTexto; icono: NombreIcono }[] = [
  { estado: '', clave: 'sedes.kpi.total', icono: 'sede' },
  { estado: 'Activa', clave: 'sedes.kpi.activas', icono: 'activas' },
  { estado: 'Inactiva', clave: 'sedes.kpi.inactivas', icono: 'inactivas' },
]

function Kpi({ estado, clave, icono }: (typeof kpis)[number]) {
  const t = useT()
  const { data } = useTotalSedes(estado)
  return (
    <li className="flex items-center gap-3 rounded-xl border border-linea bg-fondo p-4">
      <span className="grid size-10 shrink-0 place-items-center rounded-lg bg-marca-tenue text-marca">
        <Icono nombre={icono} />
      </span>
      <span className="leading-tight">
        <span className="block text-xl font-semibold">{data ?? '—'}</span>
        <span className="block text-xs text-tinta-suave">{t(clave)}</span>
      </span>
    </li>
  )
}

/**
 * HU-013 con el estilo del mockup de Figma. Los botones se muestran según los permisos que
 * informa el servidor solo por usabilidad: la API verifica permiso, alcance y organización.
 */
export function SedesPage() {
  const t = useT()
  const { data: contexto } = useContexto()
  const [estado, setEstado] = useState<EstadoSede | ''>('')
  const [pagina, setPagina] = useState(1)
  const [edicion, setEdicion] = useState<Edicion>(null)
  const [porInactivar, setPorInactivar] = useState<Sede | null>(null)
  const { data, error, isPending } = useSedes(estado, pagina)
  const inactivar = useInactivarSede()

  const puedeCrear = tienePermiso(contexto, 'sedes.crear') && (contexto?.alcanceTotalSedes ?? false)
  const puedeModificar = tienePermiso(contexto, 'sedes.modificar')
  const puedeInactivar = tienePermiso(contexto, 'sedes.inactivar')
  const paginas = data ? Math.max(1, Math.ceil(data.total / TAMANO_PAGINA)) : 1

  const botonSecundario =
    'inline-flex items-center gap-1.5 rounded-lg border border-linea bg-fondo px-3 py-1.5 text-sm font-medium hover:bg-superficie disabled:opacity-50'

  return (
    <section className="space-y-5">
      <nav aria-label={t('nav.migas')} className="text-xs text-tinta-suave">
        <ol className="flex items-center gap-2">
          <li>
            <Link to="/" className="hover:text-tinta hover:underline">
              {t('nav.inicio')}
            </Link>
          </li>
          <li aria-hidden="true">›</li>
          <li aria-current="page" className="font-medium text-tinta">
            {t('sedes.titulo')}
          </li>
        </ol>
      </nav>

      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t('sedes.titulo')}</h1>
          <p className="mt-1 text-sm text-tinta-suave">{t('sedes.descripcion')}</p>
        </div>
        {puedeCrear && (
          <button
            type="button"
            onClick={() => setEdicion({ modo: 'crear' })}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-marca"
          >
            <Icono nombre="mas" className="size-4" />
            {t('sedes.nueva')}
          </button>
        )}
      </header>

      <ul aria-label={t('sedes.kpi.resumen')} className="grid gap-3 sm:grid-cols-3">
        {kpis.map((kpi) => (
          <Kpi key={kpi.clave} {...kpi} />
        ))}
      </ul>

      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
      {inactivar.error instanceof ApiError && (
        <ProblemDetailsAlert problem={inactivar.error.problem} />
      )}

      <div className="overflow-hidden rounded-xl border border-linea bg-fondo">
        <div className="flex flex-wrap items-center gap-2 border-b border-linea p-3">
          <label htmlFor="filtro-estado" className="text-sm font-medium">
            {t('sedes.filtro')}
          </label>
          <select
            id="filtro-estado"
            value={estado}
            onChange={(e) => {
              setEstado(e.target.value as EstadoSede | '')
              setPagina(1)
            }}
            className="rounded-lg border border-linea bg-fondo px-3 py-1.5 text-sm"
          >
            <option value="">{t('sedes.filtro.todas')}</option>
            <option value="Activa">{t('sedes.filtro.activas')}</option>
            <option value="Inactiva">{t('sedes.filtro.inactivas')}</option>
          </select>
        </div>

        {isPending && <p className="p-4 text-sm text-tinta-suave">{t('sedes.cargando')}</p>}

        {data && (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <caption className="sr-only">{t('sedes.tabla')}</caption>
              <thead className="text-left text-[11px] font-semibold uppercase tracking-wide text-tinta-suave">
                <tr className="border-b border-linea">
                  <th scope="col" className="px-4 py-3">
                    {t('sedes.col.nombre')}
                  </th>
                  <th scope="col" className="hidden px-4 py-3 sm:table-cell">
                    {t('sedes.col.ciudad')}
                  </th>
                  <th scope="col" className="px-4 py-3">
                    {t('sedes.col.zona')}
                  </th>
                  <th scope="col" className="px-4 py-3">
                    {t('sedes.col.estado')}
                  </th>
                  <th scope="col" className="px-4 py-3 text-right">
                    <span className="sr-only">{t('sedes.col.acciones')}</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-linea">
                {data.elementos.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-10 text-center text-tinta-suave">
                      {t('sedes.vacio')}
                    </td>
                  </tr>
                )}
                {data.elementos.map((sede) => (
                  <tr key={sede.id} className="hover:bg-superficie/50">
                    <th scope="row" className="px-4 py-3 text-left font-normal">
                      <span className="flex items-center gap-3">
                        <span
                          aria-hidden="true"
                          className="grid size-8 shrink-0 place-items-center rounded-full bg-marca-tenue text-marca"
                        >
                          <Icono nombre="sede" className="size-4" />
                        </span>
                        <span className="min-w-0">
                          <Link
                            to={`/sedes/${sede.id}/areas`}
                            className="block font-semibold hover:underline"
                            aria-label={t('areas.ver', { nombre: sede.nombre })}
                          >
                            {sede.nombre}
                          </Link>
                          {sede.direccion && (
                            <span className="block text-xs text-tinta-suave">{sede.direccion}</span>
                          )}
                        </span>
                      </span>
                    </th>
                    <td className="hidden px-4 py-3 text-tinta-suave sm:table-cell">
                      {[sede.ciudad, sede.pais].filter(Boolean).join(', ')}
                    </td>
                    <td className="px-4 py-3">
                      <span className="rounded-md border border-linea px-2 py-0.5 font-mono text-xs">
                        {sede.zonaHoraria}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      {sede.estado === 'Activa' ? (
                        <span className="inline-flex items-center gap-1 rounded-full bg-acento/60 px-2.5 py-0.5 text-xs font-medium text-tinta">
                          <Icono nombre="check" className="size-3.5" />
                          {t('sedes.estado.Activa')}
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 rounded-full border border-gris px-2.5 py-0.5 text-xs font-medium text-tinta-suave">
                          {t('sedes.estado.Inactiva')}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-1">
                        {puedeModificar && (
                          <button
                            type="button"
                            aria-label={t('sedes.editar', { nombre: sede.nombre })}
                            title={t('sedes.editar', { nombre: sede.nombre })}
                            onClick={() => setEdicion({ modo: 'editar', sede })}
                            className="rounded-lg p-1.5 text-tinta-suave hover:bg-marca-tenue hover:text-marca"
                          >
                            <Icono nombre="editar" className="size-4" />
                          </button>
                        )}
                        {puedeInactivar && sede.estado === 'Activa' && (
                          <button
                            type="button"
                            aria-label={t('sedes.inactivar', { nombre: sede.nombre })}
                            title={t('sedes.inactivar', { nombre: sede.nombre })}
                            onClick={() => setPorInactivar(sede)}
                            className="rounded-lg p-1.5 text-tinta-suave hover:bg-red-50 hover:text-red-700"
                          >
                            <Icono nombre="apagar" className="size-4" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {data && data.total > 0 && (
          <nav
            className="flex flex-wrap items-center justify-between gap-2 border-t border-linea px-4 py-3 text-sm"
            aria-label={t('sedes.tabla')}
          >
            <span className="text-xs text-tinta-suave">
              {t('sedes.paginacion', { pagina, paginas, total: data.total })}
            </span>
            <div className="flex items-center gap-2">
              <button
                type="button"
                disabled={pagina <= 1}
                onClick={() => setPagina((p) => p - 1)}
                className={botonSecundario}
              >
                {t('sedes.anterior')}
              </button>
              <span
                aria-hidden="true"
                className="grid size-8 place-items-center rounded-lg bg-marca text-xs font-semibold text-white"
              >
                {pagina}
              </span>
              <button
                type="button"
                disabled={pagina >= paginas}
                onClick={() => setPagina((p) => p + 1)}
                className={botonSecundario}
              >
                {t('sedes.siguiente')}
              </button>
            </div>
          </nav>
        )}
      </div>

      {edicion && (
        <FormularioSede
          key={edicion.modo === 'editar' ? edicion.sede.id : 'nueva'}
          sede={edicion.modo === 'editar' ? edicion.sede : null}
          onCerrar={() => setEdicion(null)}
        />
      )}

      {porInactivar && (
        <Dialogo titulo={t('sedes.inactivar.titulo')} onCerrar={() => setPorInactivar(null)}>
          <p className="text-sm">{t('sedes.inactivar.texto', { nombre: porInactivar.nombre })}</p>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => setPorInactivar(null)}
              className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
            >
              {t('sedes.form.cancelar')}
            </button>
            <button
              type="button"
              disabled={inactivar.isPending}
              onClick={() =>
                inactivar.mutate(porInactivar, { onSettled: () => setPorInactivar(null) })
              }
              className="rounded-lg bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-800 disabled:opacity-60"
            >
              {t('sedes.inactivar.confirmar')}
            </button>
          </div>
        </Dialogo>
      )}
    </section>
  )
}
