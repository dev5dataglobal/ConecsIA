import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

describe('ZonasPage (HU-015)', () => {
  afterEach(() => sessionStorage.clear())

  it('un punto de acceso se crea con su modo y una zona interna sin modo', async () => {
    guardarSesionDesarrollo('u1')
    const envios: unknown[] = []
    vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
      const url = String(entrada)
      if (init?.method === 'POST') {
        envios.push(JSON.parse(String(init.body)))
        return Promise.resolve(respuestaJson({}, 201))
      }
      if (url.endsWith('/api/v1/me'))
        return Promise.resolve(
          respuestaJson({
            usuario: { id: 'u1', nombre: 'Ana', correo: 'a@e.test' },
            organizacion: { id: 'o1', nombre: 'Los Pinos' },
            permisos: ['zonas.consultar', 'zonas.crear'],
            alcanceTotalSedes: true,
            sedesAsignadas: [],
            puntosAsignados: [],
          }),
        )
      if (url.endsWith('/sedes/s1/zonas')) return Promise.resolve(respuestaJson([]))
      return Promise.resolve(respuestaJson([]))
    })
    const user = userEvent.setup()

    renderApp('/sedes/s1/zonas')
    await user.type(await screen.findByLabelText('Nombre'), 'Portería')
    await user.click(screen.getByLabelText('Es punto de acceso'))
    await user.selectOptions(screen.getByLabelText('Modo'), 'Entrada')
    await user.click(screen.getByRole('button', { name: 'Nueva zona' }))
    await waitFor(() => expect(envios).toHaveLength(1))

    await user.type(screen.getByLabelText('Nombre'), 'Piso 3')
    await user.click(screen.getByLabelText('Es punto de acceso'))
    await user.click(screen.getByRole('button', { name: 'Nueva zona' }))

    await waitFor(() =>
      expect(envios).toEqual([
        { nombre: 'Portería', esPuntoAcceso: true, modo: 'Entrada' },
        { nombre: 'Piso 3', esPuntoAcceso: false, modo: null },
      ]),
    )
  })
})
