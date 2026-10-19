import { useId, useMemo, useState, type FormEvent } from 'react'
import { ApiError, fieldErrors } from '../../shared/api/problemDetails'
import type { ClaveTexto } from '../../shared/i18n/es'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { useGuardarSede, type DatosSede, type Sede } from './apiSedes'
import { Dialogo } from '../../shared/ui/Dialogo'

const ZONA_POR_DEFECTO = 'America/Bogota'

/** Zonas IANA del navegador. La validación definitiva la hace el servidor. */
function zonasHorarias(actual: string): string[] {
  const zonas = new Set<string>(['UTC', ZONA_POR_DEFECTO, actual])
  try {
    for (const zona of Intl.supportedValuesOf('timeZone')) zonas.add(zona)
  } catch {
    // Navegador sin Intl.supportedValuesOf: quedan las zonas básicas.
  }
  return [...zonas].filter(Boolean).sort()
}

const campos: { nombre: keyof Omit<DatosSede, 'zonaHoraria'>; clave: ClaveTexto; max: number }[] = [
  { nombre: 'nombre', clave: 'sedes.form.nombre', max: 150 },
  { nombre: 'direccion', clave: 'sedes.form.direccion', max: 250 },
  { nombre: 'ciudad', clave: 'sedes.form.ciudad', max: 100 },
  { nombre: 'pais', clave: 'sedes.form.pais', max: 100 },
]

export function FormularioSede({ sede, onCerrar }: { sede: Sede | null; onCerrar: () => void }) {
  const t = useT()
  const id = useId()
  const guardar = useGuardarSede(sede)
  const [datos, setDatos] = useState<DatosSede>({
    nombre: sede?.nombre ?? '',
    direccion: sede?.direccion ?? '',
    ciudad: sede?.ciudad ?? '',
    pais: sede?.pais ?? '',
    zonaHoraria: sede?.zonaHoraria ?? ZONA_POR_DEFECTO,
  })
  // Cientos de opciones: se construyen una vez para no repintarlas en cada tecla.
  const opcionesZona = useMemo(
    () =>
      zonasHorarias(sede?.zonaHoraria ?? ZONA_POR_DEFECTO).map((zona) => (
        <option key={zona} value={zona}>
          {zona}
        </option>
      )),
    [sede?.zonaHoraria],
  )
  const problema = guardar.error instanceof ApiError ? guardar.error.problem : null
  const errores = (campo: string) => (problema ? fieldErrors(problema, campo) : [])

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    if (guardar.isPending) return // evita doble envío
    guardar.mutate(datos, { onSuccess: onCerrar })
  }

  const claseCampo =
    'mt-1 block w-full rounded-lg border border-linea bg-fondo px-3 py-2 text-sm focus:border-marca focus:outline-2 focus:outline-marca-suave aria-[invalid=true]:border-red-500'

  return (
    <Dialogo
      titulo={t(sede ? 'sedes.form.tituloEditar' : 'sedes.form.tituloNueva')}
      onCerrar={onCerrar}
    >
      <form onSubmit={enviar} noValidate className="space-y-3">
        {problema && <ProblemDetailsAlert problem={problema} />}
        {campos.map((campo) => {
          const lista = errores(campo.nombre)
          return (
            <div key={campo.nombre}>
              <label htmlFor={`${id}-${campo.nombre}`} className="text-sm font-medium">
                {t(campo.clave)}
              </label>
              <input
                id={`${id}-${campo.nombre}`}
                value={datos[campo.nombre]}
                maxLength={campo.max}
                required={campo.nombre === 'nombre'}
                aria-invalid={lista.length > 0}
                aria-describedby={lista.length ? `${id}-${campo.nombre}-error` : undefined}
                onChange={(e) => setDatos({ ...datos, [campo.nombre]: e.target.value })}
                className={claseCampo}
              />
              {lista.length > 0 && (
                <p id={`${id}-${campo.nombre}-error`} className="mt-1 text-xs text-red-700">
                  {lista.join(' ')}
                </p>
              )}
            </div>
          )
        })}
        <div>
          <label htmlFor={`${id}-zona`} className="text-sm font-medium">
            {t('sedes.form.zona')}
          </label>
          <select
            id={`${id}-zona`}
            value={datos.zonaHoraria}
            aria-invalid={errores('zonaHoraria').length > 0}
            aria-describedby={errores('zonaHoraria').length ? `${id}-zona-error` : undefined}
            onChange={(e) => setDatos({ ...datos, zonaHoraria: e.target.value })}
            className={claseCampo}
          >
            {opcionesZona}
          </select>
          {errores('zonaHoraria').length > 0 && (
            <p id={`${id}-zona-error`} className="mt-1 text-xs text-red-700">
              {errores('zonaHoraria').join(' ')}
            </p>
          )}
        </div>
        <div className="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onCerrar}
            className="rounded-lg border border-linea px-4 py-2 text-sm hover:bg-superficie"
          >
            {t('sedes.form.cancelar')}
          </button>
          <button
            type="submit"
            disabled={guardar.isPending}
            aria-busy={guardar.isPending}
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white hover:bg-marca-oscura disabled:opacity-60"
          >
            {guardar.isPending ? t('sedes.form.guardando') : t('sedes.form.guardar')}
          </button>
        </div>
      </form>
    </Dialogo>
  )
}
