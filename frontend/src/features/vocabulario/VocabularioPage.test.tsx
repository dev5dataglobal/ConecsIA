import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

const contexto = {
  usuario: { id: 'u1', nombre: 'Ana Admin', correo: 'ana@ejemplo.test' },
  organizacion: { id: 'o1', nombre: 'Universidad' },
  permisos: ['configuracion.modificar'],
  alcanceTotalSedes: true,
  sedesAsignadas: [],
}
const claves = ['organizacion', 'sede', 'area']

function api(
  terminos: Record<string, { singular: string; plural: string }>,
  alGuardar?: (c: unknown) => void,
) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
    const url = String(entrada)
    if (init?.method === 'PUT') {
      alGuardar?.(JSON.parse(String(init.body)))
      return Promise.resolve(respuestaJson({ claves, terminos: {}, version: 2 }))
    }
    if (url.endsWith('/api/v1/me')) return Promise.resolve(respuestaJson(contexto))
    if (url.endsWith('/api/v1/vocabulario'))
      return Promise.resolve(respuestaJson({ claves, terminos, version: 1 }))
    return Promise.resolve(respuestaJson([]))
  })
}

describe('Vocabulario por organización (HU-012)', () => {
  afterEach(() => sessionStorage.clear())

  it('la etiqueta personalizada reemplaza el término en el menú', async () => {
    guardarSesionDesarrollo('u1')
    api({ sede: { singular: 'Campus', plural: 'Campus' } })

    renderApp('/configuracion/vocabulario')

    expect(await screen.findByRole('link', { name: 'Campus' })).toHaveAttribute('href', '/sedes')
  })

  it('guarda solo los términos completos con la versión leída', async () => {
    guardarSesionDesarrollo('u1')
    let enviado: unknown
    api({}, (c) => (enviado = c))
    const user = userEvent.setup()

    renderApp('/configuracion/vocabulario')
    await user.type(await screen.findByLabelText('Singular de sede'), 'Campus')
    await user.type(screen.getByLabelText('Plural de sede'), 'Campus')
    await user.type(screen.getByLabelText('Singular de area'), 'Facultad')
    await user.click(screen.getByRole('button', { name: 'Guardar vocabulario' }))

    await waitFor(() =>
      expect(enviado).toEqual({
        terminos: { sede: { singular: 'Campus', plural: 'Campus' } },
        version: 1,
      }),
    )
  })
})
