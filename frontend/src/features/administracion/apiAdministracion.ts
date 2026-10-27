import { apiFetch } from '../../shared/api/http'
import { leerAccessToken, leerOrganizacionSeleccionada } from '../sesion/sesion'

/*
 * Administración de la organización activa. Ningún endpoint recibe la organización del cliente:
 * el servidor usa la del token o, solo para el administrador de plataforma, la seleccionada (cabecera).
 */

export interface Opcion { id: number; nombre: string }

export interface UsuarioAdmin {
  idUsuario: number
  idMembresia: number
  idOrganizacion: number
  organizacion: string
  correo: string
  idPersona: number | null
  nombre: string | null
  documento: string | null
  idRoles: number[]
  perfiles: string[]
  activo: boolean
  estado: string
}

export interface OrganizacionAdmin {
  idOrganizacion: number
  idTipoOrganizacion: number
  tipoOrganizacion: string | null
  idPlan: number
  plan: string | null
  nombre: string
  identificacion: string | null
  direccion: string | null
  telefono: string | null
  correo: string | null
  estado: string
  creadoEn: string
  tieneLogo: boolean
  idZonaEmpresarial: number | null
  zonaEmpresarial: string | null
  organizacionSuperior: string | null
  esOrganizacionSuperior: boolean
}

export interface OrganizacionEntrada {
  nombre: string
  idTipoOrganizacion: number
  idPlan: number
  identificacion: string
  direccion: string | null
  telefono: string
  correo: string
  idZonaEmpresarial: number | null
  estado: 'Activa' | 'Inactiva'
}

export interface CatalogosOrganizacion { tipos: Opcion[]; planes: Opcion[]; zonas: Opcion[] }

export interface PerfilAdmin {
  idRol: number
  idOrganizacion: number
  nombre: string
  descripcion: string | null
  activo: boolean
  totalModulos: number
  totalUsuarios: number
  permisos: { idPermiso: number; codigo: string; descripcion: string | null; activo: boolean }[]
}

export interface EmpresaZona { idOrganizacion: number; nombre: string; identificacion: string | null; estado: string }

export interface ZonaAdmin {
  idZonaEmpresarial: number
  nombre: string
  descripcion: string | null
  activa: boolean
  creadaEn: string
  totalSedes: number
  organizacionSuperior: OrganizacionAdmin | null
  empresas: EmpresaZona[]
}

export interface ZonaEntrada {
  nombre: string
  descripcion: string | null
  activa: boolean
  organizacionSuperior: OrganizacionEntrada
  idEmpresas: number[]
}

const json = (method: string, data: unknown): RequestInit => ({ method, body: JSON.stringify(data) })
const consulta = (params: Record<string, string | undefined>) =>
  new URLSearchParams(Object.entries(params).filter((par): par is [string, string] => Boolean(par[1]))).toString()

// Usuarios
export const consultarUsuarios = (params: { buscar?: string; estado?: string; idRol?: string } = {}) =>
  apiFetch<UsuarioAdmin[]>(`/api/administracion/usuarios?${consulta(params)}`)
export const consultarPerfilesAsignables = () => apiFetch<Opcion[]>('/api/administracion/usuarios/perfiles-asignables')
export const crearUsuario = (data: { correo: string; contrasena: string; idRoles: number[]; activo: boolean }) =>
  apiFetch<UsuarioAdmin>('/api/administracion/usuarios', json('POST', data))
export const editarUsuario = (idMembresia: number, data: { correo: string; idPersona: number | null; idRoles: number[]; activo: boolean }) =>
  apiFetch<UsuarioAdmin>(`/api/administracion/usuarios/${idMembresia}`, json('PUT', data))
export const cambiarEstadoUsuario = (idMembresia: number, activo: boolean) =>
  apiFetch<UsuarioAdmin>(`/api/administracion/usuarios/${idMembresia}/estado`, json('PATCH', { activo }))

// Organizaciones
export const consultarOrganizaciones = (params: { buscar?: string; estado?: string } = {}) =>
  apiFetch<OrganizacionAdmin[]>(`/api/administracion/organizaciones?${consulta(params)}`)
export const consultarCatalogosOrganizacion = () => apiFetch<CatalogosOrganizacion>('/api/administracion/organizaciones/catalogos')
export const crearOrganizacion = (data: OrganizacionEntrada) => apiFetch<OrganizacionAdmin>('/api/administracion/organizaciones', json('POST', data))
export const editarOrganizacion = (id: number, data: OrganizacionEntrada) =>
  apiFetch<OrganizacionAdmin>(`/api/administracion/organizaciones/${id}`, json('PUT', data))
export const cambiarEstadoOrganizacion = (id: number, activo: boolean) =>
  apiFetch<OrganizacionAdmin>(`/api/administracion/organizaciones/${id}/estado`, json('PATCH', { activo }))
export const cargarLogo = (id: number, archivo: File) => {
  const datos = new FormData()
  datos.append('archivo', archivo)
  return apiFetch<OrganizacionAdmin>(`/api/administracion/organizaciones/${id}/logo`, { method: 'POST', body: datos })
}
export const quitarLogo = (id: number) => apiFetch<OrganizacionAdmin>(`/api/administracion/organizaciones/${id}/logo`, { method: 'DELETE' })
/** El logo exige el token: se descarga con fetch autenticado (se muestra como URL de objeto). */
export async function descargarLogo(id: number): Promise<Blob> {
  const headers = new Headers({ Authorization: `Bearer ${leerAccessToken() ?? ''}` })
  const seleccion = leerOrganizacionSeleccionada()
  if (seleccion !== undefined) headers.set('X-Organizacion-Id', String(seleccion))
  const respuesta = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ''}/api/administracion/organizaciones/${id}/logo`, { headers })
  if (!respuesta.ok) throw new Error('logo')
  return respuesta.blob()
}

// Perfiles
export const consultarPerfiles = (params: { buscar?: string; estado?: string } = {}) =>
  apiFetch<PerfilAdmin[]>(`/api/administracion/perfiles?${consulta(params)}`)
export const crearPerfil = (data: { nombre: string; descripcion: string | null; activo: boolean }) =>
  apiFetch<PerfilAdmin>('/api/administracion/perfiles', json('POST', data))
export const editarPerfil = (idRol: number, data: { nombre: string; descripcion: string | null; activo: boolean }) =>
  apiFetch<PerfilAdmin>(`/api/administracion/perfiles/${idRol}`, json('PUT', data))
export const cambiarEstadoPerfil = (idRol: number, activo: boolean) =>
  apiFetch<PerfilAdmin>(`/api/administracion/perfiles/${idRol}/estado`, json('PATCH', { activo }))

// Zonas empresariales
export const consultarZonas = (params: { buscar?: string; activa?: string } = {}) =>
  apiFetch<ZonaAdmin[]>(`/api/zonas-empresariales?${consulta(params)}`)
export const consultarEmpresasDisponibles = (idZona?: number) =>
  apiFetch<EmpresaZona[]>(`/api/zonas-empresariales/empresas-disponibles${idZona ? `?idZona=${idZona}` : ''}`)
export const crearZona = (data: ZonaEntrada) => apiFetch<ZonaAdmin>('/api/zonas-empresariales', json('POST', data))
export const editarZona = (id: number, data: ZonaEntrada) => apiFetch<ZonaAdmin>(`/api/zonas-empresariales/${id}`, json('PUT', data))
export const cambiarEstadoZona = (id: number, activo: boolean) => apiFetch<ZonaAdmin>(`/api/zonas-empresariales/${id}/estado`, json('PATCH', { activo }))
