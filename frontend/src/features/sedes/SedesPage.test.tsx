import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'
import { renderApp, respuestaJson } from '../../test/render'

const ORG = '11111111-1111-1111-1111-111111111111'
const USUARIO = '22222222-2222-2222-2222-222222222222'

const sedeNorte = {
  id: 'aaaaaaaa-0000-0000-0000-000000000001',
  nombre: 'Sede Norte',
  direccion: 'Calle 100',
  ciudad: 'Bogotá',
  pais: 'Colombia',
  zonaHoraria: 'America/Bogota',
  estado: 'Activa',
  version: 7,
  creadaEn: '2026-10-17T10:00:00Z',
  actualizadaEn: '2026-10-17T10:00:00Z',
  inactivadaEn: null,
}

function me(permisos: string[]) {
  return {
    usuario: { id: USUARIO, nombre: 'Admin Sintético', correo: 'admin@ejemplo.test' },
    organizacion: { id: ORG, nombre: 'Conjunto Sintético' },
    permisos,
    alcanceTotalSedes: true,
    sedesAsignadas: [],
  }
}

type Manejador = (url: string, init?: RequestInit) => Response | Promise<Response>

/** Simula la API por método y ruta; registra cada llamada para verificar cuerpos y cabeceras. */
function simularApi(rutas: Record<string, Manejador>) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
    const url = String(entrada)
    const metodo = init?.method ?? 'GET'
    const clave = Object.keys(rutas).find((k) => {
      const [m, ruta] = k.split(' ')
      return m === metodo && url.split('?')[0] === ruta
    })
    if (clave) return rutas[clave]!(url, init)
    if (url.includes('/health')) return respuestaJson({ status: 'Healthy' })
    return respuestaJson({ status: 404, title: 'No encontrado' }, 404, 'application/problem+json')
  })
}

function listado(elementos: unknown[]) {
  return respuestaJson({ elementos, total: elementos.length, pagina: 1, tamano: 20 })
}

