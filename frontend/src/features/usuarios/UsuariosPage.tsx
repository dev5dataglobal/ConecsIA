import { useState } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Icono } from '../../shared/ui/Icono'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { tienePermiso, useContexto } from '../sesion/useContexto'
import { useCambiarEstadoUsuario, useUsuarios, type UsuarioOrganizacion } from './apiUsuarios'
import { DialogoAccesos, DialogoInvitar } from './DialogosUsuario'

/**
 * HU-016 y HU-018: usuarios de la organización. Las acciones se ocultan sin permiso solo por
 * usabilidad; la API verifica permisos, alcance y anti-escalamiento.
 */
export function UsuariosPage() {
  const t = useT()
  const { data: contexto } = useContexto()
  const { data: usuarios, error, isPending } = useUsuarios()
  const cambiarEstado = useCambiarEstadoUsuario()
  const [invitando, setInvitando] = useState(false)
  const [accesosDe, setAccesosDe] = useState<UsuarioOrganizacion | null>(null)
  const puedeInvitar = tienePermiso(contexto, 'usuarios.invitar')
  const puedeAdministrar = tienePermiso(contexto, 'usuarios.administrar')

  return (
    <section className="space-y-5">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">{t('usuarios.titulo')}</h1>
          <p className="mt-1 text-sm text-tinta-suave">{t('usuarios.descripcion')}</p>
        </div>
        {puedeInvitar && (
          <button
            type="button"
            onClick={() => setInvitando(true)}
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura"
          >
            <Icono nombre="mas" className="size-4" />
            {t('usuarios.invitar')}
          </button>
        )}
      </header>

      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
      {cambiarEstado.error instanceof ApiError && (
        <ProblemDetailsAlert problem={cambiarEstado.error.problem} />
      )}
      {isPending && <p className="text-sm text-tinta-suave">{t('usuarios.cargando')}</p>}

      {usuarios && (
        <div className="overflow-x-auto rounded-xl border border-linea bg-fondo">
          <table className="min-w-full text-sm">
            <caption className="sr-only">{t('usuarios.tabla')}</caption>
            <thead className="text-left text-[11px] font-semibold uppercase tracking-wide text-tinta-suave">
              <tr className="border-b border-linea">
                <th scope="col" className="px-4 py-3">
                  {t('usuarios.col.usuario')}
                </th>
                <th scope="col" className="px-4 py-3">
                  {t('usuarios.col.alcance')}
                </th>
                <th scope="col" className="px-4 py-3">
                  {t('usuarios.col.estado')}
                </th>
                <th scope="col" className="px-4 py-3 text-right">
                  <span className="sr-only">{t('usuarios.col.acciones')}</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-linea">
              {usuarios.map((u) => (
                <tr key={u.usuarioId}>
                  <th scope="row" className="px-4 py-3 text-left font-normal">
                    <span className="block font-semibold">{u.nombre}</span>
                    <span className="block text-xs text-tinta-suave">{u.correo}</span>
                  </th>
                  <td className="px-4 py-3 text-tinta-suave">
                    {u.alcanceTotalSedes
                      ? t('usuarios.alcance.total')
                      : t('usuarios.alcance.sedes', { cantidad: u.sedes.length })}
                  </td>
                  <td className="px-4 py-3">{t(`usuarios.estado.${u.estado}`)}</td>
                  <td className="px-4 py-3">
                    {puedeAdministrar && (
                      <div className="flex justify-end gap-1">
                        <button
                          type="button"
                          aria-label={t('usuarios.accesos', { nombre: u.nombre })}
                          onClick={() => setAccesosDe(u)}
                          className="rounded-lg p-1.5 text-tinta-suave hover:bg-marca-tenue hover:text-marca"
                        >
                          <Icono nombre="editar" className="size-4" />
                        </button>
                        {u.estado !== 'Pendiente' && (
                          <button
                            type="button"
                            onClick={() =>
                              cambiarEstado.mutate({
                                usuarioId: u.usuarioId,
                                accion: u.estado === 'Activo' ? 'inactivar' : 'reactivar',
                              })
                            }
                            className="rounded-lg px-2 py-1 text-xs font-medium text-tinta-suave hover:bg-superficie"
                          >
                            {u.estado === 'Activo'
                              ? t('usuarios.inactivar')
                              : t('usuarios.reactivar')}
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

      {invitando && <DialogoInvitar onCerrar={() => setInvitando(false)} />}
      {accesosDe && (
        <DialogoAccesos
          key={accesosDe.usuarioId}
          usuario={accesosDe}
          onCerrar={() => setAccesosDe(null)}
        />
      )}
    </section>
  )
}
