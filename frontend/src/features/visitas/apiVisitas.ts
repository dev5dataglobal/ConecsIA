import { apiFetch } from '../../shared/api/http'

/*
 * Visitas y su ciclo operativo. Ninguna petición envía organización, operador ni responsable que aprueba: el servidor
 * los toma del contexto validado y de la identidad autenticada. El estado, la vigencia y los motivos de denegación los
 * decide el servidor; la pantalla solo los presenta.
 */

export type EstadoVisita = 'Pendiente' | 'Autorizada' | 'Ingreso' | 'Finalizada' | 'Rechazada' | 'Cancelada' | 'Vencida'

export interface VisitaLista {
  idVisita: number
  idPersona: number
  persona: string
  documentoPersona: string
  personaActiva: boolean
  /** Tipo de permiso de acceso actual de la persona. */
  /** Histórico: visita registrada por excepción al tipo de permiso de la persona (clasificación retirada el 2026-10-28). */
  excepcionTipoAcceso: boolean
  motivoExcepcion: string | null
  idSede: number
  sede: string
  idArea: number
  area: string
  idMembresiaResponsable: number
  responsable: string | null
  motivo: string | null
  fechaEsperada: string | null
  horaEsperadaInicio: string | null
  horaEsperadaFin: string | null
  idAutorizacion: number | null
  politicaAplicada: string | null
  estado: EstadoVisita
  creadoEn: string
  entradaEn: string | null
  salidaEn: string | null
  zonaHoraria: string
}

export interface MovimientoVisita {
  idMovimiento: number
  tipoMovimiento: 'Entrada' | 'Salida'
  resultado: string
  puntoAcceso: string
  /** Zona de acceso retirada en la que se registró (solo movimientos anteriores a ADR-028). */
  zonaHistorica: string | null
  operador: string | null
  ocurridoEn: string
  observacion: string | null
}

export interface EventoVisita {
  idEvento: number
  accion: 'Registrar' | 'VincularAutorizacion' | 'Cancelar' | 'ActualizarEstado'
  actor: string | null
  ocurridoEn: string
  motivo: string | null
}

export interface VisitaDetalle {
  visita: VisitaLista
  registradaPor: string | null
  observacion: string | null
  motivoCierre: string | null
  actualizadaPor: string | null
  actualizadaEn: string | null
  autorizacion: {
    idAutorizacion: number
    estado: string
    modalidad: string
    fechaInicio: string
    fechaFin: string | null
    cupoIngresos: number | null
    ingresosConsumidos: number
    horarios: { diaSemana: number; horaInicio: string; horaFin: string }[]
  } | null
  permanencia: { idPermanencia: number; estado: 'Abierta' | 'Cerrada'; abiertaEn: string; cerradaEn: string | null; duracionSegundos: number | null } | null
  movimientos: MovimientoVisita[]
  /** Eventos de ciclo de vida leídos de la auditoría (registro, vínculo, cancelación…). */
  historial: EventoVisita[]
  evaluacion: { puedeIngresar: boolean; puedeSalir: boolean; motivos: string[] }
}

export interface PaginaVisitas {
  registros: VisitaLista[]
  total: number
  pagina: number
  tamano: number
  indicadores: { pendientes: number; autorizadas: number; dentro: number; finalizadas: number }
}

export interface OpcionesVisita {
  sedes: { id: number; nombre: string }[]
  areas: { id: number; idSede: number; nombre: string }[]
  responsables: { idMembresia: number; nombre: string; areas: number[] }[]
  puntosAcceso: { id: number; idSede: number; nombre: string; modoAcceso: 'Entrada' | 'Salida' | 'Ambos' }[]
  permiteSinAutorizacion: boolean
}

export interface AutorizacionDisponible {
  idAutorizacion: number
  estado: string
  modalidad: string
  idSede: number
  idArea: number
  fechaInicio: string
  fechaFin: string | null
}

export interface Presencia {
  idVisita: number
  idPermanencia: number
  idPersona: number
  persona: string
  documentoPersona: string
  idSede: number
  sede: string
  area: string
  responsable: string | null
  puntoEntrada: string
  entradaEn: string
  segundosDentro: number
  zonaHoraria: string
}

export interface VisitaEntrada {
  idPersona: number
  idSede: number
  idArea: number
  idMembresiaResponsable: number
  motivo: string
  fechaEsperada: string | null
  horaEsperadaInicio: string | null
  horaEsperadaFin: string | null
  idAutorizacion: number | null
  observacion: string | null
}

export interface FiltrosVisitas {
  buscar?: string
  estado?: string
  idSede?: string
  idArea?: string
  idResponsable?: string
  desde?: string
  hasta?: string
  orden?: string
  pagina?: number
  tamano?: number
}

const json = (method: string, body: unknown, cabeceras: Record<string, string> = {}): RequestInit => ({
  method, headers: { 'Content-Type': 'application/json', ...cabeceras }, body: JSON.stringify(body),
})

export function consultarVisitas(filtros: FiltrosVisitas) {
  const params = new URLSearchParams()
  for (const [clave, valor] of Object.entries(filtros)) if (valor !== undefined && valor !== '') params.set(clave, String(valor))
  return apiFetch<PaginaVisitas>(`/api/visitas?${params}`)
}

export const consultarVisita = (id: number) => apiFetch<VisitaDetalle>(`/api/visitas/${id}`)
export const consultarOpcionesVisita = () => apiFetch<OpcionesVisita>('/api/visitas/opciones')
export const consultarAutorizacionesDisponibles = (idPersona: number) => apiFetch<AutorizacionDisponible[]>(`/api/visitas/autorizaciones?idPersona=${idPersona}`)
export const consultarPresencia = (idSede?: string) => apiFetch<Presencia[]>(`/api/visitas/presencia${idSede ? `?idSede=${idSede}` : ''}`)
export const registrarVisita = (data: VisitaEntrada) => apiFetch<VisitaDetalle>('/api/visitas', json('POST', data))
export const vincularAutorizacion = (id: number, idAutorizacion: number) => apiFetch<VisitaDetalle>(`/api/visitas/${id}/autorizacion`, json('POST', { idAutorizacion }))
export const cancelarVisita = (id: number, motivo: string) => apiFetch<VisitaDetalle>(`/api/visitas/${id}/cancelar`, json('POST', { motivo }))

/**
 * Entrada o salida. La clave de idempotencia la genera quien llama una vez por intento del operador y la reutiliza en
 * los reintentos: si la red falla tras guardar, el reenvío devuelve el mismo resultado sin duplicar el movimiento.
 */
export const registrarMovimiento = (id: number, tipo: 'entrada' | 'salida', idPuntoAcceso: number, observacion: string | null, clave: string) =>
  apiFetch<VisitaDetalle>(`/api/visitas/${id}/${tipo}`, json('POST', { idPuntoAcceso, observacion }, { 'Idempotency-Key': clave }))

export const nuevaClave = () => (globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`)