describe('Sedes (HU-013)', () => {
  beforeEach(() => {
    sessionStorage.clear()
    guardarSesionDesarrollo(USUARIO)
  })

  it('lista las sedes con estado y zona horaria y envía la identidad de desarrollo', async () => {
    const fetch = simularApi({
      'GET /api/v1/me': () => respuestaJson(me(['sedes.consultar'])),
      'GET /api/v1/sedes': () => listado([sedeNorte]),
    })

    renderApp('/sedes')

    const fila = await screen.findByRole('row', { name: /Sede Norte/ })
    expect(within(fila).getByText('America/Bogota')).toBeInTheDocument()
    expect(within(fila).getByText('Activa')).toBeInTheDocument()
    const llamada = fetch.mock.calls.find(([u]) => String(u).startsWith('/api/v1/sedes'))!
    expect(new Headers(llamada[1]?.headers).get('X-Usuario-Desarrollo')).toBe(USUARIO)
  })

  it('oculta acciones sin permiso: la UI no es la barrera de seguridad, solo evita ofrecerlas', async () => {
    simularApi({
      'GET /api/v1/me': () => respuestaJson(me(['sedes.consultar'])),
      'GET /api/v1/sedes': () => listado([sedeNorte]),
    })

    renderApp('/sedes')

    await screen.findByRole('row', { name: /Sede Norte/ })
    expect(screen.queryByRole('button', { name: 'Nueva sede' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Inactivar/ })).not.toBeInTheDocument()
  })

  it('crea una sede y muestra los errores de campo que devuelve el servidor', async () => {
    const user = userEvent.setup()
    const cuerpos: unknown[] = []
    let intento = 0
    simularApi({
      'GET /api/v1/me': () => respuestaJson(me(['sedes.consultar', 'sedes.crear'])),
      'GET /api/v1/sedes': () => listado([]),
      'POST /api/v1/sedes': (_u, init) => {
        cuerpos.push(JSON.parse(String(init?.body)))
        intento += 1
        return intento === 1
          ? respuestaJson(
              {
                status: 400,
                title: 'Error de validación',
                errors: { zonaHoraria: ['La zona horaria no es un identificador IANA válido.'] },
              },
              400,
              'application/problem+json',
            )
          : respuestaJson({ ...sedeNorte, nombre: 'Sede Sur' }, 201)
      },
    })

    renderApp('/sedes')
    await user.click(await screen.findByRole('button', { name: 'Nueva sede' }))
    await user.type(screen.getByLabelText('Nombre'), 'Sede Sur')
    await user.type(screen.getByLabelText('Ciudad'), 'Cali')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(
      await screen.findByText('La zona horaria no es un identificador IANA válido.'),
    ).toBeInTheDocument()
    expect(cuerpos[0]).toMatchObject({
      nombre: 'Sede Sur',
      ciudad: 'Cali',
      zonaHoraria: 'America/Bogota',
    })
    expect(cuerpos[0]).not.toHaveProperty('organizacionId')

    await user.click(screen.getByRole('button', { name: 'Guardar' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })

  it('edita enviando la versión leída y muestra el conflicto de concurrencia', async () => {
    const user = userEvent.setup()
    const cuerpos: unknown[] = []
    simularApi({
      'GET /api/v1/me': () => respuestaJson(me(['sedes.consultar', 'sedes.modificar'])),
      'GET /api/v1/sedes': () => listado([sedeNorte]),
      [`PUT /api/v1/sedes/${sedeNorte.id}`]: (_u, init) => {
        cuerpos.push(JSON.parse(String(init?.body)))
        return respuestaJson(
          {
            status: 409,
            title: 'Conflicto de concurrencia',
            detail: 'El registro cambió desde que fue leído.',
            code: 'CONFLICTO_CONCURRENCIA',
          },
          409,
          'application/problem+json',
        )
      },
    })

    renderApp('/sedes')
    await user.click(await screen.findByRole('button', { name: 'Editar Sede Norte' }))
    await user.clear(screen.getByLabelText('Nombre'))
    await user.type(screen.getByLabelText('Nombre'), 'Sede Norte 2')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('El registro cambió desde que fue leído.')).toBeInTheDocument()
    expect(cuerpos[0]).toMatchObject({ nombre: 'Sede Norte 2', version: 7 })
  })

  it('inactiva solo después de confirmar', async () => {
    const user = userEvent.setup()
    const inactivar = vi.fn(() => respuestaJson({ ...sedeNorte, estado: 'Inactiva' }))
    simularApi({
      'GET /api/v1/me': () => respuestaJson(me(['sedes.consultar', 'sedes.inactivar'])),
      'GET /api/v1/sedes': () => listado([sedeNorte]),
      [`POST /api/v1/sedes/${sedeNorte.id}/inactivar`]: inactivar,
    })

    renderApp('/sedes')
    await user.click(await screen.findByRole('button', { name: 'Inactivar Sede Norte' }))
    expect(inactivar).not.toHaveBeenCalled()
    await user.click(
      within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirmar inactivación' }),
    )

    await waitFor(() => expect(inactivar).toHaveBeenCalledOnce())
  })

  it('muestra el rechazo del servidor cuando no hay permiso', async () => {
    simularApi({
      'GET /api/v1/me': () => respuestaJson(me([])),
      'GET /api/v1/sedes': () =>
        respuestaJson(
          {
            status: 403,
            title: 'Acceso denegado',
            detail: 'No tienes el permiso requerido.',
            code: 'PERMISO_REQUERIDO',
            traceId: 'tr-403',
          },
          403,
          'application/problem+json',
        ),
    })

    renderApp('/sedes')

    expect(await screen.findByText('No tienes el permiso requerido.')).toBeInTheDocument()
    expect(screen.getByText(/tr-403/)).toBeInTheDocument()
  })
})

describe('Sesión de desarrollo (sustituto de HU-006 mientras D-03 está abierta)', () => {
  beforeEach(() => sessionStorage.clear())

  it('sin sesión redirige a elegir un usuario sintético y luego abre sedes', async () => {
    const user = userEvent.setup()
    simularApi({
      'GET /api/v1/desarrollo/usuarios': () =>
        respuestaJson([
          {
            id: USUARIO,
            nombre: 'Admin Sintético',
            correo: 'admin@ejemplo.test',
            organizaciones: ['Conjunto Sintético'],
          },
        ]),
      'GET /api/v1/me': () => respuestaJson(me(['sedes.consultar'])),
      'GET /api/v1/sedes': () => listado([sedeNorte]),
    })

    renderApp('/sedes')

    await user.click(await screen.findByRole('button', { name: /Admin Sintético/ }))
    expect(await screen.findByRole('row', { name: /Sede Norte/ })).toBeInTheDocument()
    expect(sessionStorage.getItem('conecsia.usuarioDesarrollo')).toBe(USUARIO)
  })

  it('un 401 de la API cierra la sesión de desarrollo', async () => {
    guardarSesionDesarrollo(USUARIO)
    simularApi({
      'GET /api/v1/me': () =>
        respuestaJson({ status: 401, title: 'No autenticado' }, 401, 'application/problem+json'),
      'GET /api/v1/desarrollo/usuarios': () => respuestaJson([]),
    })

    renderApp('/sedes')

    expect(await screen.findByRole('heading', { name: 'Ingresar' })).toBeInTheDocument()
    expect(sessionStorage.getItem('conecsia.usuarioDesarrollo')).toBeNull()
  })
})
