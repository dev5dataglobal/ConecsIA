import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { reiniciarCsrf } from '../../shared/api/http'
import { renderApp, respuestaJson } from '../../test/render'
import { marcarSesionIniciada } from './sesion'

const ME = {
  usuario: { id: 'u-1', nombre: 'Ana Admin', correo: 'ana@ejemplo.test' },
  organizacion: { id: 'o-1', nombre: 'Conjunto Sintético' },
  permisos: ['sedes.consultar'],
  alcanceTotalSedes: true,
  sedesAsignadas: [],
}

type Manejador = (init?: RequestInit) => Response | Promise<Response>

function problema(status: number, detail: string, code?: string) {
  return respuestaJson({ status, title: 'Error', detail, code }, status, 'application/problem+json')
}

/** Simula la API por método y ruta; las rutas sin manejador responden 404 (p. ej. modo desarrollo apagado). */
function simularApi(rutas: Record<string, Manejador>) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
    const url = String(entrada).split('?')[0] ?? ''
    const clave = `${init?.method ?? 'GET'} ${url}`
    const manejador = rutas[clave]
    if (manejador) return manejador(init)
    if (url.includes('/health')) return respuestaJson({ status: 'Healthy' })
    return problema(404, 'No encontrado')
  })
}

function cabecera(init: RequestInit | undefined, nombre: string) {
  return new Headers(init?.headers).get(nombre)
}

function llamadas(fetch: ReturnType<typeof simularApi>, clave: string) {
  return fetch.mock.calls.filter(
    ([entrada, init]) => `${init?.method ?? 'GET'} ${String(entrada).split('?')[0]}` === clave,
  )
}

describe('Inicio de sesión real (HU-006, ADR-006)', () => {
  beforeEach(() => {
    sessionStorage.clear()
    reiniciarCsrf()
  })

  it('ingresa con correo y contraseña enviando el token anti-CSRF y abre la ruta pedida', async () => {
    const user = userEvent.setup()
    const fetch = simularApi({
      'GET /api/v1/auth/csrf': () => respuestaJson({ token: 'tok-1', cabecera: 'X-CSRF-TOKEN' }),
      'POST /api/v1/auth/login': () => new Response(null, { status: 204 }),
      'GET /api/v1/me': () => respuestaJson(ME),
      'GET /api/v1/sedes': () => respuestaJson({ elementos: [], total: 0, pagina: 1, tamano: 20 }),
    })

    renderApp('/sedes')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'ana@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'una-frase-larga')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByRole('heading', { name: 'Sedes' })).toBeInTheDocument()
    const [[, login]] = llamadas(fetch, 'POST /api/v1/auth/login') as [[unknown, RequestInit]]
    expect(cabecera(login, 'X-CSRF-TOKEN')).toBe('tok-1')
    expect(cabecera(login, 'X-Usuario-Desarrollo')).toBeNull()
    expect(JSON.parse(String(login.body))).toEqual({
      correo: 'ana@ejemplo.test',
      contrasena: 'una-frase-larga',
    })
    expect(sessionStorage.getItem('conecsia.sesionIniciada')).toBe('1')
  })

  it('muestra el error genérico del servidor, limpia la contraseña y no ofrece usuarios de desarrollo', async () => {
    const user = userEvent.setup()
    simularApi({
      'GET /api/v1/auth/csrf': () => respuestaJson({ token: 'tok-1' }),
      'POST /api/v1/auth/login': () => problema(401, 'Correo o contraseña incorrectos.'),
    })

    renderApp('/ingresar')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'x@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'incorrecta')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByText('Correo o contraseña incorrectos.')).toBeInTheDocument()
    expect(screen.getByLabelText('Contraseña')).toHaveValue('')
    expect(screen.queryByRole('heading', { name: 'Usuarios de desarrollo' })).toBeNull()
  })

  it('muestra el bloqueo temporal (423)', async () => {
    const user = userEvent.setup()
    simularApi({
      'GET /api/v1/auth/csrf': () => respuestaJson({ token: 'tok-1' }),
      'POST /api/v1/auth/login': () =>
        problema(
          423,
          'Demasiados intentos fallidos. Inténtalo de nuevo más tarde.',
          'CUENTA_BLOQUEADA',
        ),
    })

    renderApp('/ingresar')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'x@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'cualquiera')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByText(/Demasiados intentos fallidos/)).toBeInTheDocument()
  })

  it('un 401 en cualquier petición lleva de nuevo al inicio de sesión', async () => {
    marcarSesionIniciada()
    simularApi({
      'GET /api/v1/me': () => respuestaJson(ME),
      'GET /api/v1/sedes': () => problema(401, 'La sesión no corresponde a un usuario activo.'),
    })

    renderApp('/sedes')

    expect(await screen.findByRole('heading', { name: 'Ingresar' })).toBeInTheDocument()
    expect(sessionStorage.getItem('conecsia.sesionIniciada')).toBeNull()
  })

  it('cerrar sesión la revoca en el servidor con token anti-CSRF', async () => {
    marcarSesionIniciada()
    const user = userEvent.setup()
    const fetch = simularApi({
      'GET /api/v1/me': () => respuestaJson(ME),
      'GET /api/v1/sedes': () => respuestaJson({ elementos: [], total: 0, pagina: 1, tamano: 20 }),
      'GET /api/v1/auth/csrf': () => respuestaJson({ token: 'tok-sesion' }),
      'POST /api/v1/auth/logout': () => new Response(null, { status: 204 }),
    })

    renderApp('/sedes')
    await user.click(await screen.findByRole('button', { name: 'Cerrar sesión' }))

    expect(await screen.findByRole('heading', { name: 'Ingresar' })).toBeInTheDocument()
    const [[, logout]] = llamadas(fetch, 'POST /api/v1/auth/logout') as [[unknown, RequestInit]]
    expect(cabecera(logout, 'X-CSRF-TOKEN')).toBe('tok-sesion')
    expect(sessionStorage.getItem('conecsia.sesionIniciada')).toBeNull()
  })

  it('ante un token anti-CSRF rechazado pide uno nuevo y reintenta una sola vez', async () => {
    const user = userEvent.setup()
    let tokens = 0
    let intentos = 0
    const fetch = simularApi({
      'GET /api/v1/auth/csrf': () => respuestaJson({ token: `tok-${++tokens}` }),
      'POST /api/v1/auth/login': () =>
        ++intentos === 1
          ? problema(403, 'Falta el token anti-CSRF o no es válido.', 'CSRF_INVALIDO')
          : new Response(null, { status: 204 }),
      'GET /api/v1/me': () => respuestaJson(ME),
      'GET /api/v1/sedes': () => respuestaJson({ elementos: [], total: 0, pagina: 1, tamano: 20 }),
    })

    renderApp('/ingresar')
    await user.type(await screen.findByLabelText('Correo electrónico'), 'ana@ejemplo.test')
    await user.type(screen.getByLabelText('Contraseña'), 'una-frase-larga')
    await user.click(screen.getByRole('button', { name: 'Ingresar' }))

    await waitFor(() => expect(sessionStorage.getItem('conecsia.sesionIniciada')).toBe('1'))
    const intentosLogin = llamadas(fetch, 'POST /api/v1/auth/login') as [unknown, RequestInit][]
    expect(intentosLogin.map(([, init]) => cabecera(init, 'X-CSRF-TOKEN'))).toEqual([
      'tok-1',
      'tok-2',
    ])
  })
})
