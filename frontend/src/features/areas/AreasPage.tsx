import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Dialogo } from '../../shared/ui/Dialogo'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { tienePermiso, useContexto } from '../sesion/useContexto'
import { useUsuarios } from '../usuarios/apiUsuarios'
import {
  useAreas,
  useAsignar,
  useCrearArea,
  useInactivarArea,
  type Area,
  type Funcion,
} from './apiAreas'

/** HU-014: asignar responsables o autorizadores. Ser autorizador no concede permisos (RN-10). */
function DialogoAsignar({
  area,
  funcion,
  onCerrar,
}: {
  area: Area
  funcion: Funcion
  onCerrar: () => void
}) {
  const t = useT()
  const { data: usuarios } = useUsuarios()
  const asignar = useAsignar(area, funcion)
  const [elegidos, setElegidos] = useState(new Set(area[funcion]))

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    asignar.mutate([...elegidos], { onSuccess: onCerrar })
  }

  return (
    <Dialogo titulo={t(`areas.asignar.${funcion}`, { nombre: area.nombre })} onCerrar={onCerrar}>
      <form onSubmit={enviar} className="space-y-4">
        {asignar.error instanceof ApiError && (
          <ProblemDetailsAlert problem={asignar.error.problem} />
        )}
        {funcion === 'autorizadores' && (
          <p className="text-xs text-tinta-suave">{t('areas.asignar.aviso')}</p>
        )}
        <ul className="space-y-1">
          {usuarios
            ?.filter((u) => u.estado === 'Activo')
            .map((u) => (
              <li key={u.usuarioId}>
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={elegidos.has(u.usuarioId)}
                    onChange={() => {
                      const nuevos = new Set(elegidos)
                      if (nuevos.has(u.usuarioId)) nuevos.delete(u.usuarioId)
                      else nuevos.add(u.usuarioId)
                      setElegidos(nuevos)
                    }}
                  />
                  {u.nombre}
                </label>
              </li>
            ))}
        </ul>
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={onCerrar}
            className="rounded-lg border border-linea px-4 py-2 text-sm"
          >
            {t('areas.form.cancelar')}
          </button>
          <button
            type="submit"
            disabled={asignar.isPending}
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            {t('areas.form.guardar')}
          </button>
        </div>
      </form>
    </Dialogo>
  )
}

/** HU-014: áreas o destinos de una sede. La API verifica permiso, alcance de sede y organización. */
export function AreasPage() {
  const t = useT()
  const { sedeId = '' } = useParams()
  const { data: contexto } = useContexto()
  const { data: areas, error, isPending } = useAreas(sedeId)
  const crear = useCrearArea(sedeId)
  const inactivar = useInactivarArea()
  const [nombre, setNombre] = useState('')
  const [tipo, setTipo] = useState('')
  const [asignando, setAsignando] = useState<{ area: Area; funcion: Funcion } | null>(null)
  const puedeCrear = tienePermiso(contexto, 'areas.crear')
  const puedeAsignar = tienePermiso(contexto, 'areas.asignar')
  const puedeInactivar = tienePermiso(contexto, 'areas.inactivar')

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    crear.mutate(
      { nombre, tipo },
      {
        onSuccess: () => {
          setNombre('')
          setTipo('')
        },
      },
    )
  }

  return (
    <section className="space-y-5">
      <nav aria-label={t('nav.migas')} className="text-xs text-tinta-suave">
        <Link to="/sedes" className="hover:underline">
          {t('nav.sedes')}
        </Link>
      </nav>
      <h1 className="text-2xl font-semibold tracking-tight">{t('areas.titulo')}</h1>
      {error instanceof ApiError && <ProblemDetailsAlert problem={error.problem} />}
      {crear.error instanceof ApiError && <ProblemDetailsAlert problem={crear.error.problem} />}
      {inactivar.error instanceof ApiError && (
        <ProblemDetailsAlert problem={inactivar.error.problem} />
      )}

      {puedeCrear && (
        <form onSubmit={enviar} className="flex flex-wrap items-end gap-2">
          <label className="text-sm">
            <span className="block font-medium">{t('areas.form.nombre')}</span>
            <input
              required
              maxLength={150}
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              className="rounded-lg border border-linea px-3 py-2"
            />
          </label>
          <label className="text-sm">
            <span className="block font-medium">{t('areas.form.tipo')}</span>
            <input
              maxLength={50}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              className="rounded-lg border border-linea px-3 py-2"
            />
          </label>
          <button
            type="submit"
            disabled={crear.isPending}
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            {t('areas.nueva')}
          </button>
        </form>
      )}

      {isPending && <p className="text-sm text-tinta-suave">{t('areas.cargando')}</p>}
      {areas && (
        <ul
          aria-label={t('areas.lista')}
          className="divide-y divide-linea rounded-xl border border-linea bg-fondo"
        >
          {areas.length === 0 && (
            <li className="p-4 text-sm text-tinta-suave">{t('areas.vacio')}</li>
          )}
          {areas.map((area) => (
            <li key={area.id} className="flex flex-wrap items-center justify-between gap-2 p-4">
              <span>
                <span className="block font-semibold">{area.nombre}</span>
                <span className="block text-xs text-tinta-suave">
                  {t('areas.resumen', {
                    estado: t(`areas.estado.${area.estado}`),
                    responsables: area.responsables.length,
                    autorizadores: area.autorizadores.length,
                  })}
                </span>
              </span>
              <span className="flex gap-1">
                {puedeAsignar && area.estado === 'Activa' && (
                  <>
                    <button
                      type="button"
                      onClick={() => setAsignando({ area, funcion: 'responsables' })}
                      className="rounded-lg px-2 py-1 text-xs hover:bg-superficie"
                    >
                      {t('areas.responsables')}
                    </button>
                    <button
                      type="button"
                      onClick={() => setAsignando({ area, funcion: 'autorizadores' })}
                      className="rounded-lg px-2 py-1 text-xs hover:bg-superficie"
                    >
                      {t('areas.autorizadores')}
                    </button>
                  </>
                )}
                {puedeInactivar && area.estado === 'Activa' && (
                  <button
                    type="button"
                    onClick={() => inactivar.mutate(area)}
                    className="rounded-lg px-2 py-1 text-xs text-red-700 hover:bg-red-50"
                  >
                    {t('areas.inactivar')}
                  </button>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}

      {asignando && (
        <DialogoAsignar
          key={asignando.area.id + asignando.funcion}
          area={asignando.area}
          funcion={asignando.funcion}
          onCerrar={() => setAsignando(null)}
        />
      )}
    </section>
  )
}
