import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { reiniciarCsrf } from '../../shared/api/http'
import { renderApp, respuestaJson } from '../../test/render'

describe('Inicio de sesión real', () => {
  beforeEach(() => {
    sessionStorage.clear()
    reiniciarCsrf()
  })

  it('envía las credenciales al contrato de autenticación y conserva la sesión', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.endsWith('/api/v1/auth/csrf')) return respuestaJson({ token: 'tok-1' })
      if (url.endsWith('/api/v1/auth/login')) return new Response(null, { status: 204 })
      return respuestaJson({ status: 404, title: 'No encontrado' }, 404, 'application/problem+json')
    })

    renderApp('/ingresar')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'ana@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'una-frase-larga')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    const login = fetch.mock.calls.find(([, init]) => init?.method === 'POST')?.[1]
    expect(JSON.parse(String(login?.body))).toEqual({
      correo: 'ana@ejemplo.test',
      contrasena: 'una-frase-larga',
    })
    expect(new Headers(login?.headers).get('X-CSRF-TOKEN')).toBe('tok-1')
    expect(sessionStorage.getItem('conecsia.sesionIniciada')).toBe('1')
  })

  it('presenta el error genérico y limpia la contraseña', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      if (String(entrada).endsWith('/api/v1/auth/csrf')) return respuestaJson({ token: 'tok-1' })
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
