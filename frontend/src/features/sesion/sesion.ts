const CLAVE_SESION = 'conecsia.sesionIniciada'
let accessToken: string | null = null

export const EVENTO_SESION_EXPIRADA = 'conecsia:sesion-expirada'

export function guardarAccessToken(token: string) {
  accessToken = token
}

export function leerAccessToken(): string | null {
  return accessToken
}

export function marcarSesionIniciada() {
  try {
    sessionStorage.setItem(CLAVE_SESION, '1')
  } catch {
    // El token permanece en memoria aunque sessionStorage no esté disponible.
  }
}

export function haySesion(): boolean {
  return accessToken !== null
}

export function olvidarSesion() {
  accessToken = null
  try {
    sessionStorage.removeItem(CLAVE_SESION)
  } catch {
    // ignorado
  }
}
