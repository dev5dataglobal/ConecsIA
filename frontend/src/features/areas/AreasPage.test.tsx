import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

const contexto = {
  usuario: { id: 'u1', nombre: 'Ana Admin', correo: 'ana@ejemplo.test' },
  organizacion: { id: 'o1', nombre: 'Los Pinos' },
  permisos: [
    'areas.consultar',
    'areas.crear',
    'areas.asignar',
    'areas.inactivar',
    'usuarios.consultar',
  ],
  alcanceTotalSedes: true,
  sedesAsignadas: [],
}
const torre = {
  id: 'a1',
  sedeId: 's1',
  nombre: 'Torre 1',
  tipo: null,
  estado: 'Activa',
  version: 1,
  responsables: [],
  autorizadores: [],
}
const oscar = {
  usuarioId: 'u2',
  nombre: 'Óscar Operador',
  correo: 'o@e.test',
  estado: 'Activo',
  roles: [],
  alcanceTotalSedes: true,
  sedes: [],
}

function api(alEnviar: (url: string, cuerpo: unknown) => void) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
    const url = String(entrada)
    if (init?.method && init.method !== 'GET') {
      alEnviar(url, JSON.parse(String(init.body ?? 'null')))
      return Promise.resolve(respuestaJson(torre, init.method === 'POST' ? 201 : 200))
    }
    if (url.endsWith('/api/v1/me')) return Promise.resolve(respuestaJson(contexto))
    if (url.endsWith('/sedes/s1/areas')) return Promise.resolve(respuestaJson([torre]))
    if (url.endsWith('/api/v1/usuarios')) return Promise.resolve(respuestaJson([oscar]))
    return Promise.resolve(respuestaJson([]))
  })
}

describe('AreasPage (HU-014)', () => {
  afterEach(() => sessionStorage.clear())

  it('crea un área en la sede de la ruta', async () => {
    guardarSesionDesarrollo('u1')
    const envios: [string, unknown][] = []
    api((url, cuerpo) => envios.push([url, cuerpo]))
    const user = userEvent.setup()

    renderApp('/sedes/s1/areas')
    expect(await screen.findByText('Torre 1')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Nombre'), 'Recepción')
    await user.click(screen.getByRole('button', { name: 'Nueva área' }))

    await waitFor(() =>
      expect(envios).toContainEqual(['/api/v1/sedes/s1/areas', { nombre: 'Recepción', tipo: '' }]),
    )
  })

  it('asigna autorizadores advirtiendo que no conceden permisos', async () => {
    guardarSesionDesarrollo('u1')
    const envios: [string, unknown][] = []
    api((url, cuerpo) => envios.push([url, cuerpo]))
    const user = userEvent.setup()

    renderApp('/sedes/s1/areas')
    await user.click(await screen.findByRole('button', { name: 'Autorizadores' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Autorizadores de Torre 1' })
    expect(within(dialogo).getByText(/no concede permiso/)).toBeInTheDocument()
    await user.click(await within(dialogo).findByLabelText('Óscar Operador'))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    await waitFor(() =>
      expect(envios).toContainEqual(['/api/v1/areas/a1/autorizadores', { usuarios: ['u2'] }]),
    )
  })
})
