import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { ingresar } from './apiSesion'
import './ingresar.css'

interface UsuarioDesarrollo {
  id: string
  nombre: string
  correo: string
  organizaciones: string[]
}

export function IngresarPage() {
  const t = useT()
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const destino = (location.state as { desde?: string } | null)?.desde ?? '/dashboard'
  const [correo, setCorreo] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [mostrar, setMostrar] = useState(false)
  const desarrollo: { data?: UsuarioDesarrollo[] } = {}

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
    if (!ingreso.isPending) ingreso.mutate()
  }

  function elegir(_usuario: UsuarioDesarrollo) {
    // El selector de usuarios sintéticos pertenece al flujo antiguo y permanece deshabilitado.
  }

  return (
    <main className="login-page">
      <section className="login-hero" aria-label="ConecsIA">
        <div className="login-grid" aria-hidden="true" />
        <div className="login-orb login-orb-1" aria-hidden="true" />
        <div className="login-orb login-orb-2" aria-hidden="true" />
        <div className="login-orb login-orb-3" aria-hidden="true" />
        <div className="login-ring" aria-hidden="true" />

        <div className="login-brand login-in">
          <span className="login-brand-mark">✦</span>
          <span>
            <strong>ConecsIA</strong>
            <small>Visitantes y control de accesos</small>
          </span>
        </div>

        <div className="login-hero-copy">
          <h1>
            <span className="login-line">
              <span>Gestiona cada acceso</span>
            </span>
            <span className="login-line">
              <span>de forma segura y</span>
            </span>
            <span className="login-line">
              <span className="login-grad">eficiente.</span>
            </span>
          </h1>
          <p className="login-in" style={{ animationDelay: '.45s' }}>
            Visitantes, contratistas, proveedores y autorizaciones bajo un mismo control, en todas
            tus sedes y en tiempo real.
          </p>
        </div>

        <div className="login-benefits login-in" style={{ animationDelay: '1s' }}>
          <span>
            <b>01</b>
            <small>Control centralizado</small>
          </span>
          <span>
            <b>24/7</b>
            <small>Visibilidad continua</small>
          </span>
          <span>
            <b>100%</b>
            <small>Trazabilidad</small>
          </span>
        </div>
      </section>

      <section className="login-form-panel">
        <div className="login-form-wrap">
          <h1 className="sr-only">{t('sesion.ingresarTitulo')}</h1>
          <div className="login-heading login-in" style={{ animationDelay: '.3s' }}>
            <h2>
              Bienvenido{' '}
              <span className="login-wave" aria-hidden="true">
                👋
              </span>
            </h2>
            <p>Inicia sesión para continuar</p>
          </div>

          <form onSubmit={enviar} className="login-form">
            {ingreso.error instanceof ApiError && (
              <ProblemDetailsAlert problem={ingreso.error.problem} />
            )}
            <label className="login-field">
              <span>Correo electrónico o usuario</span>
              <span className="login-input-wrap">
                <svg className="login-input-icon" viewBox="0 0 24 24" aria-hidden="true">
                  <rect x="0" y="1" width="24" height="22" rx="2" />
                  <path d="m3 7 9 6 9-6" />
                </svg>
                <input
                  aria-label={t('sesion.correo')}
                  type="email"
                  name="correo"
                  autoComplete="username"
                  required
                  value={correo}
                  onChange={(e) => setCorreo(e.target.value)}
                />
              </span>
            </label>
            <label className="login-field">
              <span>{t('sesion.contrasena')}</span>
              <span className="login-input-wrap">
                <svg className="login-input-icon" viewBox="0 0 24 24" aria-hidden="true">
                  <rect x="0" y="1" width="24" height="22" rx="2" />
                  <path d="M8 11V7a4 4 0 0 1 8 0v4" />
                </svg>
                <input
                  aria-label={t('sesion.contrasena')}
                  type={mostrar ? 'text' : 'password'}
                  name="contrasena"
                  autoComplete="current-password"
                  required
                  value={contrasena}
                  onChange={(e) => setContrasena(e.target.value)}
                />
                <button
                  type="button"
                  className="login-toggle"
                  onClick={() => setMostrar((v) => !v)}
                  aria-label={mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                >
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
                    <circle cx="12" cy="12" r="3" />
                    {mostrar && <path d="M3 3l18 18" />}
                  </svg>
                </button>
              </span>
            </label>
            <div className="login-options">
              <label>
                <input
                  type="checkbox"
                  disabled
                  title="La política de sesión actual es administrada por el servidor"
                />
                <span>Recordarme</span>
              </label>
              <button type="button" className="login-link" disabled>
                ¿Olvidaste tu contraseña?
              </button>
            </div>
            <button
              type="submit"
              aria-label={t('sesion.entrar')}
              disabled={ingreso.isPending}
              className="login-submit"
              aria-busy={ingreso.isPending}
            >
              {ingreso.isPending ? (
                <span className="login-spinner" aria-hidden="true" />
              ) : (
                <>
                  Iniciar sesión{' '}
                  <span className="login-arrow" aria-hidden="true">
                    →
                  </span>
                </>
              )}
            </button>
            <div className="login-divider">
              <span>O continúa con</span>
            </div>
            <button
              type="button"
              className="login-microsoft"
              disabled
              title="Microsoft no está habilitado en el alcance actual"
            >
              <span className="ms-logo" aria-hidden="true">
                <i style={{ background: '#f25022' }} />
                <i style={{ background: '#7fba00' }} />
                <i style={{ background: '#00a4ef' }} />
                <i style={{ background: '#ffb900' }} />
              </span>
              Iniciar sesión con Microsoft
            </button>
          </form>

          {desarrollo.data && (
            <section className="login-dev" aria-labelledby="titulo-desarrollo">
              <h2 id="titulo-desarrollo">{t('sesion.desarrolloTitulo')}</h2>
              <p className="login-dev-aviso">{t('sesion.ingresarAviso')}</p>
              {desarrollo.data.length === 0 && <p>{t('sesion.sinUsuarios')}</p>}
              <ul>
                {desarrollo.data.map((usuario) => (
                  <li key={usuario.id}>
                    <button type="button" onClick={() => elegir(usuario)}>
                      <strong>{usuario.nombre}</strong>
                      <span>{usuario.correo}</span>
                      <small>{usuario.organizaciones.join(' · ')}</small>
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          )}

          <p className="login-note">La sesión está protegida y administrada por ConecsIA.</p>
        </div>
      </section>
    </main>
  )
}

/** Ejemplos ficticios que ilustran el flujo; no provienen de la API. */
export const EJEMPLOS = [
  { iniciales: 'VI', rol: 'Visitante', lugar: 'Recepción', tipo: 'entrada' },
  { iniciales: 'PR', rol: 'Proveedor', lugar: 'Bodega', tipo: 'entrada' },
  { iniciales: 'CO', rol: 'Contratista', lugar: 'Torre B', tipo: 'salida' },
  { iniciales: 'EM', rol: 'Empleado', lugar: 'Sede principal', tipo: 'entrada' },
] as const

