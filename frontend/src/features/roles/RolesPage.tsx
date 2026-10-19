import { useState } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Dialogo } from '../../shared/ui/Dialogo'
import { Icono } from '../../shared/ui/Icono'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { tienePermiso, useContexto } from '../sesion/useContexto'
import { useInactivarRol, useRoles, type Rol } from './apiRoles'
import { FormularioRol } from './FormularioRol'

type Edicion = { modo: 'crear' } | { modo: 'editar'; rol: Rol } | null

/**
 * HU-017 Roles configurables. Las acciones se muestran según los permisos que informa el
 * servidor solo por usabilidad: la API verifica permisos, escalamiento y organización.
 */
export function RolesPage() {
  const t = useT()
  const { data: contexto } = useContexto()
  const { data: roles, error, isPending } = useRoles()
  const inactivar = useInactivarRol()
  const [edicion, setEdicion] = useState<Edicion>(null)
  const [porInactivar, setPorInactivar] = useState<Rol | null>(null)
  const puedeAdministrar = tienePermiso(contexto, 'roles.administrar')

  return (
    <section className="space-y-5">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t('roles.titulo')}</h1>
          <p className="mt-1 text-sm text-tinta-suave">{t('roles.descripcion')}</p>
        </div>
        {puedeAdministrar && (
          <button
            type="button"
            onClick={() => setEdicion({ modo: 'crear' })}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura"
          >
            <Icono nombre="mas" className="size-4" />
            {t('roles.nuevo')}
          </button>
        )}
      </header>

      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
      {inactivar.error instanceof ApiError && (
        <ProblemDetailsAlert problem={inactivar.error.problem} />
      )}
      {isPending && <p className="text-sm text-tinta-suave">{t('roles.cargando')}</p>}

      {roles && (
        <div className="overflow-x-auto rounded-xl border border-linea bg-fondo">
          <table className="min-w-full text-sm">
            <caption className="sr-only">{t('roles.tabla')}</caption>
            <thead className="text-left text-[11px] font-semibold uppercase tracking-wide text-tinta-suave">
              <tr className="border-b border-linea">
                <th scope="col" className="px-4 py-3">
                  {t('roles.col.nombre')}
                </th>
                <th scope="col" className="px-4 py-3">
                  {t('roles.col.permisos')}
                </th>
                <th scope="col" className="px-4 py-3">
                  {t('roles.col.estado')}
                </th>
                <th scope="col" className="px-4 py-3 text-right">
                  <span className="sr-only">{t('roles.col.acciones')}</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-linea">
              {roles.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-10 text-center text-tinta-suave">
                    {t('roles.vacio')}
                  </td>
                </tr>
              )}
              {roles.map((rol) => (
                <tr key={rol.id}>
                  <th scope="row" className="px-4 py-3 text-left font-normal">
                    <span className="block font-semibold">{rol.nombre}</span>
                    {rol.descripcion && (
                      <span className="block text-xs text-tinta-suave">{rol.descripcion}</span>
                    )}
                  </th>
                  <td className="px-4 py-3 text-tinta-suave">
                    {t('roles.cantidadPermisos', { cantidad: rol.permisos.length })}
                  </td>
                  <td className="px-4 py-3">{t(`roles.estado.${rol.estado}`)}</td>
                  <td className="px-4 py-3">
                    {puedeAdministrar && (
                      <div className="flex justify-end gap-1">
                        <button
                          type="button"
                          aria-label={t('roles.editar', { nombre: rol.nombre })}
                          onClick={() => setEdicion({ modo: 'editar', rol })}
                          className="rounded-lg p-1.5 text-tinta-suave hover:bg-marca-tenue hover:text-marca"
                        >
                          <Icono nombre="editar" className="size-4" />
                        </button>
                        {rol.estado === 'Activo' && (
                          <button
                            type="button"
                            aria-label={t('roles.inactivar', { nombre: rol.nombre })}
                            onClick={() => setPorInactivar(rol)}
                            className="rounded-lg p-1.5 text-tinta-suave hover:bg-red-50 hover:text-red-700"
                          >
                            <Icono nombre="apagar" className="size-4" />
                          </button>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {edicion && (
        <FormularioRol
          key={edicion.modo === 'editar' ? edicion.rol.id : 'nuevo'}
          rol={edicion.modo === 'editar' ? edicion.rol : null}
          onCerrar={() => setEdicion(null)}
        />
      )}

      {porInactivar && (
        <Dialogo titulo={t('roles.inactivar.titulo')} onCerrar={() => setPorInactivar(null)}>
          <p className="text-sm">{t('roles.inactivar.texto', { nombre: porInactivar.nombre })}</p>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => setPorInactivar(null)}
              className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
            >
              {t('roles.form.cancelar')}
            </button>
            <button
              type="button"
              disabled={inactivar.isPending}
              onClick={() =>
                inactivar.mutate(porInactivar, { onSettled: () => setPorInactivar(null) })
              }
              className="rounded-lg bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-800 disabled:opacity-60"
            >
              {t('roles.inactivar.confirmar')}
            </button>
          </div>
        </Dialogo>
      )}
    </section>
  )
}
