import { screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

describe('Catálogos de la organización (HU-010)', () => {
  afterEach(() => sessionStorage.clear())

  it('distingue los elementos de plantilla de los personalizados', async () => {
    guardarSesionDesarrollo('u1')
    vi.spyOn(globalThis, 'fetch').mockImplementation((entrada) => {
      const url = String(entrada)
      if (url.endsWith('/api/v1/me'))
        return Promise.resolve(
          respuestaJson({
            usuario: { id: 'u1', nombre: 'Ana', correo: 'a@e.test' },
            organizacion: { id: 'o1', nombre: 'Los Pinos' },
            permisos: [],
            alcanceTotalSedes: true,
            sedesAsignadas: [],
          }),
        )
      if (url.endsWith('/catalogos/tipos-area'))
        return Promise.resolve(
          respuestaJson([
            {
              id: '1',
              catalogo: 'tipos-area',
              clave: 'torre',
              nombre: 'Torre',
              origen: 'plantilla:conjunto-residencial:v1',
              estado: 'Activo',
            },
            {
              id: '2',
              catalogo: 'tipos-area',
              clave: 'mezzanine',
              nombre: 'Mezzanine',
              origen: 'personalizado',
              estado: 'Activo',
            },
          ]),
        )
      return Promise.resolve(respuestaJson([]))
    })

    renderApp('/configuracion/catalogos')

    const lista = await screen.findByRole('list', { name: 'Elementos del catálogo' })
    expect(await within(lista).findByText(/De plantilla/)).toBeInTheDocument()
    expect(within(lista).getByText(/Personalizado/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Agregar' })).not.toBeInTheDocument()
  })
})
