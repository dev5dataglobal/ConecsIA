import { apiFetch } from '../../shared/api/http'

/*
 * Control de accesos unificado (ADR-029): registro de acceso con categoría, tipo de permanencia, horario y vehículos o
 * dispositivos; decisiones; entradas y salidas; presencia; historial y bitácora. La organización, los permisos, la
 * vigencia, el horario y la validez del ingreso los decide el servidor: aquí solo se presentan.
 */

export type Modalidad = 'Unica' | 'Diaria' | 'Temporal' | 'Recurrente' | 'Permanente'
export type EstadoAcceso = 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Cancelada' | 'Revocada' | 'Vencida' | 'Agotada'
export type VigenciaAcceso = 'Vigente' | 'Programada' | 'FueraDeHorario' | 'Vencida' | 'Consumida' | 'NoAplica'
export type FlujoAprobacion = 'Automatico' | 'Anfitrion' | 'Seguridad'
export type TipoObjeto = 'Vehiculo' | 'Dispositivo'

export const MODALIDADES: { valor: Modalidad; texto: string; grupo: string; descripcion: string }[] = [
  { valor: 'Unica', texto: 'Única', grupo: 'Temporal / puntual', descripcion: 'Un solo ingreso en un día. Al registrarse la entrada queda consumida.' },
  { valor: 'Diaria', texto: 'Diaria', grupo: 'Temporal / puntual', descripcion: 'Válida durante un día específico.' },
  { valor: 'Temporal', texto: 'Temporal', grupo: 'Temporal / puntual', descripcion: 'Entre una fecha y hora de inicio y una de finalización.' },
  { valor: 'Recurrente', texto: 'Recurrente', grupo: 'Recurrente', descripcion: 'Franjas por día de la semana que se repiten, con periodo opcional.' },
  { valor: 'Permanente', texto: 'Permanente', grupo: 'Permanente', descripcion: 'Sin fecha final; se puede revocar. Solo si la organización lo permite.' },
]
export const textoModalidad = (m: string) => MODALIDADES.find((x) => x.valor === m)?.texto ?? m

/** Días ISO: 1 = lunes … 7 = domingo. */
export const DIAS: { valor: number; corto: string; texto: string }[] = [
  { valor: 1, corto: 'L', texto: 'Lunes' }, { valor: 2, corto: 'M', texto: 'Martes' }, { valor: 3, corto: 'X', texto: 'Miércoles' },
  { valor: 4, corto: 'J', texto: 'Jueves' }, { valor: 5, corto: 'V', texto: 'Viernes' }, { valor: 6, corto: 'S', texto: 'Sábado' }, { valor: 7, corto: 'D', texto: 'Domingo' },
]

export interface Franja { diaSemana: number; horaInicio: string; horaFin: string }

export interface ObjetoAcceso {
  idAutorizacionObjeto: number
  tipoObjeto: TipoObjeto
  idObjeto: number
  objeto: string | null
  estado: 'Pendiente' | 'Aprobada' | 'Rechazada' | 'Revocada'
  motivoDecision: string | null
  decididoPor: string | null
  decididoEn: string | null
  /** Entrada sin salida del vehículo o dispositivo (en cualquier registro de la organización). */
  dentro: boolean
  entradaEn: string | null
}

export interface TransicionAcceso { estadoAnterior: string | null; estadoNuevo: string; actor: string | null; observacion: string | null; ocurridoEn: string }

export interface MovimientoHistorial {
  idMovimiento: number
  idAutorizacion: number | null
  idPersona: number
  persona: string
  documentoPersona: string
  sede: string
  area: string | null
  anfitrion: string | null
  puntoAcceso: string
  tipoMovimiento: 'Entrada' | 'Salida'
  resultado: 'Confirmado' | 'Denegado'
  motivo: string | null
  operador: string | null
  ocurridoEn: string
}

