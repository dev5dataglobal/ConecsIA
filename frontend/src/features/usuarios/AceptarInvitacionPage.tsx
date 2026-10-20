import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { aceptarInvitacion } from './apiUsuarios'

/** HU-016: página pública para aceptar la invitación y definir la contraseña (D-03e: mínimo 12). */
export function AceptarInvitacionPage() {
  const t = useT()
  const { token = '' } = useParams()
  const [contrasena, setContrasena] = useState('')
  const [estado, setEstado] = useState<'inicial' | 'enviando' | 'aceptada'>('inicial')
  const [error, setError] = useState<ApiError | null>(null)

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    setEstado('enviando')
    setError(null)
    try {
      await aceptarInvitacion(token, contrasena)
      setEstado('aceptada')
    } catch (e) {
      setError(e instanceof ApiError ? e : null)
      setEstado('inicial')
    }
  }

  return (
    <section className="mx-auto max-w-md space-y-4 py-10">
      <h1 className="text-xl font-semibold">{t('invitacion.titulo')}</h1>
      {estado === 'aceptada' ? (
        <>
          <p className="text-sm">{t('invitacion.aceptada')}</p>
          <Link to="/ingresar" className="text-sm font-medium text-marca underline">
            {t('invitacion.irAIngresar')}
          </Link>
        </>
      ) : (
        <form onSubmit={(e) => void enviar(e)} className="space-y-4">
          {error && <ProblemDetailsAlert problem={error.problem} />}
          <div className="space-y-1">
            <label htmlFor="inv-contrasena" className="text-sm font-medium">
              {t('invitacion.contrasena')}
            </label>
            <input
              id="inv-contrasena"
              type="password"
              autoComplete="new-password"
              minLength={12}
              required
              value={contrasena}
              onChange={(e) => setContrasena(e.target.value)}
              className="w-full rounded-lg border border-linea bg-fondo px-3 py-2 text-sm"
            />
            <p className="text-xs text-tinta-suave">{t('invitacion.ayuda')}</p>
          </div>
          <button
            type="submit"
            disabled={estado === 'enviando'}
            className="w-full rounded-lg bg-marca px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
          >
            {t('invitacion.aceptar')}
          </button>
        </form>
      )}
    </section>
  )
}
