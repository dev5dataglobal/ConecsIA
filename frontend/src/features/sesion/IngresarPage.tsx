import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, useState, type CSSProperties, type FormEvent, type PointerEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { ProblemDetailsAlert } from '../../shared/ui/ProblemDetailsAlert'
import { ingresar, restablecerContrasena, solicitarRecuperacion } from './apiSesion'
import { haySesion } from './sesion'
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

export type ModoIngreso = 'ingresar' | 'recuperar' | 'restablecer'

const CABECERAS: Record<ModoIngreso, { etiqueta: string; titulo: string; texto: string }> = {
  ingresar: { etiqueta: 'Pase de ingreso', titulo: 'Bienvenido de nuevo', texto: 'Identifícate para abrir tu turno.' },
  recuperar: { etiqueta: 'Recuperar acceso', titulo: '¿Olvidaste tu contraseña?', texto: 'Te enviaremos un enlace para crear una nueva.' },
  restablecer: { etiqueta: 'Nueva contraseña', titulo: 'Crea tu contraseña', texto: 'El enlace sirve una sola vez y vence en minutos.' },
}

/** Misma regla que el servidor (PoliticaContrasena); el servidor la vuelve a validar. */
export function contrasenaValida(valor: string) {
  return valor.length >= 8 && /[A-ZÁÉÍÓÚÑ]/.test(valor) && /[a-záéíóúñ]/.test(valor) && /\d/.test(valor)
}

