import { apiFetch, reiniciarCsrf } from '../../shared/api/http'
import { marcarSesionIniciada, olvidarSesion } from './sesion'
import { leerSesionDesarrollo } from './sesionDesarrollo'

/** POST /api/v1/auth/login. Un 401 trae un mensaje genérico; 423 indica bloqueo temporal. */
export async function ingresar(correo: string, contrasena: string) {
  olvidarSesion()
  await apiFetch<void>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ correo, contrasena }),
  })
  // El token anti-CSRF queda ligado a la identidad: tras ingresar se pide uno nuevo.
  reiniciarCsrf()
  marcarSesionIniciada()
}

/** Cierra la sesión en el servidor (la revoca) o, en modo desarrollo, solo olvida el usuario. */
export async function cerrarSesion() {
  if (leerSesionDesarrollo() === null) {
    try {
      await apiFetch<void>('/api/v1/auth/logout', { method: 'POST' })
    } catch {
      // Aunque falle la red, la sesión local se olvida; la cookie vence sola.
    }
  }
  reiniciarCsrf()
  olvidarSesion()
}
