import { useEffect, useId, type FormEvent, type ReactNode } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { mensajeError } from './utilidades'

/** Piezas compartidas por las secciones de administración (mismo lenguaje visual del dashboard). */

export function Cabecera({ titulo, descripcion, accion, children }: { titulo: string; descripcion: string; accion?: ReactNode; children?: ReactNode }) {
  return (
    <header className="ad-hero">
      <div className="ad-hero-fondo" aria-hidden="true" />
      <div className="ad-hero-fila">
        <div>
          <nav className="ad-migas" aria-label="Ruta de navegación">
            Inicio <span aria-hidden="true">/</span> Administración
          </nav>
          <h1>{titulo}</h1>
          <p>{descripcion}</p>
        </div>
        {accion}
      </div>
      {children}
    </header>
  )
}

/** Página de una sección: cabecera oscura con pestañas + tarjeta de contenido. */
export function Marco({ titulo, descripcion, accion, pestanas, children }: { titulo: string; descripcion: string; accion?: ReactNode; pestanas: ReactNode; children: ReactNode }) {
  return (
    <div className="ad">
      <Cabecera titulo={titulo} descripcion={descripcion} accion={accion}>{pestanas}</Cabecera>
      <section id="ad-panel" className="ad-tarjeta" aria-label={titulo}>{children}</section>
    </div>
  )
}

export function BotonNuevo({ texto, onClick, disabled }: { texto: string; onClick: () => void; disabled?: boolean }) {
  return (
    <button className="ad-boton" type="button" onClick={onClick} disabled={disabled}>
      <span aria-hidden="true">+</span> {texto}
    </button>
  )
}

export function Estatus({ activo, texto }: { activo: boolean; texto?: string }) {
  return <span className="ad-estatus" data-tono={activo ? 'activo' : 'inactivo'}>{texto ?? (activo ? 'Activo' : 'Inactivo')}</span>
}

export function EstadoConsulta({ loading, error, empty, vacio }: { loading: boolean; error: unknown; empty: boolean; vacio: string }) {
  if (loading) return <div className="ad-cargando" aria-busy="true" aria-label="Cargando"><div /><div /><div /></div>
  if (error) {
    const status = error instanceof ApiError ? error.status : 0
    if (status === 403) return <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar esta información." />
    return <Estado tipo="error" texto={mensajeError(error, 'No se pudo cargar la información. Inténtalo de nuevo.')} />
  }
  return empty ? <Estado tipo="vacio" texto={vacio} /> : null
}

export function Estado({ tipo, texto }: { tipo: 'vacio' | 'error' | 'permiso'; texto: string }) {
  return (
    <div className="ad-estado" data-tipo={tipo} role={tipo === 'vacio' ? undefined : 'alert'}>
      <span><Icono nombre={tipo === 'vacio' ? 'vacio' : tipo === 'permiso' ? 'autorizaciones' : 'alertas'} /></span>
      <p>{texto}</p>
    </div>
  )
}

function useEscape(onClose: () => void) {
  useEffect(() => {
    const alPresionarTecla = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose() }
    document.addEventListener('keydown', alPresionarTecla)
    return () => document.removeEventListener('keydown', alPresionarTecla)
  }, [onClose])
}

/** Formulario modal. `ancho`: 'normal' | 'amplio' (formularios de varias columnas) | 'matriz'. */
export function Dialogo({ titulo, eyebrow, icono, ancho = 'normal', onClose, onSubmit, children, pie }: {
  titulo: string
  eyebrow: string
  icono: NombreIcono
  ancho?: 'normal' | 'amplio' | 'matriz'
  onClose: () => void
  onSubmit: (event: FormEvent) => void
  children: ReactNode
  pie: ReactNode
}) {
  const id = useId()
  useEscape(onClose)
  return (
    <div className="ad-velo" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
      <form className="ad-modal" data-ancho={ancho} onSubmit={onSubmit} role="dialog" aria-modal="true" aria-labelledby={id} noValidate>
        <header>
          <span className="ad-modal-icono" aria-hidden="true"><Icono nombre={icono} /></span>
          <div>
            <span className="ad-modal-eyebrow">{eyebrow}</span>
            <h2 id={id}>{titulo}</h2>
          </div>
          <button type="button" className="ad-cerrar" onClick={onClose} aria-label="Cerrar"><Icono nombre="cerrar" /></button>
        </header>
        <div className="ad-modal-cuerpo">{children}</div>
        <footer>{pie}</footer>
      </form>
    </div>
  )
}

