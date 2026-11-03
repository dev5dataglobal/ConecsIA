import { useEffect, useId, useRef, useState, type KeyboardEvent, type ReactNode } from 'react'
import './selector.css'

export interface OpcionSelector {
  valor: string
  texto: string
  deshabilitada?: boolean
}

/**
 * Selector con lista personalizada (patrón listbox de WAI-ARIA, «select-only combobox»).
 * Teclado: Flechas/Inicio/Fin recorren, Enter/Espacio eligen, Escape o Tab cierran, una letra salta a la opción.
 * Se cierra al elegir o al hacer clic fuera, y el foco vuelve al botón.
 * Con `buscable` muestra un campo de búsqueda (sin distinguir tildes ni mayúsculas) para listas largas;
 * el foco queda en el campo y las flechas/Enter/Escape siguen recorriendo y eligiendo.
 */
export function Selector({ etiqueta, valor, opciones, onChange, deshabilitado, invalido, icono, className, buscable, placeholderBusqueda }: {
  etiqueta: string
  /** Marca de error de validación (aria-invalid). */
  invalido?: boolean
  /** Icono decorativo a la izquierda del valor. */
  icono?: ReactNode
  valor: string
  opciones: OpcionSelector[]
  onChange: (valor: string) => void
  deshabilitado?: boolean
  className?: string
  /** Agrega un campo de búsqueda sobre la lista (para catálogos largos). */
  buscable?: boolean
  placeholderBusqueda?: string
}) {
  const id = useId()
  const [abierto, setAbierto] = useState(false)
  const [activo, setActivo] = useState(0)
  const [arriba, setArriba] = useState(false)
  const raiz = useRef<HTMLDivElement>(null)
  const boton = useRef<HTMLButtonElement>(null)
  const lista = useRef<HTMLUListElement>(null)
  const campo = useRef<HTMLInputElement>(null)
  const [busqueda, setBusqueda] = useState('')
  const seleccionada = opciones.find((o) => o.valor === valor)
  const termino = normalizar(busqueda.trim())
  const visibles = opciones.map((o, i) => (!termino || normalizar(o.texto).includes(termino) ? i : -1)).filter((i) => i >= 0)
  const habilitadas = visibles.filter((i) => !opciones[i]?.deshabilitada)

  useEffect(() => {
    if (!abierto) return
    const alPulsarFuera = (event: MouseEvent) => {
      if (raiz.current && !raiz.current.contains(event.target as Node)) setAbierto(false)
    }
    document.addEventListener('mousedown', alPulsarFuera)
    if (buscable) campo.current?.focus()
    else lista.current?.focus()
    return () => document.removeEventListener('mousedown', alPulsarFuera)
  }, [abierto, buscable])

  useEffect(() => {
    if (abierto) lista.current?.querySelector<HTMLElement>(`[data-indice="${activo}"]`)?.scrollIntoView?.({ block: 'nearest' })
  }, [abierto, activo])

  function abrir() {
    if (deshabilitado) return
    const actual = opciones.findIndex((o) => o.valor === valor)
    const todas = opciones.map((o, i) => (o.deshabilitada ? -1 : i)).filter((i) => i >= 0)
    setBusqueda('')
    setActivo(actual >= 0 && !opciones[actual]?.deshabilitada ? actual : todas[0] ?? 0)
    setArriba(espacioInsuficienteAbajo(boton.current))
    setAbierto(true)
  }

  function cerrar(devolverFoco = true) {
    setAbierto(false)
    if (devolverFoco) boton.current?.focus()
  }

  function elegir(indice: number) {
    const opcion = opciones[indice]
    if (!opcion || opcion.deshabilitada) return
    if (opcion.valor !== valor) onChange(opcion.valor)
    cerrar()
  }

  function mover(paso: number) {
    const posicion = habilitadas.indexOf(activo)
    const siguiente = habilitadas[Math.min(habilitadas.length - 1, Math.max(0, posicion + paso))]
    if (siguiente !== undefined) setActivo(siguiente)
  }

  function buscarPorLetra(letra: string) {
    const inicio = habilitadas.indexOf(activo)
    const orden = [...habilitadas.slice(inicio + 1), ...habilitadas.slice(0, inicio + 1)]
    const encontrada = orden.find((i) => opciones[i]?.texto.toLocaleLowerCase('es').startsWith(letra.toLocaleLowerCase('es')))
    if (encontrada !== undefined) setActivo(encontrada)
  }

  function teclaBoton(event: KeyboardEvent<HTMLButtonElement>) {
    if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
      event.preventDefault()
      abrir()
    }
  }

  function buscar(texto: string) {
    setBusqueda(texto)
    const t = normalizar(texto.trim())
    const primera = opciones.findIndex((o) => !o.deshabilitada && (!t || normalizar(o.texto).includes(t)))
    if (primera >= 0) setActivo(primera)
  }

  function teclaCampo(event: KeyboardEvent<HTMLInputElement>) {
    switch (event.key) {
      case 'ArrowDown': event.preventDefault(); mover(1); break
      case 'ArrowUp': event.preventDefault(); mover(-1); break
      case 'Enter': event.preventDefault(); if (habilitadas.includes(activo)) elegir(activo); break
      case 'Escape': event.preventDefault(); event.stopPropagation(); event.nativeEvent.stopImmediatePropagation(); cerrar(); break
      case 'Tab': cerrar(false); break
    }
  }

  function teclaLista(event: KeyboardEvent<HTMLUListElement>) {
    switch (event.key) {
      case 'ArrowDown': event.preventDefault(); mover(1); break
      case 'ArrowUp': event.preventDefault(); mover(-1); break
      case 'Home': event.preventDefault(); setActivo(habilitadas[0] ?? 0); break
      case 'End': event.preventDefault(); setActivo(habilitadas.at(-1) ?? 0); break
      case 'Enter':
      case ' ': event.preventDefault(); elegir(activo); break
      // Escape solo cierra la lista, no el diálogo que la contiene.
      case 'Escape': event.preventDefault(); event.stopPropagation(); event.nativeEvent.stopImmediatePropagation(); cerrar(); break
      case 'Tab': cerrar(false); break
      default:
        if (event.key.length === 1 && /\S/.test(event.key)) buscarPorLetra(event.key)
    }
  }

  return (
    // preventDefault evita que un <label> contenedor reenvíe el clic al botón (abriría y cerraría a la vez).
    <div className={`sel ${className ?? ''}`} ref={raiz} data-abierto={abierto || undefined} onClick={(event) => event.preventDefault()}>
      <button
        ref={boton}
        type="button"
        className="sel-boton"
        aria-haspopup="listbox"
        aria-expanded={abierto}
        aria-controls={`${id}-lista`}
        aria-label={`${etiqueta}: ${seleccionada?.texto ?? 'sin selección'}`}
        disabled={deshabilitado}
        aria-invalid={invalido || undefined}
        data-vacio={seleccionada?.deshabilitada || undefined}
        onClick={() => (abierto ? cerrar() : abrir())}
        onKeyDown={teclaBoton}
      >
        {icono && <span className="sel-icono" aria-hidden="true">{icono}</span>}
        <span className="sel-valor">{seleccionada?.texto ?? 'Seleccionar'}</span>
        <svg className="sel-chevron" viewBox="0 0 24 24" aria-hidden="true"><path d="m6 9 6 6 6-6" /></svg>
      </button>
      {abierto && !buscable && (
        <ul
          ref={lista}
          id={`${id}-lista`}
          className="sel-lista"
          data-arriba={(!buscable && arriba) || undefined}
          role="listbox"
          tabIndex={-1}
          aria-label={etiqueta}
          aria-activedescendant={habilitadas.includes(activo) ? `${id}-op-${activo}` : undefined}
          onKeyDown={teclaLista}
        >
          {visibles.map((indice) => { const opcion = opciones[indice]!; return (
            <li
              key={opcion.valor}
              id={`${id}-op-${indice}`}
              data-indice={indice}
              role="option"
              aria-selected={opcion.valor === valor}
              aria-disabled={opcion.deshabilitada || undefined}
              data-activo={indice === activo || undefined}
              className="sel-opcion"
              onMouseEnter={() => !opcion.deshabilitada && setActivo(indice)}
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => elegir(indice)}
            >
              <span>{opcion.texto}</span>
              <svg className="sel-check" viewBox="0 0 24 24" aria-hidden="true"><path d="m5 12.5 4.5 4.5L19 7.5" /></svg>
            </li>
          ) })}
          {visibles.length === 0 && <li className="sel-sin-resultados" role="presentation">Sin resultados</li>}
        </ul>
      )}
      {abierto && buscable && (
        <div className="sel-panel" data-arriba={arriba || undefined}>
          <input
            ref={campo}
            className="sel-buscar"
            type="search"
            role="combobox"
            aria-expanded="true"
            aria-controls={`${id}-lista`}
            aria-autocomplete="list"
            aria-activedescendant={habilitadas.includes(activo) ? `${id}-op-${activo}` : undefined}
            aria-label={`Buscar ${etiqueta.toLocaleLowerCase('es')}`}
            placeholder={placeholderBusqueda ?? 'Buscar…'}
            value={busqueda}
            onChange={(event) => buscar(event.target.value)}
            onKeyDown={teclaCampo}
          />
          <ul
            ref={lista}
            id={`${id}-lista`}
            className="sel-lista"
            role="listbox"
            aria-label={etiqueta}
            aria-activedescendant={habilitadas.includes(activo) ? `${id}-op-${activo}` : undefined}
          >
            {visibles.map((indice) => { const opcion = opciones[indice]!; return (
              <li
                key={opcion.valor}
                id={`${id}-op-${indice}`}
                data-indice={indice}
                role="option"
                aria-selected={opcion.valor === valor}
                aria-disabled={opcion.deshabilitada || undefined}
                data-activo={indice === activo || undefined}
                className="sel-opcion"
                onMouseEnter={() => !opcion.deshabilitada && setActivo(indice)}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => elegir(indice)}
              >
                <span>{opcion.texto}</span>
                <svg className="sel-check" viewBox="0 0 24 24" aria-hidden="true"><path d="m5 12.5 4.5 4.5L19 7.5" /></svg>
              </li>
            ) })}
            {visibles.length === 0 && <li className="sel-sin-resultados" role="presentation">Sin resultados</li>}
          </ul>
        </div>
      )}
    </div>
  )
}

/** Abre la lista hacia arriba si debajo (hasta el borde del contenedor desplazable o de la ventana) no cabe y arriba hay más sitio. */
function espacioInsuficienteAbajo(boton: HTMLElement | null) {
  if (!boton) return false
  const r = boton.getBoundingClientRect()
  let limiteInferior = window.innerHeight
  let limiteSuperior = 0
  for (let el = boton.parentElement; el; el = el.parentElement) {
    if (/(auto|scroll|hidden)/.test(getComputedStyle(el).overflowY)) {
      const c = el.getBoundingClientRect()
      limiteInferior = Math.min(limiteInferior, c.bottom)
      limiteSuperior = Math.max(limiteSuperior, c.top)
      break
    }
  }
  const abajo = limiteInferior - r.bottom
  return abajo < 220 && r.top - limiteSuperior > abajo
}

/** Texto comparable: minúsculas y sin tildes («Bogotá» coincide con «bogota»). */
function normalizar(texto: string) {
  return texto.toLocaleLowerCase('es').normalize('NFD').replace(/[̀-ͯ]/g, '')
}
