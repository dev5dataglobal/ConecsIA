import {
  useEffect,
  useId,
  useRef,
  useState,
  type CSSProperties,
  type PointerEvent,
  type ReactNode,
} from 'react'
import { Link } from 'react-router'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { CarruselSectores } from './CarruselSectores'
import { ComparativaRetos } from './ComparativaRetos'
import { BeneficiosSection } from './BeneficiosSection'
import { PreguntasFrecuentes } from './PreguntasFrecuentes'
import { RecorridoPasos } from './RecorridoPasos'
import { ServiciosBento } from './ServiciosBento'
import { ACTIVIDAD_DEMO, BENEFICIOS, SECCIONES_NAV } from './contenido'
import { FormularioContacto } from './FormularioContacto'
import './presentacion.css'

const ANIO = new Date().getFullYear()

/** Logo y nombre; en el pie «IA» va en violeta. */
function Marca({ iaResaltada = false }: { iaResaltada?: boolean }) {
  return (
    <span className="lp-marca">
      <span className="lp-marca-icono" aria-hidden="true">
        <svg viewBox="0 0 24 24">
          <path d="M12 2 4 6v6c0 5 3.4 8.6 8 10 4.6-1.4 8-5 8-10V6l-8-4Z" />
          <path d="m8.5 12 2.5 2.5 4.5-5" />
        </svg>
      </span>
      <strong>Conecs{iaResaltada ? <span className="lp-violeta-solido">IA</span> : 'IA'}</strong>
    </span>
  )
}

/** Revela las secciones al entrar en pantalla; sin efecto si el usuario prefiere menos movimiento (CSS). */
function useRevelar() {
  useEffect(() => {
    const els = document.querySelectorAll<HTMLElement>('.lp-revela')
    if (!('IntersectionObserver' in window)) {
      els.forEach((el) => el.classList.add('lp-visible'))
      return
    }
    const io = new IntersectionObserver(
      (entradas) =>
        entradas.forEach((en) => {
          if (en.isIntersecting) {
            en.target.classList.add('lp-visible')
            io.unobserve(en.target)
          }
        }),
      { rootMargin: '0px 0px -10% 0px' },
    )
    els.forEach((el) => io.observe(el))
    return () => io.disconnect()
  }, [])
}

function Encabezado() {
  const [abierto, setAbierto] = useState(false)
  const [compacto, setCompacto] = useState(false)

  useEffect(() => {
    const alDesplazar = () => setCompacto(window.scrollY > 24)
    alDesplazar()
    window.addEventListener('scroll', alDesplazar, { passive: true })
    return () => window.removeEventListener('scroll', alDesplazar)
  }, [])

  useEffect(() => {
    if (!abierto) return
    const esc = (e: KeyboardEvent) => e.key === 'Escape' && setAbierto(false)
    window.addEventListener('keydown', esc)
    return () => window.removeEventListener('keydown', esc)
  }, [abierto])

  return (
    <header className="lp-header" data-compacto={compacto || undefined}>
      <nav className="lp-nav" aria-label="Principal">
        <a href="#inicio" className="lp-enlace-marca" aria-label="ConecsIA, ir al inicio">
          <Marca />
        </a>
        <button
          type="button"
          className="lp-menu-btn"
          aria-expanded={abierto}
          aria-controls="lp-menu"
          onClick={() => setAbierto((a) => !a)}
        >
          <Icono nombre={abierto ? 'cerrar' : 'menu'} className="lp-icono" />
          <span className="lp-oculto">{abierto ? 'Cerrar menú' : 'Abrir menú'}</span>
        </button>
        <div id="lp-menu" className="lp-menu" data-abierto={abierto || undefined}>
          <ul>
            {SECCIONES_NAV.map((s) => (
              <li key={s.id}>
                <a href={`#${s.id}`} onClick={() => setAbierto(false)}>
                  {s.texto}
                </a>
              </li>
            ))}
          </ul>
          <Link to="/ingresar" className="lp-btn lp-btn-pri lp-btn-login">
            Iniciar sesión
            <Icono nombre="flecha" className="lp-icono lp-flecha" />
          </Link>
        </div>
      </nav>
    </header>
  )
}

