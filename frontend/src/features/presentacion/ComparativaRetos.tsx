import type { CSSProperties } from 'react'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { RETOS } from './contenido'

/** Composición decorativa: fuentes dispersas (planilla, mensaje) que convergen en una persona registrada. */
function IlustracionDispersion() {
  const fichas: { icono: NombreIcono; clase: string }[] = [
    { icono: 'documento', clase: 'lp-ficha-1' },
    { icono: 'mensaje', clase: 'lp-ficha-2' },
    { icono: 'perfil', clase: 'lp-ficha-3' },
  ]
  return (
    <div className="lp-dispersion lp-revela" aria-hidden="true">
      <svg viewBox="0 0 320 160" preserveAspectRatio="none">
        <path d="M30 150 C 70 120, 60 70, 110 78 S 170 120, 220 92 S 290 60, 318 40" />
        <path d="M0 110 C 60 100, 90 140, 150 128 S 250 150, 320 120" />
      </svg>
      <span className="lp-plataforma" />
      {fichas.map((f) => (
        <span key={f.clase} className={`lp-ficha ${f.clase}`}>
          <Icono nombre={f.icono} className="lp-icono" />
        </span>
      ))}
    </div>
  )
}

/** Sección «El reto»: mensaje a la izquierda y pares Hoy → Con ConecsIA a la derecha. */
export function ComparativaRetos() {
  return (
    <section id="reto" className="lp-banda lp-banda-reto" aria-labelledby="lp-reto-titulo">
      <div className="lp-seccion lp-reto-grid">
        <div className="lp-reto-mensaje">
          <p className="lp-pildora lp-revela">El reto</p>
          <h2 id="lp-reto-titulo" className="lp-revela">
            Gestionar accesos no debería depender de la{' '}
            <span className="lp-violeta">memoria de la portería</span>
          </h2>
          <p className="lp-intro lp-revela">
            Lo que hoy está disperso, ConecsIA lo ordena en un solo flujo.
          </p>
          <IlustracionDispersion />
        </div>

        <div className="lp-comparativa">
          <div className="lp-comp-cab" aria-hidden="true">
            <span>Hoy</span>
            <span>
              Con <b>ConecsIA</b>
            </span>
          </div>
          <ol className="lp-comp-lista">
            {RETOS.map((r, i) => (
              <li
                key={r.reto}
                className="lp-comp-fila lp-revela"
                style={{ '--k': i } as CSSProperties}
              >
                <div className="lp-comp-antes">
                  <span className="lp-comp-icono">
                    <Icono nombre={r.icono} className="lp-icono" />
                  </span>
                  <p>
                    <span className="lp-comp-etq">Hoy</span>
                    <span className="lp-oculto">: </span>
                    {r.reto}
                  </p>
                </div>
                <span className="lp-comp-flecha" aria-hidden="true">
                  <span />
                  <Icono nombre="flecha" className="lp-icono" />
                </span>
                <div className="lp-comp-despues">
                  <span className="lp-comp-check">
                    <Icono nombre="check" className="lp-icono" />
                  </span>
                  <p>
                    <span className="lp-comp-etq">Con ConecsIA</span>
                    <span className="lp-oculto">: </span>
                    {r.respuesta}
                  </p>
                </div>
              </li>
            ))}
          </ol>
        </div>
      </div>
    </section>
  )
}
