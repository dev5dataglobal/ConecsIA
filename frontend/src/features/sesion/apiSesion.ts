import { apiFetch } from '../../shared/api/http'
import { guardarAccessToken, marcarSesionIniciada, olvidarSesion } from './sesion'

export interface RespuestaLogin {
  accessToken: string
  expiresAtUtc: string
  usuario: {
    idUsuario: number
    correo: string
    activo: boolean
  }
}

export async function ingresar(correo: string, password: string) {
  olvidarSesion()
  const respuesta = await apiFetch<RespuestaLogin>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ correo, password }),
  })
  guardarAccessToken(respuesta.accessToken)
  marcarSesionIniciada()
  return respuesta
}

export function cerrarSesion() {
  olvidarSesion()
}