export function IngresarPage({ modo = 'ingresar' }: { modo?: ModoIngreso }) {
  const [indice, setIndice] = useState(0)
  const pase = useRef<HTMLDivElement>(null)
  const reducido = useMovimientoReducido()
  const [estadoPase, setEstadoPase] = useState<'listo' | 'validando' | 'rechazado'>('listo')

  useEffect(() => {
    if (reducido) return
    const id = window.setInterval(() => setIndice((i) => (i + 1) % EJEMPLOS.length), 2600)
    return () => window.clearInterval(id)
  }, [reducido])

  // Sesión restaurada desde la cookie (p. ej. navegador reabierto con «Recordarme»): no se pide ingresar otra vez.
  if (modo === 'ingresar' && haySesion()) return <Navigate to="/dashboard" replace />

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

  const visibles = [0, 1, 2].map((k) => EJEMPLOS[(indice + k) % EJEMPLOS.length]!)
  const cabecera = CABECERAS[modo]

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
                <span className="lg-etiqueta">{cabecera.etiqueta}</span>
                {modo === 'ingresar' && <IngresarTitulo />}
                <h2>{cabecera.titulo}</h2>
                <p>{cabecera.texto}</p>
              </div>
              <div className="lg-qr" aria-hidden="true">
                {CELDAS.map((on, i) => (
                  <i key={i} data-on={on || undefined} />
                ))}
                <span className="lg-qr-scan" />
              </div>
            </header>

            <div className="lg-perforado" aria-hidden="true" />

            {modo === 'ingresar' && <FormularioIngreso onEstado={setEstadoPase} />}
            {modo === 'recuperar' && <FormularioRecuperar onEstado={setEstadoPase} />}
            {modo === 'restablecer' && <FormularioRestablecer onEstado={setEstadoPase} />}

            <footer className="lg-pase-pie">
              <span className="lg-estado">
                <i aria-hidden="true" />
                {estadoPase === 'validando'
                  ? 'Validando…'
                  : estadoPase === 'rechazado'
                    ? modo === 'ingresar' ? 'Acceso no concedido' : 'Solicitud no completada'
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

function IngresarTitulo() {
  const t = useT()
  return <h1 className="sr-only">{t('sesion.ingresarTitulo')}</h1>
}

type Estado = (estado: 'listo' | 'validando' | 'rechazado') => void

function useEstadoPase(onEstado: Estado, pendiente: boolean, error: boolean) {
  useEffect(() => onEstado(pendiente ? 'validando' : error ? 'rechazado' : 'listo'), [onEstado, pendiente, error])
}

function FormularioIngreso({ onEstado }: { onEstado: Estado }) {
  const t = useT()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [correo, setCorreo] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [recordarme, setRecordarme] = useState(false)
  const [mostrar, setMostrar] = useState(false)

  const ingreso = useMutation({
    mutationFn: () => ingresar(correo.trim(), contrasena, recordarme),
    onSuccess: () => {
      // Los módulos y datos en caché son del usuario anterior: la nueva sesión los consulta de nuevo.
      queryClient.clear()
      void navigate('/dashboard', { replace: true })
    },
    onSettled: () => setContrasena(''),
  })
  useEstadoPase(onEstado, ingreso.isPending, ingreso.isError)

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    if (!ingreso.isPending) ingreso.mutate()
  }

  return (
    <form onSubmit={enviar} className="lg-form">
      {ingreso.error instanceof ApiError && (
        <div className="lg-error">
          <ProblemDetailsAlert problem={ingreso.error.problem} />
        </div>
      )}

      <CampoCorreo valor={correo} onChange={setCorreo} />

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
        <IconoCandado />
        <BotonOjo mostrar={mostrar} onClick={() => setMostrar((v) => !v)} />
      </label>

      <div className="lg-opciones">
        <label title="Conserva la sesión al cerrar y reabrir el navegador, dentro de la vigencia máxima de 8 horas. No lo uses en equipos compartidos.">
          <input type="checkbox" name="recordarme" checked={recordarme} onChange={(e) => setRecordarme(e.target.checked)} />
          <span>Recordarme</span>
        </label>
        <Link className="lg-link" to="/recuperar-contrasena">
          ¿Olvidaste tu contraseña?
        </Link>
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
          <IconoFlecha />
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
  )
}

function FormularioRecuperar({ onEstado }: { onEstado: Estado }) {
  const [correo, setCorreo] = useState('')
  const solicitud = useMutation({ mutationFn: () => solicitarRecuperacion(correo.trim()) })
  useEstadoPase(onEstado, solicitud.isPending, solicitud.isError)

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    if (!solicitud.isPending) solicitud.mutate()
  }

  if (solicitud.isSuccess) {
    return (
      <div className="lg-form">
        <p className="lg-aviso" role="status">
          {solicitud.data.mensaje} Revisa también la carpeta de correo no deseado. El enlace vence pronto y sirve una sola vez.
        </p>
        <Link className="lg-enviar lg-enviar-enlace" to="/ingresar">Volver a iniciar sesión</Link>
      </div>
    )
  }

  return (
    <form onSubmit={enviar} className="lg-form">
      {solicitud.error instanceof ApiError && solicitud.error.status === 503 ? (
        // Correo sin configurar: se informa con claridad en lugar de simular el envío.
        <p className="lg-aviso" data-tono="error" role="alert">
          {solicitud.error.problem.detail ?? 'La recuperación de contraseña no está disponible en este momento.'}
        </p>
      ) : solicitud.error instanceof ApiError && (
        <div className="lg-error">
          <ProblemDetailsAlert problem={solicitud.error.problem} />
        </div>
      )}
      <CampoCorreo valor={correo} onChange={setCorreo} autoFocus />
      <button type="submit" className="lg-enviar" disabled={solicitud.isPending} aria-busy={solicitud.isPending}>
        <span className="lg-enviar-texto" data-oculto={solicitud.isPending || undefined}>
          Enviar enlace
          <IconoFlecha />
        </span>
        <span className="lg-spinner" data-on={solicitud.isPending || undefined} aria-hidden="true" />
      </button>
      <Link className="lg-link lg-volver" to="/ingresar">Volver a iniciar sesión</Link>
    </form>
  )
}

/** Lee el token del fragmento (#token=…), que el navegador no envía al servidor, y lo retira de la barra de direcciones. */
function useTokenRecuperacion() {
  const [token] = useState(() => new URLSearchParams(window.location.hash.slice(1)).get('token') ?? '')
  useEffect(() => {
    if (window.location.hash) window.history.replaceState(window.history.state, '', window.location.pathname)
  }, [])
  return token
}

