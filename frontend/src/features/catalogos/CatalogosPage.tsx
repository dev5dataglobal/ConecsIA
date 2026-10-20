import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { apiFetch } from '../../shared/api/http'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { tienePermiso, useContexto } from '../sesion/useContexto'

interface Elemento {
  id: string
  catalogo: string
  clave: string
  nombre: string
  origen: string
  estado: 'Activo' | 'Inactivo'
}

const RUTA = '/api/v1/catalogos/tipos-area'

/**
 * HU-010: catálogo de tipos de área de la organización. Muestra qué elementos vienen de la
 * plantilla y cuáles son personalizados; reaplicar la plantilla nunca los modifica.
 */
export function CatalogosPage() {
  const t = useT()
  const queryClient = useQueryClient()
  const { data: contexto } = useContexto()
  const { data, error } = useQuery({
    queryKey: ['catalogos', 'tipos-area'],
    queryFn: () => apiFetch<Elemento[]>(RUTA),
    retry: false,
  })
  const invalidar = () => queryClient.invalidateQueries({ queryKey: ['catalogos'] })
  const agregar = useMutation({
    mutationFn: (datos: { clave: string; nombre: string }) =>
      apiFetch<Elemento>(RUTA, { method: 'POST', body: JSON.stringify(datos) }),
    onSuccess: invalidar,
  })
  const inactivar = useMutation({
    mutationFn: (id: string) => apiFetch<Elemento>(`${RUTA}/${id}/inactivar`, { method: 'POST' }),
    onSuccess: invalidar,
  })
  const [clave, setClave] = useState('')
  const [nombre, setNombre] = useState('')
  const puedeModificar = tienePermiso(contexto, 'configuracion.modificar')
  const errorActual = error ?? agregar.error ?? inactivar.error

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    agregar.mutate(
      { clave, nombre },
      {
        onSuccess: () => {
          setClave('')
          setNombre('')
        },
      },
    )
  }

  return (
    <section className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight">{t('catalogos.titulo')}</h1>
      <p className="text-sm text-tinta-suave">{t('catalogos.descripcion')}</p>
      {errorActual instanceof ApiError && <ProblemDetailsAlert problem={errorActual.problem} />}
      {puedeModificar && (
        <form onSubmit={enviar} className="flex flex-wrap items-end gap-2">
          <label className="text-sm">
            <span className="block font-medium">{t('catalogos.clave')}</span>
            <input
              required
              pattern="[a-zA-Z0-9-]+"
              value={clave}
              onChange={(e) => setClave(e.target.value)}
              className="rounded-lg border border-linea px-3 py-2"
            />
          </label>
          <label className="text-sm">
            <span className="block font-medium">{t('catalogos.nombre')}</span>
            <input
              required
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              className="rounded-lg border border-linea px-3 py-2"
            />
          </label>
          <button
            type="submit"
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white"
          >
            {t('catalogos.agregar')}
          </button>
        </form>
      )}
      <ul
        aria-label={t('catalogos.lista')}
        className="divide-y divide-linea rounded-xl border border-linea bg-fondo"
      >
        {data?.map((e) => (
          <li key={e.id} className="flex items-center justify-between gap-2 p-3 text-sm">
            <span>
              <span className="font-semibold">{e.nombre}</span>{' '}
              <span className="text-xs text-tinta-suave">
                {e.origen === 'personalizado'
                  ? t('catalogos.personalizado')
                  : t('catalogos.dePlantilla')}{' '}
                · {e.estado}
              </span>
            </span>
            {puedeModificar && e.estado === 'Activo' && (
              <button
                type="button"
                onClick={() => inactivar.mutate(e.id)}
                className="rounded-lg px-2 py-1 text-xs hover:bg-superficie"
              >
                {t('catalogos.inactivar')}
              </button>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