export interface RegistroAcceso {
  idAutorizacion: number
  idCategoriaAcceso: number | null
  categoria: string | null
  flujoAprobacion: FlujoAprobacion | null
  idPersona: number
  persona: string
  documentoPersona: string
  personaActiva: boolean
  idSede: number
  sede: string
  idArea: number
  area: string
  idMembresiaResponsable: number
  anfitrion: string | null
  motivo: string | null
  observacion: string | null
  modalidad: Modalidad
  fechaInicio: string
  fechaFin: string | null
  horaInicio: string | null
  horaFin: string | null
  usaHorarioBase: boolean
  cupoIngresos: number | null
  ingresosConsumidos: number
  estado: EstadoAcceso
  vigencia: VigenciaAcceso
  origen: 'Manual' | 'Politica' | 'Migracion'
  creadoPor: string | null
  creadoEn: string
  decididoPor: string | null
  decididoEn: string | null
  motivoDecision: string | null
  dentro: boolean
  zonaHoraria: string
  horario: Franja[]
  objetos: ObjetoAcceso[]
  historial: TransicionAcceso[] | null
  movimientos: MovimientoHistorial[] | null
  motivosIngreso: string[] | null
}

export interface Pagina<T> { registros: T[]; total: number; pagina: number; tamano: number }

export interface RegistroAccesoEntrada {
  idPersona: number
  idCategoriaAcceso: number
  idSede: number
  idArea: number
  idMembresiaResponsable: number
  motivo: string | null
  observacion: string | null
  modalidad: Modalidad
  fechaInicio: string
  fechaFin: string | null
  horaInicio: string | null
  horaFin: string | null
  franjas: Franja[]
  usaHorarioBase: boolean
  objetos: { tipoObjeto: TipoObjeto; idObjeto: number }[]
}

export interface CategoriaAcceso {
  idCategoriaAcceso: number
  codigo: string
  nombre: string
  descripcion: string | null
  flujoAprobacion: FlujoAprobacion
  modalidadesPermitidas: Modalidad[]
  exigeAnfitrion: boolean
  exigeMotivo: boolean
  permiteVehiculos: boolean
  permiteDispositivos: boolean
  orden: number
  activo: boolean
}

export interface OpcionesAcceso {
  categorias: CategoriaAcceso[]
  sedes: { id: number; nombre: string }[]
  /** `responsables`: membresías designadas; vacío = cualquier anfitrión activo. */
  areas: { id: number; idSede: number; nombre: string; responsables: number[] }[]
  anfitriones: { id: number; nombre: string }[]
  puntosAcceso: { id: number; idSede: number; nombre: string; modoAcceso: 'Entrada' | 'Salida' | 'Ambos' }[]
  permitePermanente: boolean
  fueraDeHorario: 'Bloquear' | 'Alertar'
}

export interface Presencia {
  idPermanencia: number
  idAutorizacion: number
  idPersona: number
  persona: string
  documentoPersona: string
  categoria: string | null
  idSede: number
  sede: string
  area: string
  anfitrion: string | null
  puntoEntrada: string
  entradaEn: string
  segundosDentro: number
  zonaHoraria: string
  /** Vehículos y dispositivos de este ingreso que siguen dentro. */
  objetosDentro: string[]
}

/** `objetosDentro` (en la salida): vehículos y dispositivos que siguen dentro; la salida de la persona no los cierra (D-10). */
export interface MovimientoConfirmado { idAutorizacion: number; idVisita: number; idMovimiento: number; idPermanencia: number; tipoMovimiento: string; ocurridoEn: string; alertas: string[]; objetosDentro: string[] }

export interface MovimientoObjetoConfirmado {
  idAutorizacion: number
  idAutorizacionObjeto: number
  tipoObjeto: TipoObjeto
  idMovimientoObjeto: number
  idVisita: number
  tipoMovimiento: 'Entrada' | 'Salida'
  ocurridoEn: string
  duracionSegundos: number | null
  /** En la salida: la persona responsable ya salió de la sede. */
  personaFuera: boolean
}

export interface ObjetoDentro {
  idMovimientoEntrada: number
  idAutorizacion: number
  idAutorizacionObjeto: number
  tipoObjeto: TipoObjeto
  objeto: string
  idPersona: number
  persona: string
  documentoPersona: string
  personaDentro: boolean
  idSede: number
  sede: string
  puntoEntrada: string
  entradaEn: string
  segundosDentro: number
  zonaHoraria: string
}

export interface EventoBitacora {
  idAuditoria: number
  accion: string
  entidad: string
  idRegistro: string
  resultado: string
  usuario: string | null
  valorAnterior: string | null
  valorNuevo: string | null
  direccionIp: string | null
  agenteUsuario: string | null
  ocurridoEn: string
}

