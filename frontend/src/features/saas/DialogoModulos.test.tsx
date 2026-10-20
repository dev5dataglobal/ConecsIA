import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

const pinos = {
  id: 'o1',
  nombre: 'Los Pinos',
  tipo: 'club',
  identificacionLegal: null,
  zonaHoraria: null,
  contactoAdministrativo: null,
  estado: 'Activa',
  version: 3,
  creadaEn: '2026-10-19T00:00:00Z',
  actualizadaEn: '2026-10-19T00:00:00Z',
  inactivadaEn: null,
}
const modulos = [
  {
    modulo: 'vehiculos',
    nombre: 'Vehículos',
    habilitado: true,
    cambiadoEn: null,
    cambiadoPor: null,
  },
  {
    modulo: 'equipos',
    nombre: 'Equipos externos',
    habilitado: false,
    cambiadoEn: null,
    cambiadoPor: null,
  },
]

describe('Módulos por organización (HU-011)', () => {
  afterEach(() => sessionStorage.clear())

  it('envía la lista completa de módulos habilitados', async () => {
    guardarSesionDesarrollo('s1')
    let enviado: unknown
    vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
      const url = String(entrada)
      if (init?.method === 'PUT') {
        enviado = JSON.parse(String(init.body))
        return Promise.resolve(respuestaJson(modulos))
      }
      if (url.endsWith('/api/v1/me'))
        return Promise.resolve(
          respuestaJson({ status: 403, code: 'SIN_MEMBRESIA' }, 403, 'application/problem+json'),
        )
      if (url.endsWith('/me/plataforma'))
        return Promise.resolve(
          respuestaJson({
            usuario: { id: 's1', nombre: 'Sofía', correo: 's@e.test' },
            permisosSaas: ['saas.organizaciones.consultar', 'saas.organizaciones.administrar'],
          }),
        )
      if (url.endsWith('/o1/modulos')) return Promise.resolve(respuestaJson(modulos))
      if (url.endsWith('/tipos')) return Promise.resolve(respuestaJson([]))
      if (url.includes('/api/v1/saas/organizaciones'))
        return Promise.resolve(
          respuestaJson({ elementos: [pinos], total: 1, pagina: 1, tamano: 20 }),
        )
      return Promise.resolve(respuestaJson({ status: 'Healthy' }))
    })
    const user = userEvent.setup()

    renderApp('/saas/organizaciones')
    await user.click(await screen.findByRole('button', { name: 'Módulos' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Módulos de Los Pinos' })
    expect(await within(dialogo).findByLabelText('Vehículos')).toBeChecked()
    await user.click(within(dialogo).getByLabelText('Equipos externos'))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    await waitFor(() => expect(enviado).toEqual({ habilitados: ['vehiculos', 'equipos'] }))
  })
})
