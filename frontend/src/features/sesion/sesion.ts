import { olvidarOrganizacionSeleccionada } from './organizacionSeleccionada'
import { cerrarSesionDesarrollo, leerSesionDesarrollo } from './sesionDesarrollo'

/**
 * Estado de sesión visible para la UI (HU-006, ADR-006). La cookie de sesión es HttpOnly: el
 * navegador no puede leerla, así que la SPA guarda solo una marca de «sesión iniciada» para decidir
 * qué mostrar. No es un mecanismo de seguridad: la API valida la cookie en cada petición.
 */
const CLAVE_SESION = 'conecsia.sesionIniciada'

/** Se emite cuando la API responde 401: la sesión venció, se revocó o nunca existió. */
export const EVENTO_SESION_EXPIRADA = 'conecsia:sesion-expirada'

export function marcarSesionIniciada() {
  try {
    sessionStorage.setItem(CLAVE_SESION, '1')
  } catch {
    // Sin almacenamiento: al recargar se volverá a comprobar con /me.
  }
}

export function haySesion(): boolean {
  if (leerSesionDesarrollo() !== null) return true
  try {
    return sessionStorage.getItem(CLAVE_SESION) === '1'
  } catch {
    return false
  }
}

/** Olvida cualquier sesión local (real o de desarrollo). */
export function olvidarSesion() {
  cerrarSesionDesarrollo()
  olvidarOrganizacionSeleccionada()
  try {
    sessionStorage.removeItem(CLAVE_SESION)
  } catch {
    // ignorado
  }
}