export interface FiltrosAcceso { buscar?: string; estado?: string; idCategoria?: string; idSede?: string; modalidad?: string; pagina?: number; tamano?: number }
export interface FiltrosHistorial { buscar?: string; idPersona?: number; idAnfitrion?: string; idSede?: string; placa?: string; tipo?: string; resultado?: string; desde?: string; hasta?: string; pagina?: number }

export type AccionRegistro = 'aprobar' | 'rechazar' | 'revocar' | 'cancelar'
export type AccionObjeto = 'aprobar' | 'rechazar' | 'revocar'

const json = (method: string, body: unknown, headers?: Record<string, string>): RequestInit => ({ method, body: JSON.stringify(body), headers })
export const consulta = (params: Record<string, string | number | undefined>) =>
  new URLSearchParams(Object.entries(params).filter(([, v]) => v !== undefined && v !== '').map(([k, v]) => [k, String(v)])).toString()

export const consultarAccesos = (filtros: FiltrosAcceso = {}) => apiFetch<Pagina<RegistroAcceso>>(`/api/accesos?${consulta({ ...filtros })}`)
export const consultarAcceso = (id: number) => apiFetch<RegistroAcceso>(`/api/accesos/${id}`)
export const consultarOpcionesAcceso = () => apiFetch<OpcionesAcceso>('/api/accesos/opciones')
export const crearAcceso = (data: RegistroAccesoEntrada) => apiFetch<RegistroAcceso>('/api/accesos', json('POST', data))
export const decidirAcceso = (id: number, accion: AccionRegistro, motivo: string | null) =>
  apiFetch<RegistroAcceso>(`/api/accesos/${id}/${accion}`, json('POST', { motivo }))
export const decidirObjeto = (id: number, idObjeto: number, accion: AccionObjeto, motivo: string | null) =>
  apiFetch<RegistroAcceso>(`/api/accesos/${id}/objetos/${idObjeto}/${accion}`, json('POST', { motivo }))
export const consultarBitacora = (id: number) => apiFetch<EventoBitacora[]>(`/api/accesos/${id}/bitacora`)
export const consultarPresencia = (idSede?: string) => apiFetch<Presencia[]>(`/api/accesos/presencia${idSede ? `?idSede=${idSede}` : ''}`)
export const consultarHistorial = (filtros: FiltrosHistorial = {}) => apiFetch<Pagina<MovimientoHistorial>>(`/api/accesos/historial?${consulta({ ...filtros })}`)
export const rutaExportarHistorial = (filtros: FiltrosHistorial = {}) => `/api/accesos/historial/exportar?${consulta({ ...filtros, pagina: undefined })}`

/**
 * Entrada (por registro) o salida (por permanencia). La clave de idempotencia la genera quien llama una vez por intento
 * del operador y la reutiliza en los reintentos: un reenvío devuelve el mismo resultado sin duplicar el movimiento.
 */
export const registrarEntrada = (idAutorizacion: number, idPuntoAcceso: number, observacion: string | null, clave: string) =>
  apiFetch<MovimientoConfirmado>(`/api/accesos/${idAutorizacion}/entrada`, json('POST', { idPuntoAcceso, observacion }, { 'Idempotency-Key': clave }))
export const registrarSalida = (idPermanencia: number, idPuntoAcceso: number, observacion: string | null, clave: string) =>
  apiFetch<MovimientoConfirmado>(`/api/accesos/permanencias/${idPermanencia}/salida`, json('POST', { idPuntoAcceso, observacion }, { 'Idempotency-Key': clave }))

/** Entrada o salida de un vehículo o dispositivo del registro (ADR-030). Misma regla de idempotencia que la persona. */
export const registrarMovimientoObjeto = (idAutorizacion: number, idObjeto: number, tipo: 'entrada' | 'salida', idPuntoAcceso: number, observacion: string | null, clave: string) =>
  apiFetch<MovimientoObjetoConfirmado>(`/api/accesos/${idAutorizacion}/objetos/${idObjeto}/${tipo}`, json('POST', { idPuntoAcceso, observacion }, { 'Idempotency-Key': clave }))
export const consultarObjetosDentro = (idSede?: string) => apiFetch<ObjetoDentro[]>(`/api/accesos/objetos-dentro${idSede ? `?idSede=${idSede}` : ''}`)

export const nuevaClave = () => (globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`)
