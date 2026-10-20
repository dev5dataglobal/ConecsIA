import { useState } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Dialogo } from '../../shared/ui/Dialogo'
import { Icono } from '../../shared/ui/Icono'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { usePlataforma } from '../sesion/useContexto'
import {
  useAplicarPlantilla,
  useCambiarEstadoOrganizacion,
  useOrganizacionesSaas,
  useTiposOrganizacion,
  type Organizacion,
} from './apiSaas'
import { DialogoModulos } from './DialogoModulos'
import { FormularioOrganizacion } from './FormularioOrganizacion'

type Edicion = { modo: 'crear' } | { modo: 'editar'; organizacion: Organizacion } | null
type CambioEstado = { organizacion: Organizacion; accion: 'inactivar' | 'reactivar' } | null

/**
 * Consola SaaS de organizaciones (HU-009). El servidor exige permisos del plano SaaS en cada
 * operación; esta pantalla no da acceso a los datos de los clientes.
 */
export function OrganizacionesSaasPage() {
  const t = useT()
  const [pagina, setPagina] = useState(1)
  const [edicion, setEdicion] = useState<Edicion>(null)
  const [cambio, setCambio] = useState<CambioEstado>(null)
  const [modulosDe, setModulosDe] = useState<Organizacion | null>(null)
  const { data: plataforma } = usePlataforma()
  const { data, error, isPending } = useOrganizacionesSaas(pagina)
  const { data: tipos } = useTiposOrganizacion()
  const cambiarEstado = useCambiarEstadoOrganizacion()
  const aplicarPlantilla = useAplicarPlantilla()
  const puedeAdministrar =
    plataforma?.permisosSaas?.includes('saas.organizaciones.administrar') ?? false
  const paginas = data ? Math.max(1, Math.ceil(data.total / 20)) : 1
  const nombreTipo = (clave: string) => tipos?.find((tipo) => tipo.clave === clave)?.nombre ?? clave

  return (
    <section className="space-y-5">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t('saas.titulo')}</h1>
          <p className="mt-1 text-sm text-tinta-suave">{t('saas.descripcion')}</p>
        </div>
        {puedeAdministrar && (
          <button
            type="button"
            onClick={() => setEdicion({ modo: 'crear' })}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura"
          >
            <Icono nombre="mas" className="size-4" />
            {t('saas.nueva')}
          </button>
        )}
      </header>

      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
      {aplicarPlantilla.error instanceof ApiError && (
        <ProblemDetailsAlert problem={aplicarPlantilla.error.problem} />
      )}
      {cambiarEstado.error instanceof ApiError && (
        <ProblemDetailsAlert problem={cambiarEstado.error.problem} />
      )}
      {isPending && <p className="text-sm text-tinta-suave">{t('saas.cargando')}</p>}

      {data && (
        <div className="overflow-x-auto rounded-xl border border-linea bg-fondo">
          <table className="min-w-full text-sm">
            <caption className="sr-only">{t('saas.tabla')}</caption>
            <thead className="text-left text-[11px] font-semibold uppercase tracking-wide text-tinta-suave">
              <tr className="border-b border-linea">
                <th scope="col" className="px-4 py-3">
                  {t('saas.col.nombre')}
                </th>
                <th scope="col" className="px-4 py-3">
                  {t('saas.col.tipo')}
                </th>
                <th scope="col" className="px-4 py-3">
                  {t('saas.col.estado')}
                </th>
                <th scope="col" className="px-4 py-3 text-right">
                  <span className="sr-only">{t('saas.col.acciones')}</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-linea">
              {data.elementos.map((org) => (
                <tr key={org.id}>
                  <th scope="row" className="px-4 py-3 text-left font-semibold">
                    {org.nombre}
                  </th>
                  <td className="px-4 py-3 text-tinta-suave">{nombreTipo(org.tipo)}</td>
                  <td className="px-4 py-3">{t(`saas.estado.${org.estado}`)}</td>
                  <td className="px-4 py-3">
                    {puedeAdministrar && (
                      <div className="flex justify-end gap-1">
                        <button
                          type="button"
                          aria-label={t('saas.editar', { nombre: org.nombre })}
                          onClick={() => setEdicion({ modo: 'editar', organizacion: org })}
                          className="rounded-lg p-1.5 text-tinta-suave hover:bg-marca-tenue hover:text-marca"
                        >
                          <Icono nombre="editar" className="size-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => setModulosDe(org)}
                          className="rounded-lg px-2 py-1 text-xs font-medium text-tinta-suave hover:bg-superficie"
                        >
                          {t('saas.modulos')}
                        </button>
                        <button
                          type="button"
                          onClick={() => aplicarPlantilla.mutate(org.id)}
                          className="rounded-lg px-2 py-1 text-xs font-medium text-tinta-suave hover:bg-superficie"
                        >
                          {t('saas.plantilla')}
                        </button>
                        <button
                          type="button"
                          onClick={() =>
                            setCambio({
                              organizacion: org,
                              accion: org.estado === 'Activa' ? 'inactivar' : 'reactivar',
                            })
                          }
                          className="rounded-lg px-2 py-1 text-xs font-medium text-tinta-suave hover:bg-superficie"
                        >
                          {org.estado === 'Activa' ? t('saas.inactivar') : t('saas.reactivar')}
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <nav
            aria-label={t('saas.tabla')}
            className="flex items-center justify-between gap-2 border-t border-linea px-4 py-3 text-sm"
          >
            <span className="text-xs text-tinta-suave">
              {t('sedes.paginacion', { pagina, paginas, total: data.total })}
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={pagina <= 1}
                onClick={() => setPagina((p) => p - 1)}
                className="rounded-lg border border-linea px-3 py-1.5 disabled:opacity-50"
              >
                {t('sedes.anterior')}
              </button>
              <button
                type="button"
                disabled={pagina >= paginas}
                onClick={() => setPagina((p) => p + 1)}
                className="rounded-lg border border-linea px-3 py-1.5 disabled:opacity-50"
              >
                {t('sedes.siguiente')}
              </button>
            </div>
          </nav>
        </div>
      )}

      {edicion && (
        <FormularioOrganizacion
          key={edicion.modo === 'editar' ? edicion.organizacion.id : 'nueva'}
          organizacion={edicion.modo === 'editar' ? edicion.organizacion : null}
          onCerrar={() => setEdicion(null)}
        />
      )}

      {modulosDe && <DialogoModulos organizacion={modulosDe} onCerrar={() => setModulosDe(null)} />}

      {cambio && (
        <Dialogo
          titulo={
            cambio.accion === 'inactivar' ? t('saas.inactivar.titulo') : t('saas.reactivar.titulo')
          }
          onCerrar={() => setCambio(null)}
        >
          <p className="text-sm">
            {cambio.accion === 'inactivar'
              ? t('saas.inactivar.texto', { nombre: cambio.organizacion.nombre })
              : t('saas.reactivar.texto', { nombre: cambio.organizacion.nombre })}
          </p>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => setCambio(null)}
              className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
            >
              {t('saas.form.cancelar')}
            </button>
            <button
              type="button"
              disabled={cambiarEstado.isPending}
              onClick={() => cambiarEstado.mutate(cambio, { onSettled: () => setCambio(null) })}
              className="rounded-lg bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-800 disabled:opacity-60"
            >
              {t('saas.confirmar')}
            </button>
          </div>
        </Dialogo>
      )}
    </section>
  )
}
