import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState, type CSSProperties, type FormEvent, type PointerEvent } from 'react'
import { useNavigate } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { ingresar } from './apiSesion'
import './ingresar.css'

/** Ejemplos ficticios que ilustran el flujo; no provienen de la API. */
export const EJEMPLOS = [
  { iniciales: 'VI', rol: 'Visitante', lugar: 'Recepción', tipo: 'entrada' },
  { iniciales: 'PR', rol: 'Proveedor', lugar: 'Bodega', tipo: 'entrada' },
  { iniciales: 'CO', rol: 'Contratista', lugar: 'Torre B', tipo: 'salida' },
  { iniciales: 'EM', rol: 'Empleado', lugar: 'Sede principal', tipo: 'entrada' },
] as const

/** Patrón decorativo tipo QR (determinista, sin datos). */
const CELDAS = Array.from({ length: 49 }, (_, i) => {
  const f = Math.floor(i / 7)
  const c = i % 7
  const esquina = (a: number, b: number) =>
    a <= 2 && b <= 2 && !(a === 1 && b === 1)
  if (esquina(f, c) || esquina(f, 6 - c) || esquina(6 - f, c)) return true
  if (f <= 2 && (c <= 2 || c >= 4)) return false
  if (f >= 4 && c <= 2) return false
  return ((f * 5 + c * 3 + f * c) % 3) !== 0
})

function useMovimientoReducido() {
  const [reducido, setReducido] = useState(false)
  useEffect(() => {
    const mq = window.matchMedia?.('(prefers-reduced-motion: reduce)')
    if (!mq) return
    setReducido(mq.matches)
    const cambio = () => setReducido(mq.matches)
    mq.addEventListener('change', cambio)
    return () => mq.removeEventListener('change', cambio)
  }, [])
  return reducido
}