function FormularioRestablecer({ onEstado }: { onEstado: Estado }) {
  const token = useTokenRecuperacion()
  const [nueva, setNueva] = useState('')
  const [confirmacion, setConfirmacion] = useState('')
  const [mostrar, setMostrar] = useState(false)
  const [error, setError] = useState<string>()
  const cambio = useMutation({ mutationFn: () => restablecerContrasena(token, nueva, confirmacion) })
  useEstadoPase(onEstado, cambio.isPending, cambio.isError || Boolean(error))

  function enviar(evento: FormEvent) {
    evento.preventDefault()
    const mensaje = !contrasenaValida(nueva)
      ? 'La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un número.'
      : nueva !== confirmacion
        ? 'La confirmación no coincide con la nueva contraseña.'
        : undefined
    setError(mensaje)
    if (!mensaje && !cambio.isPending) cambio.mutate()
  }

  if (!token) {
    return (
      <div className="lg-form">
        <p className="lg-aviso" data-tono="error" role="alert">El enlace de recuperación no es válido. Solicita uno nuevo.</p>
        <Link className="lg-enviar lg-enviar-enlace" to="/recuperar-contrasena">Solicitar un enlace</Link>
      </div>
    )
  }

  if (cambio.isSuccess) {
    return (
      <div className="lg-form">
        <p className="lg-aviso" role="status">Tu contraseña se actualizó. Las sesiones abiertas se cerraron; inicia sesión con la nueva contraseña.</p>
        <Link className="lg-enviar lg-enviar-enlace" to="/ingresar">Iniciar sesión</Link>
      </div>
    )
  }

  return (
    <form onSubmit={enviar} className="lg-form" noValidate>
      {error && <p className="lg-aviso" data-tono="error" role="alert">{error}</p>}
      {cambio.error instanceof ApiError && (
        <div className="lg-error">
          <ProblemDetailsAlert problem={cambio.error.problem} />
        </div>
      )}
      <label className="lg-campo">
        <input
          aria-label="Nueva contraseña"
          type={mostrar ? 'text' : 'password'}
          name="nueva"
          autoComplete="new-password"
          placeholder=" "
          required
          autoFocus
          value={nueva}
          onChange={(e) => setNueva(e.target.value)}
        />
        <span className="lg-flotante">Nueva contraseña</span>
        <IconoCandado />
        <BotonOjo mostrar={mostrar} onClick={() => setMostrar((v) => !v)} />
      </label>
      <label className="lg-campo">
        <input
          aria-label="Confirmar contraseña"
          type={mostrar ? 'text' : 'password'}
          name="confirmacion"
          autoComplete="new-password"
          placeholder=" "
          required
          value={confirmacion}
          onChange={(e) => setConfirmacion(e.target.value)}
        />
        <span className="lg-flotante">Confirmar contraseña</span>
        <IconoCandado />
      </label>
      <p className="lg-ayuda">Mínimo 8 caracteres, con mayúscula, minúscula y número.</p>
      <button type="submit" className="lg-enviar" disabled={cambio.isPending} aria-busy={cambio.isPending}>
        <span className="lg-enviar-texto" data-oculto={cambio.isPending || undefined}>
          Guardar contraseña
          <IconoFlecha />
        </span>
        <span className="lg-spinner" data-on={cambio.isPending || undefined} aria-hidden="true" />
      </button>
      <Link className="lg-link lg-volver" to="/recuperar-contrasena">Solicitar otro enlace</Link>
    </form>
  )
}

function CampoCorreo({ valor, onChange, autoFocus }: { valor: string; onChange: (valor: string) => void; autoFocus?: boolean }) {
  const t = useT()
  return (
    <label className="lg-campo">
      <input
        aria-label={t('sesion.correo')}
        type="email"
        name="correo"
        autoComplete="username"
        placeholder=" "
        required
        autoFocus={autoFocus}
        value={valor}
        onChange={(e) => onChange(e.target.value)}
      />
      <span className="lg-flotante">Correo electrónico</span>
      <svg className="lg-icono" viewBox="0 0 24 24" aria-hidden="true">
        <rect x="3" y="5" width="18" height="14" rx="3" />
        <path d="m4 7 8 6 8-6" />
      </svg>
    </label>
  )
}

function IconoCandado() {
  return (
    <svg className="lg-icono" viewBox="0 0 24 24" aria-hidden="true">
      <rect x="4" y="10" width="16" height="11" rx="3" />
      <path d="M8 10V7a4 4 0 0 1 8 0v3" />
    </svg>
  )
}

function IconoFlecha() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path d="M5 12h14M13 6l6 6-6 6" />
    </svg>
  )
}

function BotonOjo({ mostrar, onClick }: { mostrar: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      className="lg-ojo"
      onClick={onClick}
      aria-label={mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña'}
      aria-pressed={mostrar}
    >
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
        <circle cx="12" cy="12" r="3" />
        <path className="lg-ojo-tachado" d="M3 3l18 18" data-on={mostrar || undefined} />
      </svg>
    </button>
  )
}
