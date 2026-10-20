import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import { apiFetch } from '../../shared/api/http'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { tienePermiso, useContexto } from '../sesion/useContexto'
import { useZonas, type Modo, type Zona } from './apiZonas'

/** HU-015 (ADR-019): zonas de una sede; un punto de acceso exige modo. La API valida y audita. */
export function ZonasPage() {
  const t = useT()
  const { sedeId = '' } = useParams()
  const queryClient = useQueryClient()
  const { data: contexto } = useContexto()
  const { data: zonas, error } = useZonas(sedeId)
  const invalidar = () => queryClient.invalidateQueries({ queryKey: ['zonas'] })
  const crear = useMutation({
    mutationFn: (datos: { nombre: string; esPuntoAcceso: boolean; modo: Modo | null }) =>
      apiFetch<Zona>(`/api/v1/sedes/${sedeId}/zonas`, {
        method: 'POST',
        body: JSON.stringify(datos),
      }),
    onSuccess: invalidar,
  })
  const inactivar = useMutation({
    mutationFn: (id: string) => apiFetch<Zona>(`/api/v1/zonas/${id}/inactivar`, { method: 'POST' }),
    onSuccess: invalidar,
  })
  const [nombre, setNombre] = useState('')
  const [punto, setPunto] = useState(false)
  const [modo, setModo] = useState<Modo>('Ambos')
  const errorActual = error ?? crear.error ?? inactivar.error

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    crear.mutate(
      { nombre, esPuntoAcceso: punto, modo: punto ? modo : null },
      { onSuccess: () => setNombre('') },
    )
  }

  return (
    <section className="space-y-5">
      <nav aria-label={t('nav.migas')} className="text-xs text-tinta-suave">
        <Link to="/sedes" className="hover:underline">
          {t('nav.sedes')}
        </Link>
      </nav>
      <h1 className="text-2xl font-semibold tracking-tight">{t('zonas.titulo')}</h1>
      {errorActual instanceof ApiError && <ProblemDetailsAlert problem={errorActual.problem} />}

      {tienePermiso(contexto, 'zonas.crear') && (
        <form onSubmit={enviar} className="flex flex-wrap items-end gap-3">
          <label className="text-sm">
            <span className="block font-medium">{t('zonas.form.nombre')}</span>
            <input
              required
              maxLength={150}
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              className="rounded-lg border border-linea px-3 py-2"
            />
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={punto} onChange={() => setPunto(!punto)} />
            {t('zonas.form.esPunto')}
          </label>
          {punto && (
            <label className="text-sm">
              <span className="block font-medium">{t('zonas.form.modo')}</span>
              <select
                value={modo}
                onChange={(e) => setModo(e.target.value as Modo)}
                className="rounded-lg border border-linea px-3 py-2"
              >
                <option value="Entrada">{t('zonas.modo.Entrada')}</option>
                <option value="Salida">{t('zonas.modo.Salida')}</option>
                <option value="Ambos">{t('zonas.modo.Ambos')}</option>
              </select>
            </label>
          )}
          <button
            type="submit"
            disabled={crear.isPending}
            className="rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            {t('zonas.nueva')}
          </button>
        </form>
      )}

      <ul
        aria-label={t('zonas.lista')}
        className="divide-y divide-linea rounded-xl border border-linea bg-fondo"
      >
        {zonas?.length === 0 && (
          <li className="p-4 text-sm text-tinta-suave">{t('zonas.vacio')}</li>
        )}
        {zonas?.map((z) => (
          <li key={z.id} className="flex items-center justify-between gap-2 p-3 text-sm">
            <span>
              <span className="font-semibold">{z.nombre}</span>{' '}
              <span className="text-xs text-tinta-suave">
                {z.esPuntoAcceso && z.modo
                  ? t('zonas.esPunto', { modo: t(`zonas.modo.${z.modo}`) })
                  : t('zonas.noEsPunto')}{' '}
                · {z.estado}
              </span>
            </span>
            {tienePermiso(contexto, 'zonas.inactivar') && z.estado === 'Activa' && (
              <button
                type="button"
                onClick={() => inactivar.mutate(z.id)}
                className="rounded-lg px-2 py-1 text-xs text-red-700 hover:bg-red-50"
              >
                {t('zonas.inactivar')}
              </button>
            )}
          </li>
        ))}
      </ul>
    </section>
  )
}
