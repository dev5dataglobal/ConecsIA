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
const TRAZA = { creadoEn: '2026-10-28T15:00:00Z', creadoPor: 'admin@ejemplo.test', actualizadoEn: null, actualizadoPor: null }
const SEDES = [{ idSede: 1, idCiudad: 1, idZonaEmpresarial: null, nombre: 'Sede Norte', direccion: null, telefono: null, correo: null, zonaHoraria: 'America/Bogota', activo: true }]
const ZONAS = [
  { ...TRAZA, idZona: 3, idSede: 1, sede: 'Sede Norte', sedeActiva: true, nombre: 'Parqueadero', descripcion: null, activo: true, puntosAcceso: 1, tieneHistorial: true },
  { ...TRAZA, idZona: 4, idSede: 1, sede: 'Sede Norte', sedeActiva: true, nombre: 'Bodega', descripcion: null, activo: true, puntosAcceso: 0, tieneHistorial: false },
]
const PUNTOS = [
  { ...TRAZA, idPuntoAcceso: 9, idSede: 1, sede: 'Sede Norte', idZona: 3, zona: 'Parqueadero', zonaActiva: true, nombre: 'Entrada de parqueadero', descripcion: null, modoAcceso: 'Entrada', activo: true, tieneHistorial: false },
]

function simular(modulos: unknown, alGuardar?: (url: string, init?: RequestInit) => Response) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
    const url = String(entrada)
    if (url.includes(MIS_MODULOS)) return respuestaJson(modulos)
    if (init?.method && init.method !== 'GET' && alGuardar) return alGuardar(url, init)
    if (url.includes('/api/sedes')) return respuestaJson(SEDES)
    if (url.includes('/api/zonas')) return respuestaJson(ZONAS)
    if (url.includes('/api/puntos-acceso')) return respuestaJson(PUNTOS)
    return respuestaJson([])
  })
}

describe('Zonas y puntos de acceso', () => {
  beforeEach(() => olvidarSesion())

  it('lista zonas con su sede y solo ofrece eliminar las que no tienen historial', async () => {
    simular(modulosPermitidos('sedes'))
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/zonas-acceso')

    expect(await screen.findByRole('heading', { name: 'Parqueadero' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Eliminar Bodega' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Eliminar Parqueadero' })).not.toBeInTheDocument()
    expect(screen.getAllByText(/por admin@ejemplo.test/).length).toBeGreaterThan(0)

    await userEvent.setup().click(screen.getByRole('tab', { name: /Puntos de acceso/ }))
    expect(await screen.findByRole('heading', { name: 'Entrada de parqueadero' })).toBeInTheDocument()
    expect(screen.getByText('Solo entradas')).toBeInTheDocument()
  })

  it('sin permiso de añadir ni editar no muestra acciones', async () => {
    simular([moduloConAcciones('sedes', { consultar: true })])
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/zonas-acceso')

    expect(await screen.findByRole('heading', { name: 'Parqueadero' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Nueva zona/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Editar Parqueadero/ })).not.toBeInTheDocument()
  })

  it('crea una zona en la sede elegida y muestra el conflicto por duplicado', async () => {
    const user = userEvent.setup()
    const enviados: unknown[] = []
    simular(modulosPermitidos('sedes'), (_url, init) => {
      enviados.push(JSON.parse(String(init?.body)))
      return respuestaJson({ title: 'Conflicto', detail: 'Ya existe una zona con ese nombre en la sede.', status: 409 }, 409)
    })
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/zonas-acceso')

    await user.click(await screen.findByRole('button', { name: /Nueva zona/ }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nueva zona' })
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))
    expect(within(dialogo).getByText('El nombre de la zona es obligatorio.')).toBeInTheDocument()
    expect(within(dialogo).getByText('Selecciona la sede de la zona.')).toBeInTheDocument()

    await user.click(within(dialogo).getByRole('button', { name: /^Sede:/ }))
    await user.click(await screen.findByRole('option', { name: 'Sede Norte' }))
    await user.type(within(dialogo).getByRole('textbox', { name: 'Nombre de la zona' }), 'parqueadero')
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    expect(await within(dialogo).findByText('Ya existe una zona con ese nombre en la sede.')).toBeInTheDocument()
    expect(enviados).toEqual([{ idSede: 1, nombre: 'parqueadero', descripcion: null }])
  })
})
