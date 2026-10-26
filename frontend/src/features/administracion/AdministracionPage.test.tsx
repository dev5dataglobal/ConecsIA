import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiFetch } from '../../shared/api/http'
import { guardarAccessToken, olvidarSesion, seleccionarOrganizacion } from '../sesion/sesion'
import { renderApp, respuestaJson } from '../../test/render'
import { moduloConAcciones, modulosPermitidos } from '../../test/modulos'

function tokenFalso(payload: Record<string, unknown>) {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/=+$/, '')
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`
}

const MIS_MODULOS = '/api/app-modulos/mios'
const USUARIO = {
  idUsuario: 20, idMembresia: 7, idOrganizacion: 1, organizacion: 'Org', correo: 'ana@ejemplo.test', idPersona: null,
  nombre: null, documento: null, idRoles: [5], perfiles: ['Operador'], activo: true, estado: 'Activo',
}

describe('Administración', () => {
  beforeEach(() => olvidarSesion())

  it('muestra solo las secciones permitidas y oculta acciones sin el permiso', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) =>
      respuestaJson(String(entrada).includes(MIS_MODULOS) ? [moduloConAcciones('usuarios', { consultar: true })] : []))
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion')

    const secciones = await screen.findByRole('navigation', { name: 'Secciones de administración' })
    expect(await within(secciones).findByRole('link', { name: /Usuarios/ })).toHaveAttribute('aria-current', 'page')
    expect(within(secciones).queryByRole('link', { name: /Organizaciones/ })).not.toBeInTheDocument()
    expect(within(secciones).queryByRole('link', { name: /Perfiles/ })).not.toBeInTheDocument()
    expect(await screen.findByText('Aún no hay usuarios registrados.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Nuevo usuario/ })).not.toBeInTheDocument()
  })

  it('distingue la falta de permiso (403) de otros errores', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) =>
      String(entrada).includes(MIS_MODULOS) ? respuestaJson(modulosPermitidos('usuarios')) : new Response(null, { status: 403 }))
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/usuarios')

    expect(await screen.findByText('Tu perfil no tiene permiso para consultar esta información.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Nuevo usuario/ })).toBeInTheDocument()
  })

  it('sin módulos de administración la ruta muestra acceso denegado', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async () => respuestaJson(modulosPermitidos('dashboard')))
    guardarAccessToken(tokenFalso({ sub: '9' }))
    renderApp('/administracion/usuarios')

    expect(await screen.findByRole('heading', { name: 'Acceso denegado' })).toBeInTheDocument()
  })

  it('crea un usuario con el perfil elegido y valida los campos', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('usuarios'))
      if (url.includes('perfiles-asignables')) return respuestaJson([{ id: 5, nombre: 'Operador' }])
      if (init?.method === 'POST') return respuestaJson(USUARIO, 201)
      return respuestaJson([])
    })
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/usuarios')

    await user.click(await screen.findByRole('button', { name: /Nuevo usuario/ }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nuevo usuario' })
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))
    expect(within(dialogo).getByText('Ingresa un correo válido.')).toBeInTheDocument()

    await user.type(within(dialogo).getByLabelText('Correo electrónico'), 'ana@ejemplo.test')
    await user.type(within(dialogo).getByLabelText('Contraseña inicial'), 'Segura-123')
    await user.click(await within(dialogo).findByRole('checkbox', { name: 'Operador' }))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    const post = fetch.mock.calls.find(([, init]) => init?.method === 'POST')
    expect(String(post?.[0])).toContain('/api/administracion/usuarios')
    expect(JSON.parse(String(post?.[1]?.body))).toEqual({ correo: 'ana@ejemplo.test', contrasena: 'Segura-123', idRoles: [5], activo: true })
    expect(await screen.findByRole('status')).toHaveTextContent('Usuario creado.')
  })

  it('pide confirmación antes de inactivar un usuario', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('usuarios'))
      if (init?.method === 'PATCH') return respuestaJson({ ...USUARIO, activo: false, estado: 'Inactivo' })
      return respuestaJson([USUARIO])
    })
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/usuarios')

    await user.click(await screen.findByRole('button', { name: 'Inactivar ana@ejemplo.test' }))
    const confirmacion = await screen.findByRole('alertdialog', { name: '¿Inactivar usuario?' })
    expect(fetch.mock.calls.some(([, init]) => init?.method === 'PATCH')).toBe(false)
    await user.click(within(confirmacion).getByRole('button', { name: 'Inactivar' }))

    const patch = fetch.mock.calls.find(([, init]) => init?.method === 'PATCH')
    expect(String(patch?.[0])).toContain('/api/administracion/usuarios/7/estado')
    expect(JSON.parse(String(patch?.[1]?.body))).toEqual({ activo: false })
    expect(await screen.findByRole('status')).toHaveTextContent('Usuario inactivado')
  })

  it('guarda la matriz de acciones por módulo de un perfil sin acciones implícitas', async () => {
    const user = userEvent.setup()
    const catalogo = modulosPermitidos('dashboard', 'visitas', 'usuarios', 'perfiles')
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(catalogo)
      if (url.endsWith('/api/app-modulos')) return respuestaJson(catalogo)
      if (url.endsWith('/api/roles/5/modulos') && init?.method === 'PUT') return respuestaJson([])
      if (url.endsWith('/api/roles/5/modulos')) return respuestaJson([moduloConAcciones('dashboard', { consultar: true })])
      if (url.includes('/api/administracion/perfiles'))
        return respuestaJson([{ idRol: 5, idOrganizacion: 1, nombre: 'Operador', descripcion: null, activo: true, totalModulos: 1, totalUsuarios: 2, permisos: [] }])
      return respuestaJson([])
    })
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/perfiles')

    await user.click(await screen.findByRole('button', { name: 'Módulos de Operador' }))
    const dialogo = await screen.findByRole('dialog', { name: 'Operador' })
    expect(await within(dialogo).findByRole('checkbox', { name: 'Consultar en Dashboard' })).toBeChecked()
    expect(within(dialogo).getByRole('checkbox', { name: 'Añadir en Dashboard' })).not.toBeChecked()
    await user.click(within(dialogo).getByRole('checkbox', { name: 'Añadir en Visitas' }))
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar permisos' }))

    const put = fetch.mock.calls.find(([, init]) => init?.method === 'PUT')
    expect(String(put?.[0])).toContain('/api/roles/5/modulos')
    expect(JSON.parse(String(put?.[1]?.body))).toEqual({
      asignaciones: [
        { idAppModulo: 1, consultar: true, anadir: false, editar: false },
        { idAppModulo: 2, consultar: false, anadir: true, editar: false },
      ],
    })
  })

  it('solo el administrador de plataforma envía la organización seleccionada', async () => {
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async () => respuestaJson([]))

    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    seleccionarOrganizacion(2)
    await apiFetch('/api/administracion/usuarios')
    expect(new Headers(fetch.mock.calls.at(-1)?.[1]?.headers).get('X-Organizacion-Id')).toBeNull()

    guardarAccessToken(tokenFalso({ sub: '1', organization_id: '1', platform_admin: 'true' }))
    seleccionarOrganizacion(2)
    await apiFetch('/api/administracion/usuarios')
    expect(new Headers(fetch.mock.calls.at(-1)?.[1]?.headers).get('X-Organizacion-Id')).toBe('2')

    // Otra sesión (otro usuario) no hereda la selección.
    guardarAccessToken(tokenFalso({ sub: '3', organization_id: '1', platform_admin: 'true' }))
    await apiFetch('/api/administracion/usuarios')
    expect(new Headers(fetch.mock.calls.at(-1)?.[1]?.headers).get('X-Organizacion-Id')).toBeNull()

    // Cerrar sesión la descarta.
    guardarAccessToken(tokenFalso({ sub: '1', organization_id: '1', platform_admin: 'true' }))
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '1', organization_id: '1', platform_admin: 'true' }))
    await apiFetch('/api/administracion/usuarios')
    expect(new Headers(fetch.mock.calls.at(-1)?.[1]?.headers).get('X-Organizacion-Id')).toBeNull()
  })
})
