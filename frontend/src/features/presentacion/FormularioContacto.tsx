import { useRef, useState, type FormEvent, type ReactNode } from 'react'
import { Icono } from '../../shared/ui/Icono'
import { TIPOS_ORGANIZACION } from './contenido'
import {
  CONTACTO_VACIO,
  enviarContacto,
  validarContacto,
  type DatosContacto,
  type ErroresContacto,
  type FalloEnvio,
} from './contacto'

function Campo({
  id,
  etiqueta,
  error,
  opcional,
  children,
  ayuda,
}: {
  id: string
  etiqueta: string
  error?: string
  opcional?: boolean
  children: ReactNode
  ayuda?: string
}) {
  return (
    <div className="lp-campo" data-error={error ? true : undefined}>
      <label htmlFor={id}>
        {etiqueta}
        {opcional && <span className="lp-opcional"> (opcional)</span>}
      </label>
      {children}
      {ayuda && !error && (
        <small id={`${id}-ayuda`} className="lp-ayuda">
          {ayuda}
        </small>
      )}
      {error && (
        <small id={`${id}-error`} className="lp-error">
          {error}
        </small>
      )}
    </div>
  )
}

type EstadoEnvio = 'inactivo' | 'enviando' | 'enviado' | FalloEnvio

const MENSAJES_FALLO: Record<FalloEnvio, string> = {
  datos: 'El servidor rechazó algunos datos. Revísalos e inténtalo de nuevo.',
  limite:
    'Recibimos varios envíos desde tu conexión. Espera un momento antes de intentarlo otra vez.',
  red: 'No pudimos enviar tu mensaje. Revisa tu conexión e inténtalo de nuevo.',
}

export function FormularioContacto() {
  const [datos, setDatos] = useState<DatosContacto>(CONTACTO_VACIO)
  const [errores, setErrores] = useState<ErroresContacto>({})
  const [estado, setEstado] = useState<EstadoEnvio>('inactivo')
  const form = useRef<HTMLFormElement>(null)
  const trampa = useRef<HTMLInputElement>(null)

  const cambiar = (k: keyof DatosContacto) => (e: { target: { value: string } }) => {
    setDatos((d) => ({ ...d, [k]: e.target.value }))
    if (errores[k]) setErrores((er) => ({ ...er, [k]: undefined }))
  }
  const a11y = (k: keyof DatosContacto, ayuda = false) => ({
    id: `contacto-${k}`,
    'aria-invalid': errores[k] ? true : undefined,
    'aria-describedby': errores[k]
      ? `contacto-${k}-error`
      : ayuda
        ? `contacto-${k}-ayuda`
        : undefined,
  })

  async function enviar(e: FormEvent) {
    e.preventDefault()
    const er = validarContacto(datos)
    setErrores(er)
    const primero = Object.keys(er)[0]
    if (primero) {
      form.current?.querySelector<HTMLElement>(`#contacto-${primero}`)?.focus()
      return
    }
    setEstado('enviando')
    try {
      await enviarContacto(datos, trampa.current?.value)
      setEstado('enviado')
      setDatos(CONTACTO_VACIO)
    } catch (fallo) {
      setEstado(fallo === 'datos' || fallo === 'limite' ? fallo : 'red')
    }
  }

  if (estado === 'enviado') {
    return (
      <div className="lp-form lp-confirmacion" role="status">
        <Icono nombre="check" className="lp-icono-grande" />
        <h3>Recibimos tu mensaje</h3>
        <p>Nos pondremos en contacto con el correo que indicaste.</p>
        <button type="button" className="lp-btn lp-btn-sec" onClick={() => setEstado('inactivo')}>
          Enviar otro mensaje
        </button>
      </div>
    )
  }

  return (
    <form
      ref={form}
      className="lp-form"
      noValidate
      onSubmit={enviar}
      aria-label="Formulario de contacto"
    >
      <div className="lp-form-grid">
        <Campo
          id="contacto-organizacion"
          etiqueta="Nombre de la organización"
          error={errores.organizacion}
        >
          <input
            {...a11y('organizacion')}
            placeholder="Nombre de tu organización"
            autoComplete="organization"
            maxLength={150}
            value={datos.organizacion}
            onChange={cambiar('organizacion')}
            required
          />
        </Campo>
        <Campo id="contacto-nombre" etiqueta="Persona de contacto" error={errores.nombre}>
          <input
            {...a11y('nombre')}
            placeholder="Tu nombre y apellido"
            autoComplete="name"
            maxLength={120}
            value={datos.nombre}
            onChange={cambiar('nombre')}
            required
          />
        </Campo>
        <Campo id="contacto-correo" etiqueta="Correo electrónico" error={errores.correo}>
          <input
            {...a11y('correo')}
            placeholder="nombre@organizacion.com"
            type="email"
            autoComplete="email"
            maxLength={200}
            value={datos.correo}
            onChange={cambiar('correo')}
            required
          />
        </Campo>
        <Campo id="contacto-telefono" etiqueta="Teléfono" opcional error={errores.telefono}>
          <input
            {...a11y('telefono')}
            placeholder="Número con indicativo"
            type="tel"
            autoComplete="tel"
            maxLength={20}
            value={datos.telefono}
            onChange={cambiar('telefono')}
          />
        </Campo>
        <Campo id="contacto-tipo" etiqueta="Tipo de organización" error={errores.tipo}>
          <select {...a11y('tipo')} value={datos.tipo} onChange={cambiar('tipo')} required>
            <option value="">Selecciona una opción</option>
            {TIPOS_ORGANIZACION.map((t) => (
              <option key={t}>{t}</option>
            ))}
          </select>
        </Campo>
        <Campo
          id="contacto-sedes"
          etiqueta="Número aproximado de sedes"
          opcional
          error={errores.sedes}
        >
          <input
            {...a11y('sedes')}
            placeholder="Cantidad de sedes"
            inputMode="numeric"
            maxLength={4}
            value={datos.sedes}
            onChange={cambiar('sedes')}
          />
        </Campo>
        <Campo
          id="contacto-mensaje"
          etiqueta="¿Qué necesitas gestionar?"
          error={errores.mensaje}
          ayuda="Cuéntanos brevemente tu necesidad principal."
        >
          <textarea
            {...a11y('mensaje', true)}
            placeholder="Cuéntanos sobre tu necesidad…"
            rows={4}
            maxLength={2000}
            value={datos.mensaje}
            onChange={cambiar('mensaje')}
            required
          />
        </Campo>
      </div>

      {/* Campo trampa para bots: fuera de la vista, del orden de tabulación y del árbol accesible. */}
      <input
        ref={trampa}
        className="lp-trampa"
        name="sitio_web"
        tabIndex={-1}
        autoComplete="off"
        aria-hidden="true"
        defaultValue=""
      />

      {(estado === 'datos' || estado === 'limite' || estado === 'red') && (
        <p className="lp-aviso lp-aviso-error" role="alert">
          {MENSAJES_FALLO[estado]} Tu mensaje <b>no se ha enviado</b>.
        </p>
      )}

      <button
        type="submit"
        className="lp-btn lp-btn-pri lp-btn-bloque"
        disabled={estado === 'enviando'}
      >
        {estado === 'enviando' ? 'Enviando…' : 'Enviar solicitud'}
        <Icono nombre="flecha" className="lp-icono" />
      </button>
    </form>
  )
}
