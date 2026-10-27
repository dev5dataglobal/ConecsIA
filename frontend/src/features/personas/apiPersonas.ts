import { apiBlob, apiFetch } from '../../shared/api/http'

/*
 * Personas de la organización activa. Ningún endpoint recibe la organización: el servidor usa la del token
 * o, solo para el administrador de plataforma, la seleccionada (cabecera X-Organizacion-Id).
 */

export interface Opcion { id: number; nombre: string }
export interface OpcionDocumento { id: number; codigo: string; nombre: string; patronValidacion: string | null }

export interface CatalogosPersona {
  tiposPersona: Opcion[]
  tiposDocumento: OpcionDocumento[]
  tiposTelefono: string[]
  tiposVehiculo: string[]
  relacionesVehiculo: string[]
}

export interface PersonaLista {
  idPersona: number
  idOrganizacion: number
  idTipoPersona: number
  tipoPersona: string | null
  idTipoDocumento: number
  tipoDocumento: string | null
  numeroDocumento: string
  nombres: string
  apellidos: string
  correo: string | null
  empresaProcedencia: string | null
  observacion: string | null
  telefonoPrincipal: string | null
  tieneFoto: boolean
  activo: boolean
  creadoEn: string
}

export interface Telefono {
  idTelefono: number
  idPersona: number
  codigoPais: string
  numero: string
  tipo: string | null
  principal: boolean
  activo: boolean
}

export interface VehiculoPersona {
  idVehiculo: number
  idPersona: number
  placa: string
  tipo: string | null
  marca: string | null
  modelo: string | null
  color: string | null
  relacion: string
  activo: boolean
  otrasPersonas: number
}

export interface PersonaDetalle extends Omit<PersonaLista, 'telefonoPrincipal'> {
  /** Usuario que registró a la persona (lo fija el servidor). */
  registradoPor: string | null
  telefonos: Telefono[]
  vehiculos: VehiculoPersona[]
}

export interface PersonaEntrada {
  idTipoPersona: number
  idTipoDocumento: number
  numeroDocumento: string
  nombres: string
  apellidos: string
  correo: string
  empresaProcedencia: string | null
  observacion: string | null
}

export interface TelefonoEntrada { codigoPais: string; numero: string; tipo: string; principal: boolean }
export interface VehiculoEntrada { placa: string; tipo: string; marca: string | null; modelo: string | null; color: string | null; relacion: string }
export interface TipoPersona { idTipoPersona: number; nombre: string; descripcion: string | null; activo: boolean }

const json = (method: string, body: unknown): RequestInit => ({ method, body: JSON.stringify(body) })
const consulta = (params: Record<string, string | number | undefined>) =>
  new URLSearchParams(Object.entries(params).filter(([, v]) => v !== undefined && v !== '').map(([k, v]) => [k, String(v)])).toString()

export const consultarCatalogosPersona = () => apiFetch<CatalogosPersona>('/api/personas/catalogos')
export const consultarPersonas = (params: { buscar?: string; estado?: string; idTipoPersona?: string } = {}) =>
  apiFetch<PersonaLista[]>(`/api/personas?${consulta(params)}`)
export const buscarPorDocumento = (idTipoDocumento: number, numero: string) =>
  apiFetch<PersonaLista>(`/api/personas/buscar-documento?${consulta({ idTipoDocumento, numero })}`)
export const consultarPersona = (id: number) => apiFetch<PersonaDetalle>(`/api/personas/${id}`)
export const crearPersona = (data: PersonaEntrada) => apiFetch<PersonaDetalle>('/api/personas', json('POST', data))
export const editarPersona = (id: number, data: PersonaEntrada) => apiFetch<PersonaDetalle>(`/api/personas/${id}`, json('PUT', data))
export const cambiarEstadoPersona = (id: number, activo: boolean) => apiFetch<PersonaDetalle>(`/api/personas/${id}/estado`, json('PATCH', { activo }))

