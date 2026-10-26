import { apiFetch, CABECERA_SESION, renovarSesion } from '../../shared/api/http'
import { guardarAccessToken, haySesion, marcarSesionIniciada, olvidarSesion } from './sesion'

export interface RespuestaLogin {
  accessToken: string
  expiresAtUtc: string
  usuario: {
    idUsuario: number
    correo: string
    activo: boolean
  }
}

/**
 * `recordarme`: el servidor emite una cookie HttpOnly persistente (sobrevive al cierre del navegador, hasta la vigencia máxima de la sesión);
 * sin ella, una cookie de sesión del navegador. El token de acceso solo vive en memoria.
 */
export async function ingresar(correo: string, password: string, recordarme = false) {
  olvidarSesion()
  const respuesta = await apiFetch<RespuestaLogin>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ correo, password, recordarme }),
  })
  guardarAccessToken(respuesta.accessToken)
  marcarSesionIniciada()
  return respuesta
}

/** Al cargar la aplicación: recupera la sesión desde la cookie (recarga o navegador reabierto con «Recordarme»). */
export async function restaurarSesion() {
  if (!haySesion()) await renovarSesion()
}

export async function cerrarSesion() {
  olvidarSesion()
  try {
    await apiFetch<void>('/api/auth/cerrar-sesion', { method: 'POST', headers: { [CABECERA_SESION]: '1' } })
  } catch {
    // Sin conexión: el token ya se olvidó; la cookie vence por sí sola en el servidor.
  }
}

export const solicitarRecuperacion = (correo: string) =>
  apiFetch<{ mensaje: string }>('/api/auth/recuperar-contrasena', { method: 'POST', body: JSON.stringify({ correo }) })

export const restablecerContrasena = (token: string, nuevaContrasena: string, confirmacionContrasena: string) =>
  apiFetch<void>('/api/auth/restablecer-contrasena', {
    method: 'POST',
    body: JSON.stringify({ token, nuevaContrasena, confirmacionContrasena }),
  })
