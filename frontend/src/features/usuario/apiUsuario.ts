import { apiFetch } from '../../shared/api/http'

export interface UsuarioPerfil {
  idUsuario: number
  correo: string
  activo: boolean
  ultimoAcceso: string | null
  creadoEn: string
}

export function consultarMiPerfil() { return apiFetch<UsuarioPerfil>('/api/Usuario/me') }
export function actualizarMiPerfil(correo: string) { return apiFetch<UsuarioPerfil>('/api/Usuario/me', { method: 'PUT', body: JSON.stringify({ correo }) }) }
export function cambiarMiContrasena(contrasenaActual: string, nuevaContrasena: string, confirmacionContrasena: string) { return apiFetch<void>('/api/Usuario/me/contrasena', { method: 'PUT', body: JSON.stringify({ contrasenaActual, nuevaContrasena, confirmacionContrasena }) }) }