/** Prefiere menos movimiento: desactiva ciclos y parallax (las animaciones CSS ya se anulan en presentacion.css). */
function useMovimientoReducido() {
  const [reducido, setReducido] = useState(
    () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false,
  )
  useEffect(() => {
    const mq = window.matchMedia?.('(prefers-reduced-motion: reduce)')
    if (!mq) return
    const cambio = () => setReducido(mq.matches)
    mq.addEventListener('change', cambio)
    return () => mq.removeEventListener('change', cambio)
  }, [])
  return reducido
}

/** Patrón decorativo tipo QR, determinista y sin datos (mismo criterio que el ingreso). */
const CELDAS = Array.from({ length: 81 }, (_, i) => {
  const f = Math.floor(i / 9)
  const c = i % 9
  const marco = (a: number, b: number) => a <= 2 && b <= 2 && !(a === 1 && b === 1)
  if (marco(f, c) || marco(f, 8 - c) || marco(8 - f, c)) return true
  if ((f <= 3 && (c <= 3 || c >= 5)) || (f >= 5 && c <= 3)) return false
  return (f * 7 + c * 5 + f * c) % 3 !== 0
})

const FLUJO = ['Solicitada', 'Aprobada', 'Vigente', 'Entrada registrada'] as const

/** Fondo fijo: orbes difuminados, trama de puntos y red de conexiones. Puramente decorativo. */
function Ambiente() {
  const nodos = [
    [8, 22],
    [22, 12],
    [30, 36],
    [48, 18],
    [62, 40],
    [78, 14],
    [90, 34],
    [70, 70],
    [40, 78],
    [14, 64],
  ] as const
  const enlaces = [
    [0, 1],
    [1, 2],
    [1, 3],
    [3, 4],
    [3, 5],
    [5, 6],
    [4, 6],
    [4, 7],
    [7, 8],
    [8, 9],
    [9, 2],
    [2, 4],
  ] as const
  return (
    <div className="lp-ambiente" aria-hidden="true">
      <span className="lp-orbe lp-orbe-1" />
      <span className="lp-orbe lp-orbe-2" />
      <span className="lp-orbe lp-orbe-3" />
      <div className="lp-trama" />
      <svg className="lp-red" viewBox="0 0 100 100" preserveAspectRatio="none">
        {enlaces.map(([a, b]) => (
          <line
            key={`${a}-${b}`}
            x1={nodos[a]![0]}
            y1={nodos[a]![1]}
            x2={nodos[b]![0]}
            y2={nodos[b]![1]}
          />
        ))}
      </svg>
      <div className="lp-nodos">
        {nodos.map(([x, y], i) => (
          <span key={i} style={{ left: `${x}%`, top: `${y}%`, '--k': i } as CSSProperties} />
        ))}
      </div>
    </div>
  )
}

function Seccion({
  id,
  eyebrow,
  titulo,
  intro,
  children,
  tono = 'a',
  className = '',
}: {
  id?: string
  eyebrow: string
  titulo: ReactNode
  intro?: ReactNode
  children: ReactNode
  tono?: 'a' | 'b' | 'c'
  className?: string
}) {
  const tid = useId()
  return (
    <section id={id} className={`lp-banda lp-banda-${tono} ${className}`} aria-labelledby={tid}>
      <div className="lp-seccion">
        <div className="lp-cab lp-revela">
          <p className="lp-eyebrow">{eyebrow}</p>
          <h2 id={tid}>{titulo}</h2>
          {intro && <p className="lp-intro">{intro}</p>}
        </div>
        {children}
      </div>
    </section>
  )
}

function Qr({ className = '' }: { className?: string }) {
  return (
    <span className={`lp-qr ${className}`} aria-hidden="true">
      {CELDAS.map((on, i) => (
        <i key={i} data-on={on || undefined} />
      ))}
    </span>
  )
}

