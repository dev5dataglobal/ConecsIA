import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import type { CSSProperties } from 'react'
import { BENEFICIOS } from './contenido'

const ICONOS: NombreIcono[] = [
  'dashboard',
  'autorizaciones',
  'accesos',
  'check',
  'reloj',
  'configuracion',
  'documento',
]

const ACENTOS = ['violeta', 'lima', 'cian', 'violeta', 'lima', 'cian', 'violeta'] as const

function RedDeAccesos() {
  return (
    <figure className="lp-beneficios-red" aria-hidden="true">
      <div className="lp-beneficios-red-halo" />
      <svg className="lp-beneficios-red-lineas" viewBox="0 0 520 360" preserveAspectRatio="none">
        <path d="M82 82 C 152 62, 174 108, 238 166" />
        <path d="M438 80 C 368 64, 346 108, 282 166" />
        <path d="M86 286 C 156 304, 180 254, 238 198" />
        <path d="M434 286 C 364 304, 340 254, 282 198" />
        <path d="M238 166 C 246 148, 274 148, 282 166" />
      </svg>
      <span className="lp-beneficios-nodo lp-beneficios-nodo-persona">
        <Icono nombre="personas" />
      </span>
      <span className="lp-beneficios-nodo lp-beneficios-nodo-sede">
        <Icono nombre="sede" />
      </span>
      <span className="lp-beneficios-nodo lp-beneficios-nodo-acceso">
        <Icono nombre="accesos" />
      </span>
      <span className="lp-beneficios-nodo lp-beneficios-nodo-historial">
        <Icono nombre="documento" />
      </span>
      <span className="lp-beneficios-nucleo">
        <span className="lp-beneficios-nucleo-capa" />
        <Icono nombre="dashboard" />
      </span>
      <span className="lp-beneficios-punto lp-beneficios-punto-1" />
      <span className="lp-beneficios-punto lp-beneficios-punto-2" />
      <span className="lp-beneficios-punto lp-beneficios-punto-3" />
      <span className="lp-beneficios-punto lp-beneficios-punto-4" />
    </figure>
  )
}

export function BeneficiosSection() {
  return (
    <section
      id="beneficios"
      className="lp-banda lp-banda-a lp-banda-beneficios"
      aria-labelledby="beneficios-titulo"
    >
      <div className="lp-seccion lp-beneficios-layout">
        <div className="lp-beneficios-intro">
          <div className="lp-cab lp-revela">
            <p className="lp-eyebrow">BENEFICIOS</p>
            <h2 id="beneficios-titulo">Más claridad en cada ingreso</h2>
            <p className="lp-intro">
              Ventajas que se derivan directamente de cómo funciona la plataforma.
            </p>
          </div>
          <RedDeAccesos />
        </div>
        <ul className="lp-beneficios" aria-label="Beneficios de ConecsIA">
          {BENEFICIOS.map((beneficio, i) => (
            <li
              key={beneficio.titulo}
              className={`lp-beneficio lp-beneficio-${ACENTOS[i]} ${i === 0 ? 'lp-beneficio-mayor' : ''} lp-revela`}
              style={{ '--k': i % 6 } as CSSProperties}
            >
              <span className="lp-beneficio-icono">
                <Icono nombre={ICONOS[i]!} />
              </span>
              <span className="lp-beneficio-copy">
                <b>{beneficio.titulo}</b>
                <small>{beneficio.texto}</small>
              </span>
              <span className="lp-beneficio-brillo" aria-hidden="true" />
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}
