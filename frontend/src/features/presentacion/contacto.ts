/** Lógica del formulario de contacto (sin dependencias de UI). */

export type DatosContacto = {
  organizacion: string
  nombre: string
  correo: string
  telefono: string
  tipo: string
  sedes: string
  mensaje: string
}

export type ErroresContacto = Partial<Record<keyof DatosContacto, string>>

export const CONTACTO_VACIO: DatosContacto = {
  organizacion: '',
  nombre: '',
  correo: '',
  telefono: '',
  tipo: '',
  sedes: '',
  mensaje: '',
}

const CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/
const TELEFONO = /^\+?[\d\s()-]{7,20}$/

export function validarContacto(d: DatosContacto): ErroresContacto {
  const e: ErroresContacto = {}
  if (d.organizacion.trim().length < 2) e.organizacion = 'Escribe el nombre de la organización.'
  else if (d.organizacion.length > 150) e.organizacion = 'Máximo 150 caracteres.'
  if (d.nombre.trim().length < 2) e.nombre = 'Escribe el nombre de la persona de contacto.'
  else if (d.nombre.length > 120) e.nombre = 'Máximo 120 caracteres.'
  if (!CORREO.test(d.correo.trim())) e.correo = 'Escribe un correo electrónico válido.'
  if (d.telefono.trim() && !TELEFONO.test(d.telefono.trim()))
    e.telefono = 'Escribe un teléfono válido (solo números, espacios, +, - o paréntesis).'
  if (!d.tipo) e.tipo = 'Selecciona el tipo de organización.'
  if (d.sedes.trim() && !/^\d{1,4}$/.test(d.sedes.trim())) e.sedes = 'Escribe un número entero.'
  if (d.mensaje.trim().length < 10) e.mensaje = 'Cuéntanos tu necesidad en al menos 10 caracteres.'
  else if (d.mensaje.length > 2000) e.mensaje = 'Máximo 2000 caracteres.'
  return e
}

/** Endpoint público y anónimo del backend (ContactoController); no usa el cliente autenticado. */
export const URL_CONTACTO = `${import.meta.env.VITE_API_BASE_URL ?? ''}/api/publico/contactos`

export type FalloEnvio = 'datos' | 'limite' | 'red'

/** Lanza un FalloEnvio; solo resuelve cuando el servidor aceptó la solicitud (202). */
export async function enviarContacto(d: DatosContacto, sitioWeb = '') {
  let r: Response
  try {
    r = await fetch(URL_CONTACTO, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        organizacion: d.organizacion.trim(),
        nombre: d.nombre.trim(),
        correo: d.correo.trim(),
        telefono: d.telefono.trim() || null,
        tipoOrganizacion: d.tipo,
        numeroSedes: d.sedes.trim() ? Number(d.sedes) : null,
        mensaje: d.mensaje.trim(),
        sitioWeb: sitioWeb || null,
      }),
    })
  } catch {
    throw 'red' satisfies FalloEnvio
  }
  if (r.status === 400) throw 'datos' satisfies FalloEnvio
  if (r.status === 429) throw 'limite' satisfies FalloEnvio
  if (!r.ok) throw 'red' satisfies FalloEnvio
}
