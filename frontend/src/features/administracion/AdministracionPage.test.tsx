import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { guardarAccessToken, olvidarSesion } from '../sesion/sesion'
import { renderApp, respuestaJson } from '../../test/render'
import { modulosPermitidos } from '../../test/modulos'

function tokenFalso(payload: Record<string, unknown>) {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/=+$/, '')
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`
}

const MIS_MODULOS = '/api/app-modulos/mios'

describe('Administración', () => {
  beforeEach(() => olvidarSesion())

  it('muestra solo las secciones y acciones permitidas', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) =>
      respuestaJson(String(entrada).includes(MIS_MODULOS) ? modulosPermitidos('administracion') : []))
    guardarAccessToken(tokenFalso({ sub: '9', permission: ['usuarios.consultar'] }))
    renderApp('/administracion')

    expect(await screen.findByRole('tab', { name: /Usuarios/ })).toHaveAttribute('aria-selected', 'true')
    expect(screen.queryByRole('tab', { name: /Organizaciones/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: /Perfiles/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Nuevo usuario/ })).not.toBeInTheDocument()
    expect(await screen.findByText('Aún no hay usuarios registrados.')).toBeInTheDocument()
  })

  it('distingue la falta de permiso (403) de otros errores', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) =>
      String(entrada).includes(MIS_MODULOS) ? respuestaJson(modulosPermitidos('administracion')) : new Response(null, { status: 403 }))
    guardarAccessToken(tokenFalso({ sub: '9', permission: ['usuarios.consultar', 'usuarios.crear'] }))
    renderApp('/administracion')

    expect(await screen.findByText('Tu perfil no tiene permiso para consultar esta información.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Nuevo usuario/ })).toBeInTheDocument()
  })

  it('sin el módulo de administración la ruta muestra acceso denegado', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async () => respuestaJson(modulosPermitidos('dashboard')))
    guardarAccessToken(tokenFalso({ sub: '9', permission: ['usuarios.consultar'] }))
    renderApp('/administracion')

    expect(await screen.findByRole('heading', { name: 'Acceso denegado' })).toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: /Usuarios/ })).not.toBeInTheDocument()
  })

  it('reemplaza los módulos de un perfil con lo seleccionado', async () => {
    const user = userEvent.setup()
    const catalogo = modulosPermitidos('dashboard', 'visitas', 'personas', 'administracion')
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('dashboard', 'administracion'))
      if (url.endsWith('/api/app-modulos')) return respuestaJson(catalogo)
      if (url.endsWith('/api/roles/5/modulos') && init?.method === 'PUT') return respuestaJson([])
      if (url.endsWith('/api/roles/5/modulos')) return respuestaJson([catalogo[0]])
      if (url.includes('/organizaciones/1/perfiles'))
        return respuestaJson([{ idRol: 5, idOrganizacion: 1, nombre: 'Operador', descripcion: null, activo: true, permisos: [] }])
      return respuestaJson([])
    })
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1', permission: ['roles.consultar', 'roles.administrar'] }))
    renderApp('/administracion')

    await user.click(await screen.findByRole('button', { name: /Módulos/ }))
    const dialogo = await screen.findByRole('dialog', { name: 'Operador' })
    expect(await within(dialogo).findByRole('checkbox', { name: /Dashboard/ })).toBeChecked()
    await user.click(within(dialogo).getByRole('checkbox', { name: /Visitas/ }))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar módulos' }))

    const put = fetch.mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(String(put?.[0])).toContain('/api/roles/5/modulos')
    expect(JSON.parse(String(put?.[1]?.body))).toEqual({ idsModulos: [1, 2] })
  })
})
