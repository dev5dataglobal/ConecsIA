/**
 * Sesión de DESARROLLO: guarda el usuario sintético elegido y la API la recibe en una cabecera.
 * Sustituye a la autenticación real (HU-006) mientras D-03 está abierta; la API la rechaza fuera
 * de Development/Testing. No es un mecanismo de seguridad.
 */
const CLAVE = 'conecsia.usuarioDesarrollo'
export const CABECERA_USUARIO_DESARROLLO = 'X-Usuario-Desarrollo'

export function leerSesionDesarrollo(): string | null {
  try {
    return sessionStorage.getItem(CLAVE)
  } catch {
    return null
  }
}

export function guardarSesionDesarrollo(usuarioId: string) {
  try {
    sessionStorage.setItem(CLAVE, usuarioId)
  } catch {
    // Sin almacenamiento: la sesión dura lo que la pestaña en memoria; no es crítico.
  }
}

export function cerrarSesionDesarrollo() {
  try {
    sessionStorage.removeItem(CLAVE)
  } catch {
    // ignorado
  }
}
