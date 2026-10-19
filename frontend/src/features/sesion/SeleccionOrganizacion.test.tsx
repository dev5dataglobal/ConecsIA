import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { leerOrganizacionSeleccionada } from './organizacionSeleccionada'
import { guardarSesionDesarrollo } from './sesionDesarrollo'

const ORG_A = '0199f000-0000-7000-8000-00000000000a'
const ORG_B = '0199f000-0000-7000-8000-00000000000b'

const membresias = [
  { organizacionId: ORG_A, nombre: 'Conjunto Alfa' },
  { organizacionId: ORG_B, nombre: 'Club Beta' },
]

function contexto(organizacion: string, nombre: string) {
  return {
    usuario: { id: 'u1', nombre: 'Usuaria Multi', correo: 'multi@ejemplo.test' },
    organizacion: { id: organizacion, nombre },
    permisos: [],
    alcanceTotalSedes: true,
    sedesAsignadas: [],
  }
}

/** Sedes vacías y salud: lo que pide el resto de la aplicación. */
function respuestaFueraDeSesion(url: string) {
  if (url.includes('/api/v1/sedes'))
    return respuestaJson({ elementos: [], total: 0, pagina: 1, tamano: 20 })
  return respuestaJson({ status: 'Healthy' })
}

/** Simula la API: /me exige selección cuando hay varias membresías (D-03b). */
function apiMultiMembresia() {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
    const url = String(entrada)
    const seleccion = new Headers(init?.headers).get('X-Organizacion-Id')
    if (url.endsWith('/api/v1/me/membresias')) return Promise.resolve(respuestaJson(membresias))
    if (url.endsWith('/api/v1/me')) {
      if (seleccion === ORG_B) return Promise.resolve(respuestaJson(contexto(ORG_B, 'Club Beta')))
      if (seleccion === ORG_A)
        return Promise.resolve(respuestaJson(contexto(ORG_A, 'Conjunto Alfa')))
      return Promise.resolve(
        respuestaJson(
          {
            status: 403,
            title: 'Organización no seleccionada',
            code: 'ORGANIZACION_NO_SELECCIONADA',
          },
          403,
          'application/problem+json',
        ),
      )
    }
    return Promise.resolve(respuestaFueraDeSesion(url))
  })
}

describe('Selección de organización (HU-007)', () => {
  afterEach(() => sessionStorage.clear())

  it('con varias membresías pide elegir y envía la selección en cada petición', async () => {
    guardarSesionDesarrollo('u1')
    const fetch = apiMultiMembresia()
    const user = userEvent.setup()

    renderApp('/sedes')

    expect(await screen.findByRole('heading', { name: 'Elige la organización' })).toBeVisible()
    await user.click(await screen.findByRole('button', { name: 'Club Beta' }))

    const selector = await screen.findByRole('combobox', { name: 'Organización' })
    await waitFor(() => expect(selector).toHaveValue(ORG_B))
    expect(leerOrganizacionSeleccionada()).toBe(ORG_B)
    const ultimaMe = fetch.mock.calls.filter(([u]) => String(u).endsWith('/api/v1/me')).at(-1)
    expect(new Headers(ultimaMe?.[1]?.headers).get('X-Organizacion-Id')).toBe(ORG_B)
  })

  it('permite cambiar de organización desde la cabecera', async () => {
    guardarSesionDesarrollo('u1')
    sessionStorage.setItem('conecsia.organizacion', ORG_A)
    apiMultiMembresia()
    const user = userEvent.setup()

    renderApp('/sedes')

    const selector = await screen.findByRole('combobox', { name: 'Organización' })
    expect(selector).toHaveValue(ORG_A)
    await user.selectOptions(selector, ORG_B)

    await waitFor(() =>
      expect(screen.getByRole('combobox', { name: 'Organización' })).toHaveValue(ORG_B),
    )
    expect(leerOrganizacionSeleccionada()).toBe(ORG_B)
  })

  it('una selección rechazada por el servidor se olvida y vuelve a pedir elegir', async () => {
    guardarSesionDesarrollo('u1')
    sessionStorage.setItem('conecsia.organizacion', 'ajena')
    vi.spyOn(globalThis, 'fetch').mockImplementation((entrada) => {
      const url = String(entrada)
      if (url.endsWith('/api/v1/me/membresias')) return Promise.resolve(respuestaJson(membresias))
      if (url.endsWith('/api/v1/me'))
        return Promise.resolve(
          respuestaJson(
            { status: 403, title: 'Acceso denegado', code: 'ORGANIZACION_NO_PERMITIDA' },
            403,
            'application/problem+json',
          ),
        )
      return Promise.resolve(respuestaFueraDeSesion(url))
    })

    renderApp('/sedes')

    expect(await screen.findByRole('heading', { name: 'Elige la organización' })).toBeVisible()
    expect(leerOrganizacionSeleccionada()).toBeNull()
  })
})