/** Confirmación previa a inactivar (o reactivar) un registro. */
export function Confirmar({ titulo, texto, accion, peligro, pendiente, error, onConfirmar, onCancelar }: {
  titulo: string
  texto: string
  accion: string
  peligro?: boolean
  pendiente: boolean
  error?: string | null
  onConfirmar: () => void
  onCancelar: () => void
}) {
  const id = useId()
  useEscape(onCancelar)
  return (
    <div className="ad-velo" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onCancelar() }}>
      <section className="ad-modal ad-confirmar" role="alertdialog" aria-modal="true" aria-labelledby={id}>
        <span className="ad-confirmar-icono" data-peligro={peligro || undefined} aria-hidden="true"><Icono nombre={peligro ? 'alertas' : 'autorizaciones'} /></span>
        <h2 id={id}>{titulo}</h2>
        <p>{texto}</p>
        {error && <p className="ad-mensaje" role="alert">{error}</p>}
        <footer>
          <button type="button" className="ad-boton-sec" onClick={onCancelar} autoFocus>Cancelar</button>
          <button type="button" className={peligro ? 'ad-boton ad-boton-peligro' : 'ad-boton'} onClick={onConfirmar} disabled={pendiente}>
            {pendiente ? 'Procesando…' : accion}
          </button>
        </footer>
      </section>
    </div>
  )
}

export function Interruptor({ etiqueta, ayuda, checked, onChange, disabled }: { etiqueta: string; ayuda?: string; checked: boolean; onChange: (valor: boolean) => void; disabled?: boolean }) {
  return (
    <label className="ad-interruptor">
      <input type="checkbox" role="switch" checked={checked} disabled={disabled} onChange={(event) => onChange(event.target.checked)} />
      <span aria-hidden="true" className="ad-interruptor-pista" />
      <span>
        <strong>{etiqueta}</strong>
        {ayuda && <small>{ayuda}</small>}
      </span>
    </label>
  )
}

/** Etiqueta + control; el error queda fuera de la etiqueta para no alterar el nombre accesible del campo. */
export function Campo({ etiqueta, error, children, ancho }: { etiqueta: string; error?: string; children: ReactNode; ancho?: 'completo' }) {
  return (
    <div className="ad-campo-grupo" data-ancho={ancho}>
      <label className="ad-campo" data-error={error ? true : undefined}>
        <span>{etiqueta}</span>
        {children}
      </label>
      {error && <small className="ad-campo-error">{error}</small>}
    </div>
  )
}

export function BotonesFormulario({ onCancelar, pendiente, texto = 'Guardar' }: { onCancelar: () => void; pendiente: boolean; texto?: string }) {
  return (
    <>
      <button type="button" className="ad-boton-sec" onClick={onCancelar}>Cancelar</button>
      <button className="ad-boton" type="submit" disabled={pendiente}>{pendiente ? 'Guardando…' : texto}</button>
    </>
  )
}

export function Buscador({ valor, onChange, placeholder, etiqueta }: { valor: string; onChange: (valor: string) => void; placeholder: string; etiqueta: string }) {
  return (
    <label className="ad-buscar">
      <Icono nombre="buscar" />
      <span className="sr-only">{etiqueta}</span>
      <input type="search" value={valor} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} />
    </label>
  )
}

export function Filtro({ etiqueta, valor, onChange, opciones }: { etiqueta: string; valor: string; onChange: (valor: string) => void; opciones: { valor: string; texto: string }[] }) {
  return (
    <label className="ad-select ad-select-corto">
      <span className="sr-only">{etiqueta}</span>
      <select value={valor} onChange={(event) => onChange(event.target.value)} aria-label={etiqueta}>
        {opciones.map((o) => <option key={o.valor} value={o.valor}>{o.texto}</option>)}
      </select>
    </label>
  )
}

export function BotonIcono({ etiqueta, icono, onClick, peligro }: { etiqueta: string; icono: NombreIcono; onClick: () => void; peligro?: boolean }) {
  return (
    <button type="button" className="ad-accion" data-peligro={peligro || undefined} onClick={onClick} aria-label={etiqueta} title={etiqueta}>
      <Icono nombre={icono} />
    </button>
  )
}
