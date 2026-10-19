import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { apiFetch } from '../../shared/api/http'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { ingresar } from './apiSesion'
import { guardarSesionDesarrollo } from './sesionDesarrollo'
import type { UsuarioDesarrollo } from './useContexto'

const campo =
  'w-full rounded-lg border border-linea bg-fondo px-3 py-2 text-sm focus-visible:outline-2 focus-visible:outline-marca'

/**
 * Inicio de sesión (HU-006, ADR-006). El error de credenciales es genérico: lo redacta el
 * servidor y no revela si el correo existe. El selector de usuarios sintéticos aparece solo
 * cuando la API tiene habilitado el modo desarrollo (ADR-022); en producción responde 404.
 */
export function IngresarPage() {
  const t = useT()
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const destino = (location.state as { desde?: string } | null)?.desde ?? '/sedes'
  const [correo, setCorreo] = useState('')
  const [contrasena, setContrasena] = useState('')

  const desarrollo = useQuery({
    queryKey: ['desarrollo', 'usuarios'],
    queryFn: () => apiFetch<UsuarioDesarrollo[]>('/api/v1/desarrollo/usuarios'),
    retry: false,
  })

  function continuar() {
    queryClient.removeQueries({ queryKey: ['me'] })
    queryClient.removeQueries({ queryKey: ['sedes'] })
    void navigate(destino, { replace: true })
  }

  const ingreso = useMutation({
    mutationFn: () => ingresar(correo.trim(), contrasena),
    onSuccess: continuar,
    onSettled: () => setContrasena(''),
  })

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    ingreso.mutate()
  }

  function elegir(usuario: UsuarioDesarrollo) {
    guardarSesionDesarrollo(usuario.id)
    continuar()
  }

  return (
    <section className="mx-auto max-w-md space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight">{t('sesion.ingresarTitulo')}</h1>

      <form onSubmit={enviar} className="space-y-4 rounded-lg border border-linea bg-fondo p-4">
        {ingreso.error instanceof ApiError && (
          <ProblemDetailsAlert problem={ingreso.error.problem} />
        )}
        <label className="block space-y-1 text-sm font-medium">
          <span>{t('sesion.correo')}</span>
          <input
            type="email"
            name="correo"
            autoComplete="username"
            required
            value={correo}
            onChange={(e) => setCorreo(e.target.value)}
            className={campo}
          />
        </label>
        <label className="block space-y-1 text-sm font-medium">
          <span>{t('sesion.contrasena')}</span>
          <input
            type="password"
            name="contrasena"
            autoComplete="current-password"
            required
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
            className={campo}
          />
        </label>
        <button
          type="submit"
          disabled={ingreso.isPending}
          className="w-full rounded-lg bg-marca px-4 py-2 text-sm font-semibold text-white hover:opacity-90 disabled:opacity-60"
        >
          {ingreso.isPending ? t('sesion.entrando') : t('sesion.entrar')}
        </button>
      </form>

      {desarrollo.data && (
        <section className="space-y-3" aria-labelledby="titulo-desarrollo">
          <h2 id="titulo-desarrollo" className="text-lg font-semibold">
            {t('sesion.desarrolloTitulo')}
          </h2>
          <p className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900">
            {t('sesion.ingresarAviso')}
          </p>
          {desarrollo.data.length === 0 && (
            <p className="text-sm text-tinta-suave">{t('sesion.sinUsuarios')}</p>
          )}
          <ul className="space-y-2">
            {desarrollo.data.map((usuario) => (
              <li key={usuario.id}>
                <button
                  type="button"
                  onClick={() => elegir(usuario)}
                  className="w-full rounded-lg border border-linea bg-fondo p-3 text-left hover:border-marca focus-visible:outline-2 focus-visible:outline-marca"
                >
                  <span className="block font-medium">{usuario.nombre}</span>
                  <span className="block text-sm text-tinta-suave">{usuario.correo}</span>
                  <span className="block text-xs text-tinta-suave">
                    {usuario.organizaciones.join(' · ')}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        </section>
      )}
    </section>
  )
}