export const cargarFoto = (id: number, archivo: File) => {
  const datos = new FormData()
  datos.append('archivo', archivo)
  return apiFetch<PersonaDetalle>(`/api/personas/${id}/foto`, { method: 'POST', body: datos })
}
export const quitarFoto = (id: number) => apiFetch<PersonaDetalle>(`/api/personas/${id}/foto`, { method: 'DELETE' })
/** La foto exige el token: se descarga con fetch autenticado (con renovación de sesión) y se muestra como URL de objeto. */
export const descargarFoto = (id: number) => apiBlob(`/api/personas/${id}/foto`)

export const agregarTelefono = (idPersona: number, data: TelefonoEntrada) =>
  apiFetch<Telefono>(`/api/personas/${idPersona}/telefonos`, json('POST', data))
export const editarTelefono = (idPersona: number, idTelefono: number, data: TelefonoEntrada) =>
  apiFetch<Telefono>(`/api/personas/${idPersona}/telefonos/${idTelefono}`, json('PUT', data))
export const cambiarEstadoTelefono = (idPersona: number, idTelefono: number, activo: boolean) =>
  apiFetch<Telefono>(`/api/personas/${idPersona}/telefonos/${idTelefono}/estado`, json('PATCH', { activo }))

export const agregarVehiculo = (idPersona: number, data: VehiculoEntrada) =>
  apiFetch<VehiculoPersona>(`/api/personas/${idPersona}/vehiculos`, json('POST', data))
export const editarVehiculo = (idPersona: number, idVehiculo: number, data: VehiculoEntrada) =>
  apiFetch<VehiculoPersona>(`/api/personas/${idPersona}/vehiculos/${idVehiculo}`, json('PUT', data))
export const cambiarEstadoVehiculo = (idPersona: number, idVehiculo: number, activo: boolean) =>
  apiFetch<VehiculoPersona>(`/api/personas/${idPersona}/vehiculos/${idVehiculo}/estado`, json('PATCH', { activo }))

export const consultarTiposPersona = () => apiFetch<TipoPersona[]>('/api/personas/tipos-persona')
export const crearTipoPersona = (data: { nombre: string; descripcion: string | null }) =>
  apiFetch<TipoPersona>('/api/personas/tipos-persona', json('POST', data))
export const editarTipoPersona = (id: number, data: { nombre: string; descripcion: string | null }) =>
  apiFetch<TipoPersona>(`/api/personas/tipos-persona/${id}`, json('PUT', data))
export const cambiarEstadoTipoPersona = (id: number, activo: boolean) =>
  apiFetch<TipoPersona>(`/api/personas/tipos-persona/${id}/estado`, json('PATCH', { activo }))

// ---------- Dispositivos ----------

export interface TipoDispositivo { idTipoDispositivo: number; nombre: string; descripcion: string | null; activo: boolean }
export interface Dispositivo {
  idDispositivo: number
  idPersona: number
  idTipoDispositivo: number
  tipoDispositivo: string | null
  codigoIdentificador: string
  serial: string | null
  marca: string | null
  modelo: string | null
  color: string | null
  descripcion: string | null
  activo: boolean
  creadoEn: string
}
export interface DispositivoEntrada {
  idTipoDispositivo: number
  codigoIdentificador: string
  serial: string | null
  marca: string | null
  modelo: string | null
  color: string | null
  descripcion: string | null
}

export const consultarTiposDispositivo = () => apiFetch<TipoDispositivo[]>('/api/dispositivos/tipos')
export const crearTipoDispositivo = (data: { nombre: string; descripcion: string | null }) =>
  apiFetch<TipoDispositivo>('/api/dispositivos/tipos', json('POST', data))
export const consultarDispositivos = (idPersona: number) => apiFetch<Dispositivo[]>(`/api/personas/${idPersona}/dispositivos`)
export const agregarDispositivo = (idPersona: number, data: DispositivoEntrada) =>
  apiFetch<Dispositivo>(`/api/personas/${idPersona}/dispositivos`, json('POST', data))
export const editarDispositivo = (idPersona: number, idDispositivo: number, data: DispositivoEntrada) =>
  apiFetch<Dispositivo>(`/api/personas/${idPersona}/dispositivos/${idDispositivo}`, json('PUT', data))
export const cambiarEstadoDispositivo = (idPersona: number, idDispositivo: number, activo: boolean) =>
  apiFetch<Dispositivo>(`/api/personas/${idPersona}/dispositivos/${idDispositivo}/estado`, json('PATCH', { activo }))
