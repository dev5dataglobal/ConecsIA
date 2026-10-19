import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'
import { guardarSesionDesarrollo } from '../sesion/sesionDesarrollo'

const catalogo = [
  {
    clave: 'sedes.consultar',
    recurso: 'sedes',
    accion: 'consultar',
    descripcion: 'Consultar sedes',
  },
  { clave: 'sedes.crear', recurso: 'sedes', accion: 'crear', descripcion: 'Crear sedes' },
  {
    clave: 'roles.consultar',
    recurso: 'roles',
    accion: 'consultar',
    descripcion: 'Consultar roles',
  },
  {
    clave: 'roles.administrar',
    recurso: 'roles',
    accion: 'administrar',
    descripcion: 'Administrar roles',
  },
]
const plantillas = [
  {
    clave: 'auditor',
    nombre: 'Auditor',
    descripcion: 'Consulta',
    permisos: ['sedes.consultar', 'roles.consultar'],
  },
]
const rolPorteria = {
  id: 'r1',
  nombre: 'Portería',
  descripcion: null,
  permisos: ['sedes.consultar'],
  plantilla: null,
  estado: 'Activo',
  version: 7,
  creadoEn: '2026-10-19T00:00:00Z',
  actualizadoEn: '2026-10-19T00:00:00Z',
  inactivadoEn: null,
}

function contexto(permisos: string[]) {
  return {
    usuario: { id: 'u1', nombre: 'Ana Admin', correo: 'ana@ejemplo.test' },
    organizacion: { id: 'o1', nombre: 'Los Pinos' },
    permisos,
    alcanceTotalSedes: true,
    sedesAsignadas: [],
  }
}

function api(permisos: string[], alCrear?: (cuerpo: unknown) => Response) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((entrada, init) => {
    const url = String(entrada)
    if (url.endsWith('/api/v1/me')) return Promise.resolve(respuestaJson(contexto(permisos)))
    if (url.endsWith('/api/v1/me/membresias')) return Promise.resolve(respuestaJson([]))
    if (url.endsWith('/api/v1/permisos')) return Promise.resolve(respuestaJson(catalogo))
    if (url.endsWith('/api/v1/roles/plantillas')) return Promise.resolve(respuestaJson(plantillas))
    if (url.endsWith('/api/v1/roles') && init?.method === 'POST')
      return Promise.resolve(
        alCrear?.(JSON.parse(String(init.body))) ?? respuestaJson(rolPorteria, 201),
      )
    if (url.endsWith('/api/v1/roles')) return Promise.resolve(respuestaJson([rolPorteria]))
    return Promise.resolve(respuestaJson({ status: 'Healthy' }))
  })
}

describe('RolesPage (HU-017)', () => {
  afterEach(() => sessionStorage.clear())

  it('lista los roles de la organización', async () => {
    guardarSesionDesarrollo('u1')
    api(['roles.consultar'])

    renderApp('/roles')

    const tabla = await screen.findByRole('table', { name: 'Roles de la organización' })
    expect(within(tabla).getByText('Portería')).toBeInTheDocument()
    expect(within(tabla).getByText('1 permisos')).toBeInTheDocument()
  })

  it('sin roles.administrar no ofrece crear, editar ni inactivar (solo UX)', async () => {
    guardarSesionDesarrollo('u1')
    api(['roles.consultar'])

    renderApp('/roles')

    await screen.findByText('Portería')
    expect(screen.queryByRole('button', { name: 'Nuevo rol' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Editar Portería' })).not.toBeInTheDocument()
  })

  it('crea un rol desde una plantilla con sus permisos precargados', async () => {
    guardarSesionDesarrollo('u1')
    let enviado: unknown
    api(['roles.consultar', 'roles.administrar'], (cuerpo) => {
      enviado = cuerpo
      return respuestaJson(rolPorteria, 201)
    })
    const user = userEvent.setup()

    renderApp('/roles')
    await user.click(await screen.findByRole('button', { name: 'Nuevo rol' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nuevo rol' })
    const plantilla = await within(dialogo).findByLabelText('Partir de una plantilla')
    await within(dialogo).findByLabelText('Consultar roles')
    await user.selectOptions(plantilla, 'auditor')
    expect(within(dialogo).getByLabelText('Consultar roles')).toBeChecked()
    expect(within(dialogo).getByLabelText('Crear sedes')).not.toBeChecked()
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    await waitFor(() =>
      expect(enviado).toEqual({
        nombre: 'Auditor',
        descripcion: '',
        permisos: ['sedes.consultar', 'roles.consultar'],
        plantilla: 'auditor',
      }),
    )
  })

  it('muestra el error del servidor al intentar escalar permisos', async () => {
    guardarSesionDesarrollo('u1')
    api(['roles.consultar', 'roles.administrar'], () =>
      respuestaJson(
        {
          status: 403,
          title: 'Acceso denegado',
          detail: 'No puedes conceder permisos que no tienes.',
          code: 'ESCALAMIENTO_DE_PERMISOS',
        },
        403,
        'application/problem+json',
      ),
    )
    const user = userEvent.setup()

    renderApp('/roles')
    await user.click(await screen.findByRole('button', { name: 'Nuevo rol' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nuevo rol' })
    await user.type(within(dialogo).getByLabelText('Nombre'), 'Escalado')
    await user.click(await within(dialogo).findByLabelText('Crear sedes'))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    expect(
      await within(dialogo).findByText('No puedes conceder permisos que no tienes.'),
    ).toBeInTheDocument()
  })
})
