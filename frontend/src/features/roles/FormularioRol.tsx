import { useState, type FormEvent } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { Dialogo } from '../../shared/ui/Dialogo'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import {
  agruparPorRecurso,
  useCatalogoPermisos,
  useGuardarRol,
  usePlantillasRol,
  type Rol,
} from './apiRoles'

/**
 * Alta y edición de roles (HU-017). Una plantilla solo precarga permisos: el rol creado es una
 * copia independiente. El servidor valida el catálogo, el anti-escalamiento y el último administrador.
 */
export function FormularioRol({ rol, onCerrar }: { rol: Rol | null; onCerrar: () => void }) {
  const t = useT()
  const { data: catalogo } = useCatalogoPermisos()
  const { data: plantillas } = usePlantillasRol()
  const guardar = useGuardarRol(rol)
  const [nombre, setNombre] = useState(rol?.nombre ?? '')
  const [descripcion, setDescripcion] = useState(rol?.descripcion ?? '')
  const [plantilla, setPlantilla] = useState<string | null>(null)
  const [permisos, setPermisos] = useState<Set<string>>(new Set(rol?.permisos ?? []))

  function elegirPlantilla(clave: string) {
    const elegida = plantillas?.find((p) => p.clave === clave)
    setPlantilla(elegida ? clave : null)
    if (elegida) {
      setPermisos(new Set(elegida.permisos))
      if (!nombre.trim()) setNombre(elegida.nombre)
    }
  }

  function alternar(clave: string) {
    setPermisos((actuales) => {
      const nuevos = new Set(actuales)
      if (nuevos.has(clave)) nuevos.delete(clave)
      else nuevos.add(clave)
      return nuevos
    })
  }

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    guardar.mutate(
      { nombre, descripcion, permisos: [...permisos], plantilla },
      { onSuccess: onCerrar },
    )
  }

  const campo = 'w-full rounded-lg border border-linea bg-fondo px-3 py-2 text-sm'

  return (
    <Dialogo titulo={rol ? t('roles.form.editar') : t('roles.form.nuevo')} onCerrar={onCerrar}>
      <form onSubmit={enviar} className="space-y-4">
        {guardar.error instanceof ApiError && (
          <ProblemDetailsAlert problem={guardar.error.problem} />
        )}

        {!rol && plantillas && (
          <div className="space-y-1">
            <label htmlFor="rol-plantilla" className="text-sm font-medium">
              {t('roles.form.plantilla')}
            </label>
            <select
              id="rol-plantilla"
              value={plantilla ?? ''}
              onChange={(e) => elegirPlantilla(e.target.value)}
              className={campo}
            >
              <option value="">{t('roles.form.sinPlantilla')}</option>
              {plantillas.map((p) => (
                <option key={p.clave} value={p.clave}>
                  {p.nombre}
                </option>
              ))}
            </select>
          </div>
        )}

        <div className="space-y-1">
          <label htmlFor="rol-nombre" className="text-sm font-medium">
            {t('roles.form.nombre')}
          </label>
          <input
            id="rol-nombre"
            value={nombre}
            onChange={(e) => setNombre(e.target.value)}
            maxLength={100}
            required
            className={campo}
          />
        </div>

        <div className="space-y-1">
          <label htmlFor="rol-descripcion" className="text-sm font-medium">
            {t('roles.form.descripcion')}
          </label>
          <input
            id="rol-descripcion"
            value={descripcion}
            onChange={(e) => setDescripcion(e.target.value)}
            maxLength={250}
            className={campo}
          />
        </div>

        <fieldset className="space-y-3">
          <legend className="text-sm font-medium">{t('roles.form.permisos')}</legend>
          {catalogo &&
            agruparPorRecurso(catalogo).map(([recurso, definiciones]) => (
              <div key={recurso} className="rounded-lg border border-linea p-3">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-tinta-suave">
                  {recurso}
                </p>
                <ul className="grid gap-1 sm:grid-cols-2">
                  {definiciones.map((d) => (
                    <li key={d.clave}>
                      <label className="flex items-center gap-2 text-sm">
                        <input
                          type="checkbox"
                          checked={permisos.has(d.clave)}
                          onChange={() => alternar(d.clave)}
                        />
                        {d.descripcion}
                      </label>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
        </fieldset>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onCerrar}
            className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
          >
            {t('roles.form.cancelar')}
          </button>
          <button
            type="submit"
            disabled={guardar.isPending}
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura disabled:opacity-60"
          >
            {t('roles.form.guardar')}
          </button>
        </div>
      </form>
    </Dialogo>
  )
}
