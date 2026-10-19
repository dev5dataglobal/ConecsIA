import {
  CABECERA_ORGANIZACION,
  leerOrganizacionSeleccionada,
} from '../../features/sesion/organizacionSeleccionada'
import { EVENTO_SESION_EXPIRADA, olvidarSesion } from '../../features/sesion/sesion'
import {
  CABECERA_USUARIO_DESARROLLO,
  leerSesionDesarrollo,
} from '../../features/sesion/sesionDesarrollo'
import { ApiError, parseProblemDetails } from './problemDetails'

/** Base de la API. Vacía = mismo origen (proxy de Vite en desarrollo, nginx en contenedor). */
const baseUrl: string = import.meta.env.VITE_API_BASE_URL ?? ''

const CABECERA_CSRF = 'X-CSRF-TOKEN'
const CODIGO_CSRF_INVALIDO = 'CSRF_INVALIDO'
const METODOS_SEGUROS = new Set(['GET', 'HEAD', 'OPTIONS'])

let tokenCsrf: Promise<string> | null = null

/** Descarta el token anti-CSRF en caché (cambió la identidad: ingreso o cierre de sesión). */
export function reiniciarCsrf() {
  tokenCsrf = null
}

function obtenerTokenCsrf(): Promise<string> {
  tokenCsrf ??= apiFetch<{ token: string }>('/api/v1/auth/csrf').then(
    (r) => r.token,
    (error: unknown) => {
      tokenCsrf = null
      throw error
    },
  )
  return tokenCsrf
}

/**
 * Cliente HTTP único. Envía la cookie de sesión del mismo origen (ADR-006) y, en las escrituras,
 * el token anti-CSRF. Convierte cualquier respuesta no exitosa en ApiError con Problem Details.
 */
export async function apiFetch<T>(
  path: string,
  init: RequestInit = {},
  reintentarCsrf = true,
): Promise<T> {
  const metodo = (init.method ?? 'GET').toUpperCase()
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  const usuario = leerSesionDesarrollo()
  if (usuario && !headers.has(CABECERA_USUARIO_DESARROLLO)) {
    headers.set(CABECERA_USUARIO_DESARROLLO, usuario)
  }
  const organizacion = leerOrganizacionSeleccionada()
  if (organizacion && !headers.has(CABECERA_ORGANIZACION)) {
    headers.set(CABECERA_ORGANIZACION, organizacion)
  }
  // El modo desarrollo no usa cookie de sesión y la API no le exige el token.
  if (!usuario && !METODOS_SEGUROS.has(metodo) && !headers.has(CABECERA_CSRF)) {
    headers.set(CABECERA_CSRF, await obtenerTokenCsrf())
  }

  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, { ...init, headers, credentials: 'same-origin' })
  } catch {
    throw new ApiError(0, { title: 'error.sinConexion', status: 0 })
  }

  if (response.status === 204) return undefined as T

  const isJson = response.headers.get('Content-Type')?.includes('json') ?? false
  const body: unknown = isJson ? await response.json().catch(() => undefined) : undefined

  if (!response.ok) {
    const problema = parseProblemDetails(body, response.status)
    // Token vencido o de otra identidad: se pide uno nuevo y se reintenta una sola vez.
    if (response.status === 403 && problema.code === CODIGO_CSRF_INVALIDO && reintentarCsrf) {
      reiniciarCsrf()
      return apiFetch<T>(path, init, false)
    }
    // La sesión ya no es válida para el servidor: se olvida y la UI vuelve a pedir identidad.
    if (response.status === 401) {
      olvidarSesion()
      window.dispatchEvent(new Event(EVENTO_SESION_EXPIRADA))
    }
    throw new ApiError(response.status, problema)
  }
  return body as T
}
