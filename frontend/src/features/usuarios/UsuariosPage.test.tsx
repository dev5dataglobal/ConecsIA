import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

const contexto = {
  usuario: { id: 'u1', nombre: 'Ana Admin', correo: 'ana@ejemplo.test' },
  organizacion: { id: 'o1', nombre: 'Los Pinos' },
  permisos: [
    'usuarios.consultar',
    'usuarios.invitar',
    'usuarios.administrar',
    'sedes.consultar',
    'roles.consultar',
  ],
  alcanceTotalSedes: true,
  sedesAsignadas: [],
}
const operador = {
  usuarioId: 'u2',
  nombre: 'Óscar Operador',
  correo: 'oscar@ejemplo.test',
  estado: 'Activo',
  roles: [],
  alcanceTotalSedes: false,
  sedes: ['s1'],
}
const rol = {
  id: 'r1',
  nombre: 'Portería',
  descripcion: null,
  permisos: ['sedes.consultar'],
  plantilla: null,
  estado: 'Activo',
  version: 1,
  creadoEn: '',
  actualizadoEn: '',
  inactivadoEn: null,
}
const sede = { id: 's1', nombre: 'Portería Norte', estado: 'Activa' }

function api(
  alEnviar: (url: string, metodo: string, cuerpo: unknown) => Response | undefined = () =>
    undefined,
) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
    const url = String(entrada)
    const metodo = init?.method ?? 'GET'
    if (metodo !== 'GET') {
      const respuesta = alEnviar(url, metodo, init?.body ? JSON.parse(String(init.body)) : null)
      return Promise.resolve(respuesta ?? respuestaJson(operador))
    }
    if (url.endsWith('/api/v1/me')) return Promise.resolve(respuestaJson(contexto))
    if (url.endsWith('/api/v1/usuarios')) return Promise.resolve(respuestaJson([operador]))
    if (url.endsWith('/api/v1/roles')) return Promise.resolve(respuestaJson([rol]))
    if (url.endsWith('/zonas')) return Promise.resolve(respuestaJson([]))
    if (url.includes('/api/v1/sedes'))
      return Promise.resolve(respuestaJson({ elementos: [sede], total: 1, pagina: 1, tamano: 20 }))
    return Promise.resolve(respuestaJson([]))
  })
}

describe('UsuariosPage (HU-016, HU-018)', () => {
  afterEach(() => sessionStorage.clear())

  it('lista los usuarios con su estado y alcance', async () => {
    guardarSesionDesarrollo('u1')
    api()

    renderApp('/usuarios')

    const tabla = await screen.findByRole('table', { name: 'Usuarios de la organización' })
    expect(within(tabla).getByText('Óscar Operador')).toBeInTheDocument()
    expect(within(tabla).getByText('1 sedes')).toBeInTheDocument()
  })

  it('invita con roles y alcance y muestra el enlace de desarrollo', async () => {
    guardarSesionDesarrollo('u1')
    let enviado: unknown
    api((url, _metodo, cuerpo) => {
      if (url.endsWith('/invitaciones')) {
        enviado = cuerpo
        return respuestaJson({ usuario: operador, enlaceAceptacion: '/invitaciones/abc' }, 201)
      }
      return undefined
    })
    const user = userEvent.setup()

    renderApp('/usuarios')
    await user.click(await screen.findByRole('button', { name: 'Invitar usuario' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Invitar usuario' })
    await user.type(within(dialogo).getByLabelText('Correo'), 'nuevo@ejemplo.test')
    await user.type(within(dialogo).getByLabelText('Nombre'), 'Nuevo')
    await user.click(await within(dialogo).findByLabelText('Portería'))
    await user.click(await within(dialogo).findByLabelText('Portería Norte'))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    expect(await within(dialogo).findByText(/\/invitaciones\/abc/)).toBeInTheDocument()
    expect(enviado).toEqual({
      correo: 'nuevo@ejemplo.test',
      nombre: 'Nuevo',
      roles: ['r1'],
      alcanceTotalSedes: false,
      sedes: ['s1'],
    })
  })

  it('muestra el rechazo del servidor al asignar alcance fuera del propio', async () => {
    guardarSesionDesarrollo('u1')
    api((url) =>
      url.endsWith('/alcance')
        ? respuestaJson(
            {
              status: 403,
              title: 'Acceso denegado',
              detail: 'La sede está fuera de tu alcance asignado.',
              code: 'FUERA_DE_ALCANCE',
            },
            403,
            'application/problem+json',
          )
        : undefined,
    )
    const user = userEvent.setup()

    renderApp('/usuarios')
    await user.click(
      await screen.findByRole('button', { name: 'Roles y alcance de Óscar Operador' }),
    )
    const dialogo = await screen.findByRole('dialog', { name: 'Roles y alcance de Óscar Operador' })
    await user.click(within(dialogo).getByLabelText('Toda la organización'))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    await waitFor(() =>
      expect(
        within(dialogo).getByText('La sede está fuera de tu alcance asignado.'),
      ).toBeInTheDocument(),
    )
  })

  it('acepta una invitación definiendo la contraseña', async () => {
    let aceptada = ''
    vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
      const url = String(entrada)
      if (url.endsWith('/auth/csrf')) return Promise.resolve(respuestaJson({ token: 'csrf' }))
      if (init?.method === 'POST') {
        aceptada = url
        return Promise.resolve(new Response(null, { status: 204 }))
      }
      return Promise.resolve(respuestaJson({ status: 'Healthy' }))
    })
    const user = userEvent.setup()

    renderApp('/invitaciones/tok-123')
    await user.type(await screen.findByLabelText('Define tu contraseña'), 'una-contraseña-larga')
    await user.click(screen.getByRole('button', { name: 'Aceptar invitación' }))

    expect(await screen.findByText(/Ya puedes ingresar/)).toBeInTheDocument()
    expect(aceptada).toContain('/api/v1/invitaciones/aceptar')
    expect(aceptada).not.toContain('tok-123')
  })
})