/** Escena del hero: flujo de autorización, invitación con QR y actividad, con parallax ligero. */
function EscenaHero() {
  const reducido = useMovimientoReducido()
  const [ciclo, setPaso] = useState(0)
  // Sin movimiento se muestra el flujo completo, sin ciclo.
  const paso = reducido ? FLUJO.length - 1 : ciclo
  const escena = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (reducido) return
    const id = window.setInterval(() => setPaso((p) => (p + 1) % FLUJO.length), 2200)
    return () => window.clearInterval(id)
  }, [reducido])

  function mover(e: PointerEvent<HTMLDivElement>) {
    const el = escena.current
    if (!el || reducido || e.pointerType !== 'mouse') return
    const r = el.getBoundingClientRect()
    el.style.setProperty('--px', ((e.clientX - r.left) / r.width - 0.5).toFixed(3))
    el.style.setProperty('--py', ((e.clientY - r.top) / r.height - 0.5).toFixed(3))
  }
  function salir() {
    escena.current?.style.setProperty('--px', '0')
    escena.current?.style.setProperty('--py', '0')
  }

  return (
    <div
      ref={escena}
      className="lp-escena"
      onPointerMove={mover}
      onPointerLeave={salir}
      role="img"
      aria-label="Ilustración con datos ficticios: una autorización avanza de solicitada a entrada registrada, junto a una invitación con código QR y la actividad reciente de una sede de ejemplo."
    >
      <div className="lp-capa lp-capa-flujo" aria-hidden="true">
        <div className="lp-flujo">
          <div className="lp-flujo-cab">
            <span className="lp-avatar">VI</span>
            <span>
              <b>Visitante de ejemplo</b>
              <small>Destino: Recepción · Sede demo</small>
            </span>
          </div>
          <ol>
            {FLUJO.map((f, i) => (
              <li key={f} data-estado={i < paso ? 'hecho' : i === paso ? 'actual' : undefined}>
                <span className="lp-flujo-marca">
                  {i <= paso ? <Icono nombre="check" className="lp-icono" /> : i + 1}
                </span>
                {f}
              </li>
            ))}
          </ol>
          <div className="lp-flujo-pie">
            <small>Vigencia</small>
            <b>Hoy · 08:00 – 17:00</b>
          </div>
        </div>
      </div>

      <div className="lp-capa lp-capa-qr" aria-hidden="true">
        <div className="lp-invitacion">
          <span className="lp-pase-etq">Invitación · ejemplo</span>
          <Qr />
          <small>Válida solo en la sede y el horario autorizados</small>
        </div>
      </div>

      <div className="lp-capa lp-capa-feed" aria-hidden="true">
        <div className="lp-feed">
          {ACTIVIDAD_DEMO.slice(0, 3).map((a) => (
            <div key={a.nombre} className="lp-feed-item">
              <span className="lp-avatar">{a.iniciales}</span>
              <span className="lp-demo-texto">
                <b>{a.nombre}</b>
                <small>{a.lugar}</small>
              </span>
              <span className={`lp-chip lp-chip-${a.tono}`}>{a.estado}</span>
            </div>
          ))}
        </div>
      </div>

      <svg
        className="lp-enlaces"
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
        aria-hidden="true"
      >
        <path d="M30 40 C 45 30, 55 30, 70 22" />
        <path d="M34 62 C 48 72, 58 74, 72 70" />
      </svg>
      <span className="lp-sello lp-sello-escena" aria-hidden="true">
        Datos de ejemplo
      </span>
    </div>
  )
}

