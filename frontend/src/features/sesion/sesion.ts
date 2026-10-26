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
  organizacionSeleccionada = null
  try {
    sessionStorage.removeItem(CLAVE_SESION)
    sessionStorage.removeItem(CLAVE_ORGANIZACION)
  } catch {
    // ignorado
  }
}

/*
 * Organización seleccionada por el administrador de plataforma. Se guarda en sessionStorage (por pestaña)
 * junto al usuario que la eligió: otro usuario o una nueva sesión nunca la heredan. El backend la valida
 * en cada petición (cabecera X-Organizacion-Id) y rechaza a quien no sea administrador de plataforma.
 */
const CLAVE_ORGANIZACION = 'conecsia.organizacionSeleccionada'
let organizacionSeleccionada: { usuario: string; id: number } | null = null

export function leerOrganizacionSeleccionada(): number | undefined {
  if (!esAdministradorGeneral()) return undefined
  const usuario = String(extraerSubject(accessToken) ?? '')
  if (!organizacionSeleccionada) {
    try {
      const guardada = JSON.parse(sessionStorage.getItem(CLAVE_ORGANIZACION) ?? 'null') as { usuario?: unknown; id?: unknown } | null
      if (guardada && typeof guardada.usuario === 'string' && typeof guardada.id === 'number') organizacionSeleccionada = { usuario: guardada.usuario, id: guardada.id }
    } catch {
      // Sin almacenamiento: la selección vive solo en memoria.
    }
  }
  return organizacionSeleccionada?.usuario === usuario ? organizacionSeleccionada.id : undefined
}

export function seleccionarOrganizacion(id: number | undefined) {
  const usuario = String(extraerSubject(accessToken) ?? '')
  organizacionSeleccionada = id === undefined ? null : { usuario, id }
  try {
    if (organizacionSeleccionada) sessionStorage.setItem(CLAVE_ORGANIZACION, JSON.stringify(organizacionSeleccionada))
    else sessionStorage.removeItem(CLAVE_ORGANIZACION)
  } catch {
    // ignorado
  }
}

/** Organización con la que se trabaja: la seleccionada (solo plataforma) o la del token. */
export function leerOrganizacionActiva(): number | undefined {
  return leerOrganizacionSeleccionada() ?? leerOrganizacion()
}

export function tienePermiso(permiso: string) {
  return permisos.includes(permiso)
}

/** Claim `platform_admin` emitido por el backend (solo presentación; el servidor decide). */
export function esAdministradorGeneral() {
  return decodificarPayload(accessToken)?.platform_admin === 'true'
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
