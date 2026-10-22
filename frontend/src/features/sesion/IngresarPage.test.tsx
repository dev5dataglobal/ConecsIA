import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { leerAccessToken, olvidarSesion } from './sesion'
import { renderApp, respuestaJson } from '../../test/render'

describe('Inicio de sesión real', () => {
  beforeEach(() => {
    sessionStorage.clear()
    olvidarSesion()
  })

  it('envía las credenciales al contrato de autenticación y conserva la sesión', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.endsWith('/api/auth/login')) {
        return respuestaJson({
          accessToken: 'jwt-1',
          expiresAtUtc: '2030-01-01T00:00:00Z',
          usuario: { idUsuario: 1, correo: 'ana@ejemplo.test', activo: true },
        })
      }
      return respuestaJson({ status: 404, title: 'No encontrado' }, 404, 'application/problem+json')
    })

    renderApp('/ingresar')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'ana@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'una-frase-larga')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    const login = fetch.mock.calls.find(([, init]) => init?.method === 'POST')?.[1]
    expect(String(login?.body)).toBe(
      JSON.stringify({ correo: 'ana@ejemplo.test', password: 'una-frase-larga' }),
    )
    expect(new Headers(login?.headers).get('Authorization')).toBeNull()
    expect(leerAccessToken()).toBe('jwt-1')
    expect(sessionStorage.getItem('conecsia.sesionIniciada')).toBe('1')
  })

  it('presenta el error genérico y limpia la contraseña', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async () => {
      return respuestaJson({ status: 401, title: 'Error', detail: 'Credenciales incorrectas.' }, 401, 'application/problem+json')
    })

    renderApp('/ingresar')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'x@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'incorrecta')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByText('Credenciales incorrectas.')).toBeInTheDocument()
    expect(screen.getByLabelText('Contraseña')).toHaveValue('')
  })
})
