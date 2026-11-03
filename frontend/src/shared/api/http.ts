import { EVENTO_SESION_EXPIRADA, guardarAccessToken, leerAccessToken, leerOrganizacionSeleccionada, olvidarSesion } from '../../features/sesion/sesion'
import { ApiError, parseProblemDetails } from './problemDetails'

const baseUrl: string = import.meta.env.VITE_API_BASE_URL ?? ''

/** Cabecera exigida por /api/auth/renovar y /cerrar-sesion (fuerza preflight CORS). */
export const CABECERA_SESION = 'X-Conecsia-Sesion'

let renovacionEnCurso: Promise<boolean> | null = null

/**
 * Pide un token de acceso nuevo con la cookie HttpOnly de sesión (el navegador la envía sola; JS no la ve).
 * Una sola petición en vuelo aunque varias consultas reciban 401 a la vez.
 */
export function renovarSesion(): Promise<boolean> {
  renovacionEnCurso ??= (async () => {
    try {
      const response = await fetch(`${baseUrl}/api/auth/renovar`, {
        method: 'POST',
        credentials: 'same-origin',
        headers: { Accept: 'application/json', [CABECERA_SESION]: '1' },
      })
      if (!response.ok) return false
      const body = (await response.json()) as { accessToken?: unknown }
      if (typeof body.accessToken !== 'string') return false
      guardarAccessToken(body.accessToken)
      return true
    } catch {
      return false
    } finally {
      renovacionEnCurso = null
    }
  })()
  return renovacionEnCurso
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response = await enviar(path, init)

  // Token de acceso vencido o invalidado: se intenta una renovación y se repite la petición una vez.
  if (response.status === 401 && leerAccessToken() && !path.startsWith('/api/auth/') && (await renovarSesion())) {
    response = await enviar(path, init)
  }

  if (response.status === 204) return undefined as T
  const isJson = response.headers.get('Content-Type')?.includes('json') ?? false
  const body: unknown = isJson ? await response.json().catch(() => undefined) : undefined

  if (!response.ok) {
    const problema = parseProblemDetails(body, response.status)
    if (response.status === 401 && !path.startsWith('/api/auth/')) {
      olvidarSesion()
      window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA))
    }
    throw new ApiError(response.status, problema)
  }
  return body as T
}

/**
 * Descarga un archivo protegido (p. ej. la foto de una persona) con el mismo token, organización y renovación de
 * sesión que {@link apiFetch}. Sin renovación, un token vencido dejaba la imagen sin cargar.
 */
export async function apiBlob(path: string): Promise<Blob> {
  const init: RequestInit = { headers: { Accept: '*/*' } }
  let response = await enviar(path, init)
  if (response.status === 401 && leerAccessToken() && (await renovarSesion())) response = await enviar(path, init)
  if (!response.ok) throw new ApiError(response.status, { status: response.status })
  return response.blob()
}

async function enviar(path: string, init: RequestInit): Promise<Response> {
  const headers = new Headers(init.headers)
  if (!headers.has('Accept')) headers.set('Accept', 'application/json')
  // FormData define su propio Content-Type (multipart con boundary).
  if (init.body !== undefined && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  const token = leerAccessToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  const organizacion = leerOrganizacionSeleccionada()
  if (token && organizacion !== undefined) headers.set('X-Organizacion-Id', String(organizacion))

  try {
    return await fetch(`${baseUrl}${path}`, { ...init, headers })
  } catch {
    throw new ApiError(0, { title: 'error.sinConexion', status: 0 })
  }
}
