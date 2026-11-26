import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { guardarAccessToken, olvidarSesion } from '../sesion/sesion'
import { renderApp, respuestaJson } from '../../test/render'
import { moduloConAcciones, modulosPermitidos } from '../../test/modulos'

function tokenFalso(payload: Record<string, unknown>) {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/=+$/, '')
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`
}

const MIS_MODULOS = '/api/app-modulos/mios'
const SEDES = [{ idSede: 1, idCiudad: 1, idZonaEmpresarial: null, nombre: 'Sede Norte', direccion: null, telefono: null, correo: null, zonaHoraria: 'America/Bogota', activo: true }]
const AREAS = [{ idArea: 3, idSede: 1, idAreaPadre: null, nombre: 'Gerencia', descripcion: null, esComunZonaEmpresarial: false, activo: true }]
const ANA = {
  idArea: 3, idMembresia: 21, usuario: 'ana@ejemplo.test', usuarioActivo: true, esAutorizador: false, activo: true,
  asignadoEn: '2026-11-10T14:00:00Z', asignadoPor: 'admin@ejemplo.test', actualizadoEn: null, actualizadoPor: null,
}
const LUIS = { ...ANA, idMembresia: 22, usuario: 'luis@ejemplo.test', activo: false, actualizadoEn: '2026-11-10T15:00:00Z', actualizadoPor: 'admin@ejemplo.test' }

function simular(modulos: unknown, alGuardar?: (url: string, init: RequestInit) => Response) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
    const url = String(entrada)
    if (url.includes(MIS_MODULOS)) return respuestaJson(modulos)
    if (init?.method && init.method !== 'GET' && alGuardar) return alGuardar(url, init)
    if (url.includes('/api/areas/3/responsables')) return respuestaJson([ANA, LUIS])
    if (url.includes('/api/areas/miembros')) return respuestaJson([{ idMembresia: 21, usuario: 'ana@ejemplo.test' }, { idMembresia: 23, usuario: 'eva@ejemplo.test' }])
    if (url.includes('/api/areas')) return respuestaJson(AREAS)
    if (url.includes('/api/sedes')) return respuestaJson(SEDES)
    return respuestaJson([])
  })
}

async function abrirResponsables(user: ReturnType<typeof userEvent.setup>) {
  await user.click(await screen.findByRole('tab', { name: /Áreas/ }))
  await user.click(await screen.findByRole('button', { name: 'Responsables de Gerencia' }))
  return screen.findByRole('dialog', { name: 'Responsables de Gerencia' })
}

describe('Responsables y autorizadores por destino', () => {
  beforeEach(() => {
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
  })

  it('designa a un usuario como autorizador y retira a otro sin borrarlo', async () => {
    const user = userEvent.setup()
    const enviadas: { url: string; metodo: string; cuerpo: unknown }[] = []
    simular(modulosPermitidos('sedes'), (url, init) => {
      enviadas.push({ url, metodo: init.method!, cuerpo: init.body ? JSON.parse(String(init.body)) : null })
      if (url.endsWith('/retirar')) return respuestaJson({ ...ANA, activo: false })
      return respuestaJson({ ...ANA, idMembresia: 23, usuario: 'eva@ejemplo.test', esAutorizador: true })
    })
    renderApp('/administracion/sedes')
    const dialogo = await abrirResponsables(user)

    // Activos y retirados por separado; el ya designado no se ofrece de nuevo.
    const activos = await within(dialogo).findByRole('list', { name: 'Responsables activos' })
    expect(within(activos).getByText('ana@ejemplo.test')).toBeInTheDocument()
    expect(within(dialogo).getByText('Retirados (1)')).toBeInTheDocument()
    await user.click(within(dialogo).getByRole('button', { name: /^Usuario:/ }))
    expect(screen.queryByRole('option', { name: 'ana@ejemplo.test' })).not.toBeInTheDocument()
    await user.click(await screen.findByRole('option', { name: 'eva@ejemplo.test' }))
    await user.click(within(dialogo).getByRole('switch', { name: /También es autorizador/ }))
    await user.click(within(dialogo).getByRole('button', { name: 'Designar' }))
    expect(await screen.findByText('eva@ejemplo.test designado en Gerencia.')).toBeInTheDocument()
    expect(enviadas[0]).toEqual({ url: expect.stringContaining('/api/areas/3/responsables'), metodo: 'POST', cuerpo: { idMembresia: 23, esAutorizador: true } })

    await user.click(within(activos).getByRole('button', { name: 'Retirar' }))
    expect(await screen.findByText(/retirado de Gerencia\. Su historial se conserva\./)).toBeInTheDocument()
    expect(enviadas[1]).toMatchObject({ url: expect.stringContaining('/api/areas/3/responsables/21/retirar'), metodo: 'POST' })
  }, 20_000)

  it('exige elegir un usuario y, solo con consultar, no ofrece cambios', async () => {
    const user = userEvent.setup()
    const guardar = vi.fn(() => respuestaJson(ANA))
    simular(modulosPermitidos('sedes'), guardar)
    const { unmount } = renderApp('/administracion/sedes')
    let dialogo = await abrirResponsables(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Designar' }))
    expect(within(dialogo).getByText('Selecciona el usuario que vas a designar.')).toBeInTheDocument()
    expect(guardar).not.toHaveBeenCalled()
    unmount()
    vi.restoreAllMocks()

    simular([moduloConAcciones('sedes', { consultar: true })])
    renderApp('/administracion/sedes')
    dialogo = await abrirResponsables(user)
    expect(await within(dialogo).findByText('ana@ejemplo.test')).toBeInTheDocument()
    expect(within(dialogo).queryByRole('button', { name: 'Designar' })).not.toBeInTheDocument()
    expect(within(dialogo).queryByRole('button', { name: 'Retirar' })).not.toBeInTheDocument()
  }, 20_000)
})
