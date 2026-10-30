import { useRef, useState, type CSSProperties, type KeyboardEvent, type PointerEvent } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { SECTORES } from './contenido'
import { IlustracionSector } from './IlustracionSector'

/** Distancia circular más corta entre i y el activo, en [-n/2, n/2]. */
function desfase(i: number, activo: number, n: number) {
  let d = (i - activo) % n
  if (d > n / 2) d -= n
  if (d < -n / 2) d += n
  return d
}

/**
 * Carrusel «Para quién»: tarjeta activa al centro, vecinas parcialmente visibles y ciclo continuo
 * entre la última y la primera. Botones, indicadores, flechas del teclado y gesto de deslizar.
 * Sin avance automático.
 */
export function CarruselSectores() {
  const n = SECTORES.length
  const [activo, setActivo] = useState(0)
  const gesto = useRef<{ x: number; y: number; id: number } | null>(null)

  const ir = (i: number) => setActivo(((i % n) + n) % n)

  function tecla(e: KeyboardEvent) {
    if (e.key === 'ArrowRight') ir(activo + 1)
    else if (e.key === 'ArrowLeft') ir(activo - 1)
    else if (e.key === 'Home') ir(0)
    else if (e.key === 'End') ir(n - 1)
    else return
    e.preventDefault()
  }

  // Deslizar (táctil o ratón): solo cuenta un gesto mayormente horizontal de más de 40 px.
  function bajar(e: PointerEvent<HTMLDivElement>) {
    if (e.pointerType === 'mouse' && e.button !== 0) return
    gesto.current = { x: e.clientX, y: e.clientY, id: e.pointerId }
  }
  function soltar(e: PointerEvent<HTMLDivElement>) {
    const g = gesto.current
    gesto.current = null
    if (!g || g.id !== e.pointerId) return
    const dx = e.clientX - g.x
    const dy = e.clientY - g.y
    if (Math.abs(dx) > 40 && Math.abs(dx) > Math.abs(dy)) ir(activo + (dx < 0 ? 1 : -1))
  }

  const actual = SECTORES[activo]!

  return (
    <div
      className="lp-cs"
      role="region"
      aria-roledescription="carrusel"
      aria-label="Tipos de organización"
    >
      <div
        className="lp-cs-escenario"
        tabIndex={0}
        aria-label="Tipos de organización: usa las flechas izquierda y derecha para cambiar"
        onKeyDown={tecla}
        onPointerDown={bajar}
        onPointerUp={soltar}
        onPointerCancel={() => (gesto.current = null)}
      >
        {SECTORES.map((s, i) => {
          const d = desfase(i, activo, n)
          const visible = Math.abs(d) <= 2
          const central = d === 0
          return (
            <article
              key={s.nombre}
              className={`lp-cs-tarjeta lp-cs-${s.acento}`}
              data-central={central || undefined}
              data-oculta={!visible || undefined}
              style={{ '--d': d, '--ad': Math.abs(d) } as CSSProperties}
              role="group"
              aria-roledescription="diapositiva"
              aria-label={`${i + 1} de ${n}: ${s.nombre}`}
              aria-hidden={!central || undefined}
              onClick={central ? undefined : () => ir(i)}
            >
              <IlustracionSector escena={s.escena} />
              <span className="lp-cs-velo" aria-hidden="true" />
              <Icono nombre={s.icono} className="lp-cs-icono" />
              {central && <span className="lp-cs-insignia">Seleccionado</span>}
              <div className="lp-cs-texto">
                <h3>{s.nombre}</h3>
                <p>{s.ejemplo}</p>
              </div>
            </article>
          )
        })}
      </div>

      <p className="lp-oculto" aria-live="polite">
        {actual.nombre}, {activo + 1} de {n}
      </p>

      <div className="lp-cs-controles">
        <div className="lp-cs-puntos">
          {SECTORES.map((s, i) => (
            <button
              key={s.nombre}
              type="button"
              aria-label={`Ver ${s.nombre}`}
              aria-current={i === activo || undefined}
              onClick={() => ir(i)}
            />
          ))}
        </div>
        <span className="lp-cs-contador" aria-hidden="true">
          <b>{activo + 1}</b> / {n}
        </span>
        <button
          type="button"
          className="lp-cs-btn"
          aria-label="Tipo de organización anterior"
          onClick={() => ir(activo - 1)}
        >
          <Icono nombre="flecha" className="lp-icono lp-girar" />
        </button>
        <button
          type="button"
          className="lp-cs-btn lp-cs-btn-sig"
          aria-label="Tipo de organización siguiente"
          onClick={() => ir(activo + 1)}
        >
          <Icono nombre="flecha" className="lp-icono" />
        </button>
      </div>
    </div>
  )
}
