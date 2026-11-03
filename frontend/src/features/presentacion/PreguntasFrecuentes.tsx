import { useState } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { PREGUNTAS } from './contenido'

function IlustracionPreguntas() {
  return (
    <figure className="lp-faq-ilustracion" aria-hidden="true">
      <div className="lp-faq-ilustracion-halo" />
      <svg className="lp-faq-orbitas" viewBox="0 0 480 310" preserveAspectRatio="none">
        <ellipse cx="240" cy="160" rx="188" ry="88" />
        <ellipse cx="240" cy="160" rx="126" ry="142" transform="rotate(-28 240 160)" />
      </svg>
      <span className="lp-faq-burbuja lp-faq-burbuja-principal">
        <span className="lp-faq-pregunta">?</span>
        <i />
        <i />
        <i />
      </span>
      <span className="lp-faq-burbuja lp-faq-burbuja-secundaria">
        <i />
        <i />
        <i />
      </span>
      <span className="lp-faq-nodo lp-faq-nodo-1">
        <Icono nombre="mensaje" />
      </span>
      <span className="lp-faq-nodo lp-faq-nodo-2">
        <Icono nombre="documento" />
      </span>
      <span className="lp-faq-nodo lp-faq-nodo-3">
        <Icono nombre="check" />
      </span>
      <span className="lp-faq-punto lp-faq-punto-1" />
      <span className="lp-faq-punto lp-faq-punto-2" />
      <span className="lp-faq-punto lp-faq-punto-3" />
    </figure>
  )
}

export function PreguntasFrecuentes() {
  const [abiertas, setAbiertas] = useState<Set<number>>(new Set([0]))

  function alternar(indice: number) {
    setAbiertas((actuales) => {
      const siguiente = new Set(actuales)
      if (siguiente.has(indice)) siguiente.delete(indice)
      else siguiente.add(indice)
      return siguiente
    })
  }

  return (
    <section
      id="preguntas-frecuentes"
      className="lp-banda lp-banda-faq"
      aria-labelledby="lp-faq-titulo"
    >
      <div className="lp-seccion lp-faq-layout">
        <div className="lp-faq-intro lp-revela">
          <p className="lp-eyebrow">PREGUNTAS FRECUENTES</p>
          <h2 id="lp-faq-titulo">Lo que suelen preguntarnos</h2>
          <p className="lp-intro">Encuentra respuestas rápidas sobre la plataforma.</p>
          <IlustracionPreguntas />
          <a href="#contacto" className="lp-faq-contacto">
            <span className="lp-faq-contacto-icono">
              <Icono nombre="mensaje" />
            </span>
            <span>¿Tienes otra consulta? Contáctanos</span>
            <Icono nombre="flecha" className="lp-faq-contacto-flecha" />
          </a>
        </div>

        <div className="lp-faq" aria-label="Preguntas frecuentes">
          {PREGUNTAS.map((pregunta, indice) => {
            const abierta = abiertas.has(indice)
            const respuestaId = `lp-faq-respuesta-${indice}`
            return (
              <article
                key={pregunta.p}
                className={`lp-faq-item ${abierta ? 'lp-faq-item-abierto' : ''}`}
              >
                <h3>
                  <button
                    type="button"
                    className="lp-faq-trigger"
                    aria-expanded={abierta}
                    aria-controls={respuestaId}
                    onClick={() => alternar(indice)}
                  >
                    <span>{pregunta.p}</span>
                    <span className="lp-faq-signo" aria-hidden="true" />
                  </button>
                </h3>
                <div
                  id={respuestaId}
                  className={`lp-faq-respuesta ${abierta ? 'lp-faq-respuesta-abierta' : ''}`}
                  aria-hidden={!abierta}
                >
                  <p>{pregunta.r}</p>
                </div>
              </article>
            )
          })}
        </div>
      </div>
    </section>
  )
}