/** Vista del producto: ventana de aplicación con una autorización y la actividad (ficticia). */
function VistaProducto() {
  const menu: NombreIcono[] = ['dashboard', 'personas', 'autorizaciones', 'visitas', 'accesos']
  return (
    <figure className="lp-ventana lp-revela" aria-label="Vista ilustrativa con datos ficticios">
      <div className="lp-ventana-barra" aria-hidden="true">
        <span />
        <span />
        <span />
        <b>ConecsIA · demostración</b>
        <span className="lp-sello">Datos de ejemplo</span>
      </div>
      <div className="lp-ventana-cuerpo">
        <nav className="lp-ventana-menu" aria-hidden="true">
          {menu.map((m, i) => (
            <span key={m} data-activo={i === 4 || undefined}>
              <Icono nombre={m} className="lp-icono" />
            </span>
          ))}
        </nav>
        <div className="lp-ventana-persona">
          <span className="lp-avatar lp-avatar-grande" aria-hidden="true">
            VI
          </span>
          <b>Visitante de ejemplo</b>
          <small>Documento ••••••</small>
          <dl>
            <div>
              <dt>Destino</dt>
              <dd>Recepción · Sede demo</dd>
            </div>
            <div>
              <dt>Zonas</dt>
              <dd>Lobby, Piso 2</dd>
            </div>
            <div>
              <dt>Vigencia</dt>
              <dd>Hoy · 08:00 – 17:00</dd>
            </div>
            <div>
              <dt>Autorizó</dt>
              <dd>Autorizador de ejemplo</dd>
            </div>
          </dl>
          <span className="lp-chip lp-chip-aprobada">
            <Icono nombre="check" className="lp-icono-mini" /> Vigente
          </span>
        </div>
        <div className="lp-ventana-actividad">
          <p className="lp-ventana-titulo">
            <span className="lp-pulso" aria-hidden="true" /> Actividad reciente
          </p>
          <ul>
            {ACTIVIDAD_DEMO.map((a) => (
              <li key={a.nombre}>
                <span className="lp-avatar" aria-hidden="true">
                  {a.iniciales}
                </span>
                <span className="lp-demo-texto">
                  <b>{a.nombre}</b>
                  <small>{a.lugar}</small>
                </span>
                <span className={`lp-chip lp-chip-${a.tono}`}>{a.estado}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
      <figcaption>
        Ilustración del producto. No corresponde a personas ni registros reales.
      </figcaption>
    </figure>
  )
}

export function PresentacionPage() {
  useRevelar()

  useEffect(() => {
    const previo = document.title
    document.title = 'ConecsIA – Gestión y control de accesos'
    return () => {
      document.title = previo
    }
  }, [])

  return (
    <div className="lp">
      <a href="#contenido" className="lp-saltar">
        Saltar al contenido
      </a>
      <Ambiente />
      <Encabezado />

      <main id="contenido">
        {/* 1. Qué es */}
        <section id="inicio" className="lp-hero" aria-labelledby="lp-titulo">
          <div className="lp-hero-copy">
            <p
              className="lp-eyebrow lp-chip-eyebrow lp-entra"
              style={{ '--d': 0 } as CSSProperties}
            >
              <span className="lp-pulso" aria-hidden="true" /> Plataforma de gestión y control de
              accesos
            </p>
            <h1 id="lp-titulo" className="lp-entra" style={{ '--d': 1 } as CSSProperties}>
              Cada acceso bajo control.{' '}
              <span className="lp-acento">Cada visita, mejor gestionada.</span>
            </h1>
            <p className="lp-sub lp-entra" style={{ '--d': 2 } as CSSProperties}>
              ConecsIA ayuda a organizaciones a administrar autorizaciones, registrar entradas y
              salidas, y consultar el historial de acceso desde una plataforma centralizada.
            </p>
            <div className="lp-ctas lp-entra" style={{ '--d': 3 } as CSSProperties}>
              <a href="#contacto" className="lp-btn lp-btn-pri">
                Solicitar información <Icono nombre="flecha" className="lp-icono lp-flecha" />
              </a>
              <a href="#como-funciona" className="lp-btn lp-btn-sec">
                Conocer cómo funciona
              </a>
            </div>
            <ul className="lp-hero-claves lp-entra" style={{ '--d': 4 } as CSSProperties}>
              <li>
                <Icono nombre="autorizaciones" className="lp-icono" /> Autorizaciones
              </li>
              <li>
                <Icono nombre="visitas" className="lp-icono" /> Visitas
              </li>
              <li>
                <Icono nombre="accesos" className="lp-icono" /> Entradas y salidas
              </li>
              <li>
                <Icono nombre="reportes" className="lp-icono" /> Trazabilidad
              </li>
            </ul>
          </div>
          <div className="lp-entra lp-hero-visual" style={{ '--d': 2 } as CSSProperties}>
            <EscenaHero />
          </div>
          <a href="#reto" className="lp-bajar" aria-label="Ir a la siguiente sección">
            <span />
          </a>
        </section>

        {/* 1b. Problema que resuelve: comparativa Hoy → Con ConecsIA */}
        <ComparativaRetos />

        {/* 2. Cómo funciona: recorrido conectado */}
        <Seccion
          id="como-funciona"
          tono="a"
          className="lp-centrado lp-banda-recorrido"
          eyebrow="Cómo funciona"
          titulo={
            <>
              De la autorización al historial, en{' '}
              <span className="lp-acento-lima">cinco pasos</span>
            </>
          }
          intro="Las opciones disponibles pueden variar según la configuración de cada organización."
        >
          <RecorridoPasos />
        </Seccion>

        {/* 3. Servicios: bento asimétrico */}
        <Seccion
          id="servicios"
          tono="c"
          className="lp-centrado lp-banda-servicios"
          eyebrow="Servicios"
          titulo={
            <>
              Todo lo necesario para <span className="lp-acento">controlar el ingreso</span>
            </>
          }
        >
          <ServiciosBento />
        </Seccion>

        {/* 4. Para quién: carrusel con tarjeta central */}
        <Seccion
          tono="b"
          className="lp-banda-sectores"
          eyebrow="Para quién"
          titulo={
            <>
              Una base común, configurable para{' '}
              <span className="lp-acento-lima">cada organización</span>
            </>
          }
          intro="La terminología, los catálogos y las políticas se ajustan al funcionamiento de cada entidad. Los ejemplos son ilustrativos."
        >
          <CarruselSectores />
        </Seccion>

        {/* 5. Beneficios: composición asimétrica */}
        <BeneficiosSection />

        <Seccion
          id="beneficios-antiguo"
          tono="a"
          className="lp-asim lp-beneficios-antiguo"
          eyebrow="Beneficios"
          titulo="Más claridad en cada ingreso"
          intro="Ventajas que se derivan directamente de cómo funciona la plataforma."
        >
          <ul className="lp-beneficios">
            {BENEFICIOS.map((b, i) => (
              <li
                key={b.titulo}
                className={`lp-revela ${i === 0 ? 'lp-beneficio-mayor' : ''}`}
                style={{ '--k': i % 4 } as CSSProperties}
              >
                <Icono nombre="check" className="lp-check" />
                <span>
                  <b>{b.titulo}</b>
                  <small>{b.texto}</small>
                </span>
              </li>
            ))}
          </ul>
        </Seccion>

        {/* Vista del producto */}
        <Seccion
          tono="c"
          eyebrow="Así se ve"
          titulo="La actividad de tus sedes, de un vistazo"
          intro="Quién está autorizado, a dónde va, hasta cuándo y qué movimientos hubo."
        >
          <VistaProducto />
        </Seccion>

        {/* FAQ */}
        <PreguntasFrecuentes />

        {/* 6. Contacto */}
        <section
          id="contacto"
          className="lp-banda lp-banda-contacto"
          aria-labelledby="lp-contacto-titulo"
        >
          <div className="lp-seccion lp-contacto">
            <div className="lp-contacto-copy lp-revela">
              <p className="lp-pildora lp-pildora-punto">Contacto</p>
              <h2 id="lp-contacto-titulo">
                Hablemos sobre cómo <span className="lp-acento-cian">gestionar los accesos</span> de
                tu organización.
              </h2>
              <p className="lp-intro">
                Déjanos tus datos y te contactaremos para mostrarte la plataforma.
              </p>
              <ol className="lp-contacto-pasos" aria-label="Qué sigue">
                <li>Recibimos tu solicitud</li>
                <li>Te contactamos por correo</li>
                <li>Coordinamos una presentación</li>
              </ol>
              <div className="lp-contacto-ilus" aria-hidden="true">
                <span className="lp-contacto-orbita" />
                <span className="lp-contacto-ficha">
                  <Icono nombre="perfil" className="lp-icono" />
                  <i />
                  <i />
                </span>
                <span className="lp-contacto-burbuja">
                  <i />
                  <i />
                  <i />
                </span>
              </div>
            </div>
            <FormularioContacto />
          </div>
        </section>
      </main>

      <footer className="lp-footer">
        <div className="lp-footer-marca">
          <Marca iaResaltada />
          <p>
            Plataforma para gestionar autorizaciones, visitas y el registro de entradas y salidas.
          </p>
        </div>
        <nav aria-label="Pie de página">
          <ul>
            {SECCIONES_NAV.map((s) => (
              <li key={s.id}>
                <a href={`#${s.id}`}>{s.texto}</a>
              </li>
            ))}
            <li className="lp-footer-ingresar">
              <Link to="/ingresar">Iniciar sesión</Link>
            </li>
          </ul>
        </nav>
        <p className="lp-footer-legal">
          <small>© {ANIO} ConecsIA</small>
          <span aria-hidden="true">|</span>
          <small>
            Elaborado por <span className="lp-footer-autor">DataGlobal</span>
          </small>
        </p>
      </footer>
    </div>
  )
}
