import { apiFetch } from '../../shared/api/http'

export interface UsuarioAdmin { idUsuario: number; idMembresia: number; idOrganizacion: number; organizacion: string; correo: string; nombre: string | null; documento: string | null; perfiles: string[]; estado: string }
export interface OrganizacionAdmin { idOrganizacion: number; idTipoOrganizacion: number; idPlan: number; nombre: string; identificacion: string | null; direccion: string | null; telefono: string | null; correo: string | null; estado: string; creadoEn: string }
export interface PerfilAdmin { idRol: number; idOrganizacion: number; nombre: string; descripcion: string | null; activo: boolean; permisos: { idPermiso: number; codigo: string; descripcion: string | null; activo: boolean }[] }

export const consultarUsuarios = (params: { buscar?: string; estado?: string } = {}) => apiFetch<UsuarioAdmin[]>(`/api/administracion/usuarios?${new URLSearchParams(params)}`)
export const consultarOrganizaciones = () => apiFetch<OrganizacionAdmin[]>('/api/administracion/organizaciones')
export const consultarPerfiles = (organizacionId: number) => apiFetch<PerfilAdmin[]>(`/api/administracion/organizaciones/${organizacionId}/perfiles`)
export const crearOrganizacion = (data: unknown) => apiFetch<OrganizacionAdmin>('/api/administracion/organizaciones', { method: 'POST', body: JSON.stringify(data) })
export const crearPerfil = (organizacionId: number, data: unknown) => apiFetch<PerfilAdmin>(`/api/administracion/organizaciones/${organizacionId}/perfiles`, { method: 'POST', body: JSON.stringify(data) })
export const crearUsuario = (data: unknown) => apiFetch<UsuarioAdmin>('/api/administracion/usuarios', { method: 'POST', body: JSON.stringify(data) })
