import { EVENTO_SESION_EXPIRADA, leerAccessToken, leerOrganizacionSeleccionada, olvidarSesion } from '../../features/sesion/sesion'
import { ApiError, parseProblemDetails } from './problemDetails'

const baseUrl: string = import.meta.env.VITE_API_BASE_URL ?? ''

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  // FormData define su propio Content-Type (multipart con boundary).
  if (init.body !== undefined && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  const token = leerAccessToken()
  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`)
  }
  const organizacion = leerOrganizacionSeleccionada()
  if (token && organizacion !== undefined) headers.set('X-Organizacion-Id', String(organizacion))

  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, { ...init, headers })
  } catch {
    throw new ApiError(0, { title: 'error.sinConexion', status: 0 })
  }

  if (response.status === 204) return undefined as T
  const isJson = response.headers.get('Content-Type')?.includes('json') ?? false
  const body: unknown = isJson ? await response.json().catch(() => undefined) : undefined

  if (!response.ok) {
    const problema = parseProblemDetails(body, response.status)
    if (response.status === 401) {
      olvidarSesion()
      window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA))
    }
    throw new ApiError(response.status, problema)
  }
  return body as T
}
