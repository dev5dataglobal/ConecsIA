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
const SEDES = [
  { idSede: 1, idCiudad: 1, idZonaEmpresarial: null, nombre: 'Sede Norte', direccion: null, telefono: null, correo: null, zonaHoraria: 'America/Bogota', activo: true },
  { idSede: 2, idCiudad: 1, idZonaEmpresarial: 5, nombre: 'Bodega franca', direccion: null, telefono: null, correo: null, zonaHoraria: 'America/Bogota', activo: true },
]
const PUNTOS = [
  { ...TRAZA, idPuntoAcceso: 9, idSede: 1, sede: 'Sede Norte', sedeActiva: true, zonaEmpresarial: null, esComunZonaEmpresarial: false, nombre: 'Entrada de parqueadero', descripcion: null, modoAcceso: 'Entrada', activo: true, tieneHistorial: false },
  { ...TRAZA, idPuntoAcceso: 10, idSede: 2, sede: 'Bodega franca', sedeActiva: true, zonaEmpresarial: 'Zona Franca', esComunZonaEmpresarial: true, nombre: 'Portería del parque', descripcion: null, modoAcceso: 'Ambos', activo: true, tieneHistorial: true },
]

function simular(modulos: unknown, alGuardar?: (url: string, init?: RequestInit) => Response) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
    const url = String(entrada)
    if (url.includes(MIS_MODULOS)) return respuestaJson(modulos)
    if (init?.method && init.method !== 'GET' && alGuardar) return alGuardar(url, init)
    if (url.includes('/api/sedes')) return respuestaJson(SEDES)
    if (url.includes('/api/puntos-acceso')) return respuestaJson(PUNTOS)
    return respuestaJson([])
  })
}

describe('Puntos de acceso', () => {
  beforeEach(() => olvidarSesion())

  it('lista los puntos con su sede, la marca común y solo ofrece eliminar los que no tienen historial', async () => {
    simular(modulosPermitidos('sedes'))
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/puntos-acceso')

    expect(await screen.findByRole('heading', { name: 'Entrada de parqueadero' })).toBeInTheDocument()
    expect(screen.getByText('Solo entradas')).toBeInTheDocument()
    expect(screen.getByText('Común de la zona empresarial')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Eliminar Entrada de parqueadero' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Eliminar Portería del parque' })).not.toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: /Zonas/ })).not.toBeInTheDocument()
  })

  it('sin permiso de añadir ni editar no muestra acciones', async () => {
    simular([moduloConAcciones('sedes', { consultar: true })])
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/puntos-acceso')

    expect(await screen.findByRole('heading', { name: 'Entrada de parqueadero' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Nuevo punto de acceso/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Editar Entrada/ })).not.toBeInTheDocument()
  })

  it('crea un punto en la sede elegida, ofrece la marca común solo con zona y muestra el conflicto por duplicado', async () => {
    const user = userEvent.setup()
    const enviados: unknown[] = []
    simular(modulosPermitidos('sedes'), (_url, init) => {
      enviados.push(JSON.parse(String(init?.body)))
      return respuestaJson({ title: 'Conflicto', detail: 'Ya existe un punto de acceso con ese nombre en la sede.', status: 409 }, 409)
    })
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
    renderApp('/administracion/puntos-acceso')

    await user.click(await screen.findByRole('button', { name: /Nuevo punto de acceso/ }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nuevo punto de acceso' })
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))
    expect(within(dialogo).getByText('El nombre del punto de acceso es obligatorio.')).toBeInTheDocument()
    expect(within(dialogo).getByText('Selecciona la sede del punto de acceso.')).toBeInTheDocument()

    await user.click(within(dialogo).getByRole('button', { name: /^Sede:/ }))
    await user.click(await screen.findByRole('option', { name: 'Sede Norte' }))
    expect(within(dialogo).queryByText('Punto común de la zona empresarial')).not.toBeInTheDocument()
    await user.click(within(dialogo).getByRole('button', { name: /^Sede:/ }))
    await user.click(await screen.findByRole('option', { name: 'Bodega franca' }))
    expect(within(dialogo).getByText('Punto común de la zona empresarial')).toBeInTheDocument()

    await user.type(within(dialogo).getByRole('textbox', { name: 'Nombre del punto de acceso' }), 'portería')
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))

    expect(await within(dialogo).findByText('Ya existe un punto de acceso con ese nombre en la sede.')).toBeInTheDocument()
    expect(enviados).toEqual([{ idSede: 2, nombre: 'portería', descripcion: null, modoAcceso: 'Ambos', esComunZonaEmpresarial: false }])
  })
})
