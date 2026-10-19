import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

const plataforma = {
  usuario: { id: 's1', nombre: 'Sofía Superadmin', correo: 'super@ejemplo.test' },
  permisosSaas: ['saas.organizaciones.consultar', 'saas.organizaciones.administrar'],
}
const tipos = [
  { clave: 'club', nombre: 'Club' },
  { clave: 'otro', nombre: 'Otro' },
]
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

function sinMembresia() {
  return respuestaJson(
    { status: 403, title: 'Acceso denegado', code: 'SIN_MEMBRESIA' },
    403,
    'application/problem+json',
  )
}

function api(alEnviar?: (url: string, cuerpo: unknown) => void) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
    const url = String(entrada)
    if (init?.method === 'POST') {
      alEnviar?.(url, init.body ? JSON.parse(String(init.body)) : null)
      return Promise.resolve(respuestaJson(pinos, 201))
    }
    // Sin membresía, la API rechaza igual cualquier recurso de organización.
    if (url.endsWith('/api/v1/me') || url.includes('/api/v1/sedes'))
      return Promise.resolve(sinMembresia())
    if (url.endsWith('/api/v1/me/plataforma')) return Promise.resolve(respuestaJson(plataforma))
    if (url.endsWith('/api/v1/saas/organizaciones/tipos'))
      return Promise.resolve(respuestaJson(tipos))
    if (url.includes('/api/v1/saas/organizaciones'))
      return Promise.resolve(respuestaJson({ elementos: [pinos], total: 1, pagina: 1, tamano: 20 }))
    return Promise.resolve(respuestaJson({ status: 'Healthy' }))
  })
}

describe('Consola SaaS de organizaciones (HU-009)', () => {
  afterEach(() => sessionStorage.clear())

  it('el superadministrador sin membresías llega a la consola SaaS', async () => {
    guardarSesionDesarrollo('s1')
    api()

    renderApp('/sedes')

    expect(await screen.findByRole('heading', { name: 'Organizaciones' })).toBeVisible()
    const tabla = await screen.findByRole('table', { name: 'Organizaciones de la plataforma' })
    expect(within(tabla).getByText('Los Pinos')).toBeInTheDocument()
    expect(await within(tabla).findByText('Club')).toBeInTheDocument()
  })

  it('crea una organización con su primer administrador', async () => {
    guardarSesionDesarrollo('s1')
    let enviado: unknown
    api((url, cuerpo) => {
      if (url.endsWith('/api/v1/saas/organizaciones')) enviado = cuerpo
    })
    const user = userEvent.setup()

    renderApp('/saas/organizaciones')
    await user.click(await screen.findByRole('button', { name: 'Nueva organización' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nueva organización' })
    await user.type(within(dialogo).getByLabelText('Nombre'), 'Club Nuevo')
    await within(dialogo).findByRole('option', { name: 'Club' })
    await user.selectOptions(within(dialogo).getByLabelText('Tipo de organización'), 'club')
    await user.type(within(dialogo).getByLabelText('Correo del administrador'), 'ana@club.test')
    await user.type(within(dialogo).getByLabelText('Nombre del administrador'), 'Ana')
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    await waitFor(() =>
      expect(enviado).toMatchObject({
        nombre: 'Club Nuevo',
        tipo: 'club',
        administrador: { correo: 'ana@club.test', nombre: 'Ana' },
      }),
    )
  })

  it('advierte los efectos antes de inactivar', async () => {
    guardarSesionDesarrollo('s1')
    let inactivada = ''
    api((url) => {
      if (url.endsWith('/inactivar')) inactivada = url
    })
    const user = userEvent.setup()

    renderApp('/saas/organizaciones')
    await user.click(await screen.findByRole('button', { name: 'Inactivar' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Inactivar organización' })
    expect(within(dialogo).getByText(/Sus datos e historial se conservan/)).toBeInTheDocument()
    await user.click(within(dialogo).getByRole('button', { name: 'Confirmar' }))

    await waitFor(() => expect(inactivada).toContain('/api/v1/saas/organizaciones/o1/inactivar'))
  })
})
