import { screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { guardarAccessToken, olvidarSesion } from '../sesion/sesion'
import { renderApp, respuestaJson } from '../../test/render'
import { modulosPermitidos } from '../../test/modulos'

function tokenFalso(payload: Record<string, unknown>) {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/=+$/, '')
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`
}

const dashboardVacio = {
  organizacion: { idOrganizacion: 7, nombre: 'Organización de prueba' },
  sedes: [{ idSede: 1, nombre: 'Sede de prueba' }],
  resumen: { personasDentro: 3, visitas: 12, autorizacionesActivas: 5, alertasPendientes: 0 },
  visitasPorDia: [],
  distribucionPorTipo: [],
  personasDentro: [],
  actividadReciente: [],
}

function simularApi(modulos: ReturnType<typeof modulosPermitidos>) {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) =>
    String(entrada).includes('/api/app-modulos/mios') ? respuestaJson(modulos) : respuestaJson(dashboardVacio),
  )
}

describe('Shell y dashboard', () => {
  beforeEach(() => {
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '9', email: 'operador@ejemplo.test' }))
  })

  it('construye el menú con los módulos que autoriza el servidor y marca la sección activa', async () => {
    simularApi(modulosPermitidos('dashboard', 'visitas'))
    renderApp('/dashboard')

    const nav = await screen.findByRole('navigation', { name: 'Navegación principal' })
    expect(await within(nav).findByRole('link', { name: 'Dashboard' })).toHaveAttribute('aria-current', 'page')
    expect(within(nav).getByText('Visitas')).toBeInTheDocument()
    expect(within(nav).getByText('Pronto')).toBeInTheDocument()
    expect(within(nav).queryByText('Administración')).not.toBeInTheDocument()
    expect(within(nav).queryByText('Personas')).not.toBeInTheDocument()
  })

  it('presenta los indicadores reales, el contexto y los estados vacíos', async () => {
    simularApi(modulosPermitidos('dashboard'))
    renderApp('/dashboard')

    expect(await screen.findByText('Organización de prueba')).toBeInTheDocument()
    const metricas = screen.getByRole('region', { name: 'Indicadores principales' })
    expect(within(metricas).getByText('12')).toBeInTheDocument()
    expect(within(metricas).getByText('Sin pendientes')).toBeInTheDocument()
    expect(screen.getByText('No hay visitas en este periodo')).toBeInTheDocument()
    expect(screen.getByText('No hay actividad reciente')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /Administración/ })).not.toBeInTheDocument()
  })

  it('muestra acceso denegado si el servidor no concede el módulo de la ruta', async () => {
    simularApi(modulosPermitidos('visitas'))
    renderApp('/dashboard')

    expect(await screen.findByRole('heading', { name: 'Acceso denegado' })).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Indicadores principales' })).not.toBeInTheDocument()
  })
})
