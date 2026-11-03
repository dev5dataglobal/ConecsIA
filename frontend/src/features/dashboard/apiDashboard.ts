import { apiFetch } from '../../shared/api/http'

export type PeriodoDashboard = 'hoy' | '7d'
export interface Dashboard {
  organizacion: { idOrganizacion: number; nombre: string }
  sedes: { idSede: number; nombre: string }[]
  resumen: { personasDentro: number; visitas: number; autorizacionesActivas: number; alertasPendientes: number }
  visitasPorDia: { fecha: string; total: number }[]
  distribucionPorTipo: { tipo: string; total: number }[]
  personasDentro: { idPersona: number; persona: string; tipo: string; entradaEn: string; zonaDestino?: string | null; estado: string }[]
  actividadReciente: { tipo: string; titulo: string; detalle?: string | null; ocurrioEn: string }[]
}

export function consultarDashboard(periodo: PeriodoDashboard, sedeId?: number) {
  const query = new URLSearchParams({ periodo })
  if (sedeId) query.set('sedeId', String(sedeId))
  return apiFetch<Dashboard>(`/api/dashboard?${query.toString()}`)
}
