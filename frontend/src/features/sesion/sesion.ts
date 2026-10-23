const CLAVE_SESION = 'conecsia.sesionIniciada'
let accessToken: string | null = null
let permisos: string[] = []

export const EVENTO_SESION_EXPIRADA = 'conecsia:sesion-expirada'

export function guardarAccessToken(token: string) {
  accessToken = token
  permisos = extraerPermisos(token)
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
  permisos = []
  try {
    sessionStorage.removeItem(CLAVE_SESION)
  } catch {
    // ignorado
  }
}

export function tienePermiso(permiso: string) {
  return permisos.includes(permiso)
}

export function esAdministradorGeneral() {
  return extraerSubject(accessToken) === '1'
}

/** Correo del usuario autenticado según el claim `email` del token (solo para presentación). */
export function leerCorreo(): string | null {
  const correo = decodificarPayload(accessToken)?.email
  return typeof correo === 'string' ? correo : null
}

/** Organización activa del token (primer claim `organization_id`, igual que el backend). */
export function leerOrganizacion(): number | undefined {
  const claim = decodificarPayload(accessToken)?.organization_id
  const valor = Number(Array.isArray(claim) ? claim[0] : claim)
  return Number.isInteger(valor) && valor > 0 ? valor : undefined
}

function extraerPermisos(token: string | null): string[] {
  const payload = decodificarPayload(token)
  if (!payload) return []
  const claim = payload.permission
  return Array.isArray(claim)
    ? claim.filter((value): value is string => typeof value === 'string')
    : typeof claim === 'string' ? [claim] : []
}

function extraerSubject(token: string | null) {
  return decodificarPayload(token)?.sub
}

function decodificarPayload(token: string | null): Record<string, unknown> | null {
  if (!token) return null
  try {
    const segmento = token.split('.')[1]
    if (!segmento) return null
    const texto = atob(segmento.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(texto) as Record<string, unknown>
  } catch {
    return null
  }
}