export function IngresarPage() {
  const t = useT()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [correo, setCorreo] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [mostrar, setMostrar] = useState(false)
  const [indice, setIndice] = useState(0)
  const pase = useRef<HTMLDivElement>(null)
  const reducido = useMovimientoReducido()

  useEffect(() => {
    if (reducido) return
    const id = window.setInterval(() => setIndice((i) => (i + 1) % EJEMPLOS.length), 2600)
    return () => window.clearInterval(id)
  }, [reducido])

  function continuar() {
    queryClient.removeQueries({ queryKey: ['me'] })
    queryClient.removeQueries({ queryKey: ['sedes'] })
    void navigate('/dashboard', { replace: true })
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

  // Inclinación 3D sutil: transform directo sobre el elemento, solo con puntero fino.
  function inclinar(e: PointerEvent<HTMLDivElement>) {
    const el = pase.current
    if (!el || reducido || e.pointerType !== 'mouse') return
    const r = el.getBoundingClientRect()
    const x = (e.clientX - r.left) / r.width - 0.5
    const y = (e.clientY - r.top) / r.height - 0.5
    el.style.transform = `rotateX(${(-y * 5).toFixed(2)}deg) rotateY(${(x * 7).toFixed(2)}deg)`
    el.style.setProperty('--brillo-x', `${((x + 0.5) * 100).toFixed(1)}%`)
    el.style.setProperty('--brillo-y', `${((y + 0.5) * 100).toFixed(1)}%`)
  }

  function soltar() {
    if (pase.current) pase.current.style.transform = ''
  }

  const estadoPase = ingreso.isPending ? 'validando' : ingreso.isError ? 'rechazado' : 'listo'
  const visibles = [0, 1, 2].map((k) => EJEMPLOS[(indice + k) % EJEMPLOS.length]!)

  return (
    <main className="lg">
      <div className="lg-fondo" aria-hidden="true">
        <div className="lg-aurora" />
        <div className="lg-puntos" />
        {[1, 2, 3, 4, 5, 6].map((n) => (
          <span key={n} className={`lg-burbuja lg-burbuja-${n}`} />
        ))}
      </div>

      <section className="lg-hero" aria-label="ConecsIA">
        <div className="lg-marca lg-entra" style={{ '--d': 0 } as CSSProperties}>
          <span className="lg-marca-icono" aria-hidden="true">
            <svg viewBox="0 0 24 24">
              <path d="M12 2 4 6v6c0 5 3.4 8.6 8 10 4.6-1.4 8-5 8-10V6l-8-4Z" />
              <path d="m8.5 12 2.5 2.5 4.5-5" />
            </svg>
          </span>
          <span>
            <strong>ConecsIA</strong>
            <small>Visitantes y control de accesos</small>
          </span>
        </div>

        <div className="lg-copy">
          <p className="lg-eyebrow lg-entra" style={{ '--d': 1 } as CSSProperties}>
            <span className="lg-pulso" aria-hidden="true" /> Control de accesos en tiempo real
          </p>
          <h1 className="lg-titulo">
            <span className="lg-entra" style={{ '--d': 2 } as CSSProperties}>
              Gestiona cada acceso
            </span>
            <span className="lg-entra" style={{ '--d': 3 } as CSSProperties}>
              de forma segura y
            </span>
            <span className="lg-entra lg-acento" style={{ '--d': 4 } as CSSProperties}>
              eficiente.
            </span>
          </h1>
          <p className="lg-sub lg-entra" style={{ '--d': 5 } as CSSProperties}>
            Visitantes, contratistas y proveedores autorizados y trazados en todas tus sedes.
          </p>
        </div>

        <div className="lg-feed lg-entra" style={{ '--d': 6 } as CSSProperties}>
          <div className="lg-feed-cab">
            <span>Actividad</span>
            <small>Vista ilustrativa</small>
          </div>
          <ul aria-hidden="true">
            {visibles.map((ej, k) => (
              <li key={`${indice}-${k}`} className="lg-feed-item" style={{ '--k': k } as CSSProperties}>
                <span className="lg-avatar">{ej.iniciales}</span>
                <span className="lg-feed-texto">
                  <b>{ej.rol}</b>
                  <small>{ej.lugar}</small>
                </span>
                <span className={`lg-chip lg-chip-${ej.tipo}`}>
                  {ej.tipo === 'entrada' ? '↘ Entrada' : '↗ Salida'}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      <section className="lg-panel">
        <div className="lg-escena" onPointerMove={inclinar} onPointerLeave={soltar}>
          <div className="lg-pila" aria-hidden="true">
            <span />
            <span />
          </div>

          <div className="lg-pase lg-entra-pase" ref={pase} data-estado={estadoPase}>
            <span className="lg-ranura" aria-hidden="true" />
            <span className="lg-brillo" aria-hidden="true" />

            <header className="lg-pase-cab">
              <div>
                <span className="lg-etiqueta">Pase de ingreso</span>
                <h1 className="sr-only">{t('sesion.ingresarTitulo')}</h1>
                <h2>Bienvenido de nuevo</h2>
                <p>Identifícate para abrir tu turno.</p>
              </div>
              <div className="lg-qr" aria-hidden="true">
                {CELDAS.map((on, i) => (
                  <i key={i} data-on={on || undefined} />
                ))}
                <span className="lg-qr-scan" />
              </div>
            </header>

            <div className="lg-perforado" aria-hidden="true" />

            <form onSubmit={enviar} className="lg-form">
              {ingreso.error instanceof ApiError && (
                <div className="lg-error">
                  <ProblemDetailsAlert problem={ingreso.error.problem} />
                </div>
              )}

              <label className="lg-campo">
                <input
                  aria-label={t('sesion.correo')}
                  type="email"
                  name="correo"
                  autoComplete="username"
                  placeholder=" "
                  required
                  value={correo}
                  onChange={(e) => setCorreo(e.target.value)}
                />
                <span className="lg-flotante">Correo electrónico</span>
                <svg className="lg-icono" viewBox="0 0 24 24" aria-hidden="true">
                  <rect x="3" y="5" width="18" height="14" rx="3" />
                  <path d="m4 7 8 6 8-6" />
                </svg>
              </label>

              <label className="lg-campo">
                <input
                  aria-label={t('sesion.contrasena')}
                  type={mostrar ? 'text' : 'password'}
                  name="contrasena"
                  autoComplete="current-password"
                  placeholder=" "
                  required
                  value={contrasena}
                  onChange={(e) => setContrasena(e.target.value)}
                />
                <span className="lg-flotante">{t('sesion.contrasena')}</span>
                <svg className="lg-icono" viewBox="0 0 24 24" aria-hidden="true">
                  <rect x="4" y="10" width="16" height="11" rx="3" />
                  <path d="M8 10V7a4 4 0 0 1 8 0v3" />
                </svg>
                <button
                  type="button"
                  className="lg-ojo"
                  onClick={() => setMostrar((v) => !v)}
                  aria-label={mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                  aria-pressed={mostrar}
                >
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
                    <circle cx="12" cy="12" r="3" />
                    <path className="lg-ojo-tachado" d="M3 3l18 18" data-on={mostrar || undefined} />
                  </svg>
                </button>
              </label>

              <div className="lg-opciones">
                <label>
                  <input
                    type="checkbox"
                    disabled
                    title="La política de sesión actual es administrada por el servidor"
                  />
                  <span>Recordarme</span>
                </label>
                <button type="button" className="lg-link" disabled>
                  ¿Olvidaste tu contraseña?
                </button>
              </div>

              <button
                type="submit"
                aria-label={t('sesion.entrar')}
                disabled={ingreso.isPending}
                className="lg-enviar"
                aria-busy={ingreso.isPending}
              >
                <span className="lg-enviar-texto" data-oculto={ingreso.isPending || undefined}>
                  Iniciar sesión
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M5 12h14M13 6l6 6-6 6" />
                  </svg>
                </span>
                <span className="lg-spinner" data-on={ingreso.isPending || undefined} aria-hidden="true" />
              </button>

              <div className="lg-divisor">
                <span>o</span>
              </div>

              <button
                type="button"
                className="lg-microsoft"
                disabled
                title="Microsoft no está habilitado en el alcance actual"
              >
                <span className="ms-logo" aria-hidden="true">
                  <i style={{ background: '#f25022' }} />
                  <i style={{ background: '#7fba00' }} />
                  <i style={{ background: '#00a4ef' }} />
                  <i style={{ background: '#ffb900' }} />
                </span>
                Continuar con Microsoft
              </button>
            </form>

            <footer className="lg-pase-pie">
              <span className="lg-estado">
                <i aria-hidden="true" />
                {estadoPase === 'validando'
                  ? 'Validando credenciales…'
                  : estadoPase === 'rechazado'
                    ? 'Acceso no concedido'
                    : 'Canal cifrado · sesión administrada'}
              </span>
              <span className="lg-serie">CNX-01</span>
            </footer>
          </div>
        </div>
      </section>
    </main>
  )
}
