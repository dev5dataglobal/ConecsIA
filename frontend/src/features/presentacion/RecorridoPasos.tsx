import type { CSSProperties } from 'react'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { PASOS } from './contenido'

/** Apariencia de cada etapa: iconos (principal y secundario), color de acento y altura del nodo en la onda (%). */
const ETAPAS: { icono: NombreIcono; apoyo: NombreIcono; rgb: string; y: number }[] = [
  { icono: 'personas', apoyo: 'sede', rgb: '157 132 255', y: 30 },
  { icono: 'autorizaciones', apoyo: 'documento', rgb: '124 120 255', y: 62 },
  { icono: 'documento', apoyo: 'check', rgb: '86 196 255', y: 54 },
  { icono: 'accesos', apoyo: 'perfil', rgb: '150 230 120', y: 66 },
  { icono: 'reportes', apoyo: 'reloj', rgb: '201 242 107', y: 38 },
]

/** x del centro de cada columna (columnas iguales, sin separación). */
const X = ETAPAS.map((_, i) => ((i + 0.5) * 100) / ETAPAS.length)

/** Curva suave que pasa por los nodos: controles horizontales a mitad de tramo. */
const TRAYECTO =
  ETAPAS.reduce((d, e, i) => {
    if (i === 0)
      return `M0 ${e.y + 18} C ${X[0]! / 2} ${e.y + 18}, ${X[0]! / 2} ${e.y}, ${X[0]} ${e.y}`
    const xm = (X[i - 1]! + X[i]!) / 2
    return `${d} C ${xm} ${ETAPAS[i - 1]!.y}, ${xm} ${e.y}, ${X[i]} ${e.y}`
  }, '') +
  ` C ${(X[4]! + 100) / 2} ${ETAPAS[4]!.y}, ${(X[4]! + 100) / 2} ${ETAPAS[4]!.y - 20}, 100 ${ETAPAS[4]!.y - 20}`

/** «Cómo funciona»: recorrido horizontal conectado en escritorio, secuencia vertical en móvil. */
export function RecorridoPasos() {
  return (
    <div className="lp-recorrido lp-revela">
      <svg
        className="lp-trayecto"
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
        aria-hidden="true"
      >
        <defs>
          <linearGradient id="lp-trayecto-grad" x1="0" x2="1" y1="0" y2="0">
            <stop offset="0" stopColor="#8b6cff" />
            <stop offset="0.5" stopColor="#56c4ff" />
            <stop offset="1" stopColor="#c9f26b" />
          </linearGradient>
        </defs>
        <path className="lp-trayecto-halo" d={TRAYECTO} />
        <path className="lp-trayecto-linea" d={TRAYECTO} pathLength={1} />
      </svg>
      {X.slice(0, -1).map((x, i) => (
        <span
          key={x}
          className="lp-chevron"
          style={
            {
              left: `${(x + X[i + 1]!) / 2}%`,
              top: `calc(${(ETAPAS[i]!.y + ETAPAS[i + 1]!.y) / 2} * var(--alto-tray) / 100)`,
              '--rgb': ETAPAS[i + 1]!.rgb,
            } as CSSProperties
          }
          aria-hidden="true"
        >
          <Icono nombre="flecha" className="lp-icono" />
        </span>
      ))}

      <ol className="lp-pasos-v2">
        {PASOS.map((p, i) => {
          const e = ETAPAS[i]!
          return (
            <li
              key={p.titulo}
              className="lp-paso2"
              style={{ '--k': i, '--rgb': e.rgb, '--y': e.y } as CSSProperties}
            >
              <span className="lp-nodo" aria-hidden="true">
                {String(i + 1).padStart(2, '0')}
              </span>
              <article className="lp-paso2-tarjeta">
                <div className="lp-paso2-visual" aria-hidden="true">
                  <span className="lp-paso2-base" />
                  <span className="lp-paso2-ficha">
                    <Icono nombre={e.icono} className="lp-icono" />
                  </span>
                  <span className="lp-paso2-apoyo">
                    <Icono nombre={e.apoyo} className="lp-icono" />
                  </span>
                </div>
                <h3>
                  <span className="lp-oculto">
                    Paso {i + 1} de {PASOS.length}:{' '}
                  </span>
                  {p.titulo}
                </h3>
                <p>{p.texto}</p>
                <span className="lp-paso2-barra" aria-hidden="true" />
              </article>
            </li>
          )
        })}
      </ol>
    </div>
  )
}
