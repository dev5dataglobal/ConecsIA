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
      JSON.stringify({ correo: 'ana@ejemplo.test', password: 'una-frase-larga', recordarme: false }),
    )
    expect(new Headers(login?.headers).get('Authorization')).toBeNull()
    expect(leerAccessToken()).toBe('jwt-1')
    expect(sessionStorage.getItem('conecsia.sesionIniciada')).toBe('1')
  })

  it('envía «Recordarme» y renueva el token vencido con la cookie de sesión', async () => {
    const user = userEvent.setup()
    let token = 'jwt-1'
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.endsWith('/api/auth/login')) return respuestaJson({ accessToken: token, expiresAtUtc: '2030-01-01T00:00:00Z', usuario: { idUsuario: 2, correo: 'b@ejemplo.test', activo: true } })
      if (url.endsWith('/api/auth/renovar')) {
        token = 'jwt-2'
        return respuestaJson({ accessToken: token, expiresAtUtc: '2030-01-01T00:00:00Z', usuario: { idUsuario: 2, correo: 'b@ejemplo.test', activo: true } })
      }
      // El primer token "vence": el servidor responde 401 hasta que llegue el renovado.
      const autorizacion = new Headers(init?.headers).get('Authorization')
      if (autorizacion !== 'Bearer jwt-2') return respuestaJson({ status: 401, title: 'No autorizado' }, 401, 'application/problem+json')
      return respuestaJson([])
    })

    renderApp('/ingresar')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'b@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'una-frase-larga')
    await user.click(screen.getByRole('checkbox', { name: 'Recordarme' }))
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    const login = fetch.mock.calls.find(([url]) => String(url).endsWith('/api/auth/login'))?.[1]
    expect(JSON.parse(String(login?.body))).toMatchObject({ recordarme: true })
    await vi.waitFor(() => expect(leerAccessToken()).toBe('jwt-2'))
    const renovar = fetch.mock.calls.find(([url]) => String(url).endsWith('/api/auth/renovar'))?.[1]
    expect(new Headers(renovar?.headers).get('X-Conecsia-Sesion')).toBe('1')
    expect(localStorage.length + sessionStorage.length).toBeLessThanOrEqual(2)
    expect(JSON.stringify({ ...localStorage, ...sessionStorage })).not.toContain('jwt-')
  })

  it('el enlace de recuperación solicita el correo sin revelar si la cuenta existe', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async () =>
      respuestaJson({ mensaje: 'Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.' }, 202))

    renderApp('/ingresar')
    await user.click(await screen.findByRole('link', { name: '¿Olvidaste tu contraseña?' }))
    await user.type(await screen.findByLabelText('Correo electrónico'), 'x@ejemplo.test')
    await user.click(screen.getByRole('button', { name: /Enviar enlace/ }))

    expect(await screen.findByRole('status')).toHaveTextContent('Si el correo está registrado')
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
