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
const OPCIONES = { sedes: [{ id: 1, nombre: 'Sede Norte' }], areas: [{ id: 3, idSede: 1, nombre: 'Recepción' }] }
const PENDIENTE = {
  idAutorizacionObjeto: 7, tipoObjeto: 'Dispositivo', idVehiculo: null, idDispositivo: 4, objeto: 'Portátil · SN-123', objetoActivo: true,
  idPersona: 11, persona: 'Ana Gómez', documentoPersona: '1020304050', idSede: 1, sede: 'Sede Norte', idArea: 3, area: 'Recepción', idAutorizacion: null,
  fechaInicio: '2026-10-26', fechaFin: '2026-10-30', horaInicio: '07:00:00', horaFin: '18:00:00', motivo: 'Mantenimiento', estado: 'Pendiente', vigencia: 'NoAplica',
  motivoDecision: null, solicitadoPor: 'operador@ejemplo.test', solicitadoEn: '2026-10-26T12:00:00Z', decididoPor: null, decididoEn: null,
  historial: [{ idTransicion: 1, estadoAnterior: null, estadoNuevo: 'Pendiente', motivo: 'Mantenimiento', actor: 'operador@ejemplo.test', ocurridoEn: '2026-10-26T12:00:00Z' }],
}

describe('Autorizaciones de vehículos y dispositivos', () => {
  beforeEach(() => {
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
  })

  it('lista autorizaciones con estado y envía los filtros al servidor', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('autorizaciones', 'vehiculos', 'dispositivos'))
      if (url.includes('/opciones')) return respuestaJson(OPCIONES)
      return respuestaJson([PENDIENTE])
    })
    renderApp('/autorizaciones')

    expect(await screen.findByText('Portátil · SN-123')).toBeInTheDocument()
    expect(screen.getByText('Ana Gómez')).toBeInTheDocument()
    const fila = screen.getByText('Portátil · SN-123').closest('tr')!
    expect(within(fila).getByText('Pendiente')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /^Filtrar por estado:/ }))
    await user.click(await screen.findByRole('option', { name: 'Aprobadas' }))
    await vi.waitFor(() => expect(fetch.mock.calls.some(([u]) => String(u).includes('/api/autorizaciones-objeto?') && String(u).includes('estado=Aprobada'))).toBe(true))
  })

  it('exige motivo para rechazar y envía la decisión sin identificar al aprobador desde el cliente', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('autorizaciones', 'vehiculos', 'dispositivos'))
      if (url.includes('/opciones')) return respuestaJson(OPCIONES)
      if (url.endsWith('/api/autorizaciones-objeto/7/rechazar') && init?.method === 'POST')
        return respuestaJson({ ...PENDIENTE, estado: 'Rechazada', motivoDecision: 'No requerido', decididoPor: 'admin@ejemplo.test', decididoEn: '2026-10-26T13:00:00Z',
          historial: [...PENDIENTE.historial, { idTransicion: 2, estadoAnterior: 'Pendiente', estadoNuevo: 'Rechazada', motivo: 'No requerido', actor: 'admin@ejemplo.test', ocurridoEn: '2026-10-26T13:00:00Z' }] })
      if (url.endsWith('/api/autorizaciones-objeto/7')) return respuestaJson(PENDIENTE)
      return respuestaJson([PENDIENTE])
    })
    renderApp('/autorizaciones')

    await user.click(await screen.findByRole('button', { name: 'Ver autorización de Portátil · SN-123' }))
    const dialogo = await screen.findByRole('dialog')
    expect(await within(dialogo).findByText(/Solicitud creada/)).toBeInTheDocument()
    await user.click(within(dialogo).getByRole('button', { name: 'Rechazar' }))
    const grupo = within(dialogo).getByRole('group', { name: 'Rechazar autorización' })
    await user.click(within(grupo).getByRole('button', { name: 'Confirmar: rechazar' }))
    expect(within(grupo).getByText('Indica el motivo.')).toBeInTheDocument()

    await user.type(within(grupo).getByLabelText('Motivo'), 'No requerido')
    await user.click(within(grupo).getByRole('button', { name: 'Confirmar: rechazar' }))
    expect(await within(dialogo).findByText('Pendiente → Rechazada')).toBeInTheDocument()
    const envio = fetch.mock.calls.find(([u, i]) => String(u).endsWith('/7/rechazar') && i?.method === 'POST')
    expect(JSON.parse(String(envio?.[1]?.body))).toEqual({ motivo: 'No requerido' })
  })

  it('sin permiso de edición no ofrece aprobar ni rechazar, y sin añadir no ofrece nueva solicitud', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson([moduloConAcciones('autorizaciones', { consultar: true }), moduloConAcciones('dispositivos', { consultar: true }, 2)])
      if (url.includes('/opciones')) return respuestaJson(OPCIONES)
      if (url.endsWith('/api/autorizaciones-objeto/7')) return respuestaJson(PENDIENTE)
      return respuestaJson([PENDIENTE])
    })
    renderApp('/autorizaciones')

    expect(screen.queryByRole('button', { name: /Nueva solicitud/ })).not.toBeInTheDocument()
    await user.click(await screen.findByRole('button', { name: 'Ver autorización de Portátil · SN-123' }))
    const dialogo = await screen.findByRole('dialog')
    await within(dialogo).findByText(/Solicitud creada/)
    expect(within(dialogo).queryByRole('button', { name: 'Aprobar' })).not.toBeInTheDocument()
    expect(within(dialogo).queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument()
  })

  it('al registrar una solicitud refresca el listado y abre su detalle', async () => {
    const user = userEvent.setup()
    let creada = false
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('autorizaciones', 'vehiculos', 'dispositivos', 'personas'))
      if (url.includes('/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/api/personas?')) return respuestaJson([{ idPersona: 11, nombres: 'Ana', apellidos: 'Gómez', tipoDocumento: 'CC', numeroDocumento: '1020304050' }])
      if (url.endsWith('/api/personas/11/dispositivos')) return respuestaJson([{ idDispositivo: 4, idPersona: 11, tipoDispositivo: 'Portátil', codigoIdentificador: 'SN-123', activo: true }])
      if (url.endsWith('/api/personas/11')) return respuestaJson({ idPersona: 11, nombres: 'Ana', apellidos: 'Gómez', tipoDocumento: 'CC', numeroDocumento: '1020304050', vehiculos: [], telefonos: [] })
      if (url.endsWith('/api/autorizaciones-objeto') && init?.method === 'POST') { creada = true; return respuestaJson(PENDIENTE, 201) }
      if (url.endsWith('/api/autorizaciones-objeto/7')) return respuestaJson(PENDIENTE)
      return respuestaJson(creada ? [PENDIENTE] : [])
    })
    renderApp('/autorizaciones')

    await user.click(await screen.findByRole('button', { name: /Nueva solicitud/ }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nueva solicitud de autorización' })
    await user.type(within(dialogo).getByLabelText('Buscar persona'), 'Ana')
    await user.click(await within(dialogo).findByRole('option', { name: /Ana Gómez/ }))
    await user.click(within(dialogo).getByRole('button', { name: /^Tipo de objeto:/ }))
    await user.click(await screen.findByRole('option', { name: 'Dispositivo' }))
    await user.click(await within(dialogo).findByRole('button', { name: /^Dispositivo: Selecciona/ }))
    await user.click(await screen.findByRole('option', { name: /SN-123/ }))
    await user.click(within(dialogo).getByRole('button', { name: 'Solicitar' }))

    expect(await screen.findByRole('cell', { name: /Portátil · SN-123/ })).toBeInTheDocument()
    expect(fetch.mock.calls.some(([u, i]) => String(u).endsWith('/api/autorizaciones-objeto') && i?.method === 'POST')).toBe(true)
  })

  it('valida la solicitud antes de enviarla', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('autorizaciones', 'vehiculos', 'dispositivos'))
      if (url.includes('/opciones')) return respuestaJson(OPCIONES)
      return respuestaJson([])
    })
    renderApp('/autorizaciones')

    await user.click(await screen.findByRole('button', { name: /Nueva solicitud/ }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nueva solicitud de autorización' })
    await user.click(within(dialogo).getByRole('button', { name: 'Solicitar' }))
    expect(within(dialogo).getByText('Selecciona la persona.')).toBeInTheDocument()
    expect(within(dialogo).getByText('Selecciona el vehículo.')).toBeInTheDocument()
    expect(fetch.mock.calls.some(([, i]) => i?.method === 'POST')).toBe(false)
  })
})
