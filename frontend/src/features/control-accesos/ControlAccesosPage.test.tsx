import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { guardarAccessToken, olvidarSesion } from '../sesion/sesion'
import { renderApp, respuestaJson } from '../../test/render'
import { moduloConAcciones, modulosPermitidos } from '../../test/modulos'
import { validarSolicitudAcceso } from './ControlAccesosPage'

function tokenFalso(payload: Record<string, unknown>) {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/=+$/, '')
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`
}

const MIS_MODULOS = '/api/app-modulos/mios'
const OPCIONES = {
  sedes: [{ id: 1, nombre: 'Sede Norte' }],
  areas: [{ id: 3, idSede: 1, nombre: 'Gerencia' }],
  responsables: [{ idMembresia: 5, nombre: 'gerente@ejemplo.test', areas: [] }],
  zonas: [],
  permitePermanente: false,
  idMembresiaPropia: 5,
}
const PERSONA = {
  idPersona: 11, idOrganizacion: 1, idTipoPersona: 3, tipoPersona: 'Contratista', idTipoDocumento: 1, tipoDocumento: 'CC', numeroDocumento: '1020304050',
  nombres: 'Ana', apellidos: 'Gómez', correo: 'ana@ejemplo.test', empresaProcedencia: null, observacion: null, telefonoPrincipal: null,
  tieneFoto: false, activo: true, creadoEn: '2026-10-26T12:00:00Z', registradoPor: null, telefonos: [], vehiculos: [],
}
const PENDIENTE = {
  idAutorizacion: 40, idPersona: 11, persona: 'Ana Gómez', documentoPersona: '1020304050', personaActiva: true, idSede: 1, sede: 'Sede Norte', idArea: 3, area: 'Gerencia',
  idMembresiaResponsable: 5, responsable: 'gerente@ejemplo.test', modalidad: 'Unica', fechaInicio: '2026-10-29', fechaFin: '2026-10-29', horaInicio: null, horaFin: null,
  cupoIngresos: 1, ingresosConsumidos: 0, motivo: 'Mantenimiento', observacion: null, estado: 'Pendiente', vigencia: 'NoAplica',
  solicitadoPor: 'recepcion@ejemplo.test', solicitadoEn: '2026-10-28T13:00:00Z', decididoPor: null, decididoEn: null, motivoDecision: null,
  horarios: [], zonas: [], zonaHoraria: 'America/Bogota',
  historial: [{ idTransicion: 1, estadoAnterior: null, estadoNuevo: 'Pendiente', motivo: 'Mantenimiento', actor: 'recepcion@ejemplo.test', ocurridoEn: '2026-10-28T13:00:00Z' }],
}

describe('Control de accesos', () => {
  beforeEach(() => {
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
  })
  afterEach(() => vi.restoreAllMocks())

  it('valida solo las condiciones del tipo de autorización elegido', () => {
    const base = { idSede: 1, idArea: 3, idMembresiaResponsable: 5, fechaInicio: '2026-11-02', fechaFin: null, horaInicio: null, horaFin: null, diasSemana: [], zonas: [], motivo: 'Obra', observacion: null }
    const hoy = '2026-10-28'
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Unica' }, false, hoy)).toEqual({})
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Diaria', fechaInicio: '2026-10-27' }, false, hoy).fechaInicio).toMatch(/anterior a hoy/)
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Diaria', horaInicio: '10:00' }, false, hoy).horas).toMatch(/ambas horas/)
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Temporal', horaInicio: '08:00', horaFin: '08:00', fechaFin: '2026-11-02' }, false, hoy).fechaFin).toMatch(/posterior al inicio/)
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Temporal', horaInicio: '08:00', horaFin: '07:00', fechaFin: '2026-11-03' }, false, hoy)).toEqual({})
    const recurrente = validarSolicitudAcceso(11, { ...base, modalidad: 'Recurrente' }, false, hoy)
    expect(recurrente.dias).toBeDefined()
    expect(recurrente.horas).toBeDefined()
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Recurrente', diasSemana: [1], horaInicio: '07:00', horaFin: '17:00' }, false, hoy)).toEqual({})
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Permanente' }, false, hoy).modalidad).toMatch(/política/)
    expect(validarSolicitudAcceso(11, { ...base, modalidad: 'Permanente' }, true, hoy)).toEqual({})
    expect(validarSolicitudAcceso(0, { ...base, modalidad: 'Unica', motivo: ' ' }, false, hoy)).toMatchObject({ persona: expect.any(String), motivo: expect.any(String) })
  })

  it('«Gestionar acceso» abre la solicitud con la persona ya identificada y muestra solo los campos del tipo elegido', async () => {
    const user = userEvent.setup()
    const enviadas: unknown[] = []
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('personas', 'control_accesos'))
      if (url.includes('/api/personas/catalogos')) return respuestaJson({ tiposPersona: [], tiposDocumento: [], tiposTelefono: [], tiposVehiculo: [], relacionesVehiculo: [] })
      if (url.endsWith('/api/personas/11')) return respuestaJson(PERSONA)
      if (url.includes('/api/personas')) return respuestaJson([PERSONA])
      if (url.includes('/api/autorizaciones-acceso/opciones')) return respuestaJson(OPCIONES)
      if (url.endsWith('/api/autorizaciones-acceso') && init?.method === 'POST') { enviadas.push(JSON.parse(String(init.body))); return respuestaJson(PENDIENTE, 201) }
      if (url.includes('/api/autorizaciones-acceso/40')) return respuestaJson(PENDIENTE)
      return respuestaJson([])
    })
    renderApp('/personas')

    await user.click(await screen.findByRole('button', { name: 'Gestionar acceso de Ana Gómez' }, { timeout: 5000 }))
    const dialogo = await screen.findByRole('dialog', { name: /Solicitar acceso/ }, { timeout: 5000 })
    expect(await within(dialogo).findByText('Ana Gómez')).toBeInTheDocument()
    expect(within(dialogo).queryByLabelText('Buscar persona')).not.toBeInTheDocument()

    // Única: solo el día y la franja opcional.
    expect(within(dialogo).getByLabelText('Día')).toBeInTheDocument()
    expect(within(dialogo).queryByLabelText('Fecha de finalización')).not.toBeInTheDocument()
    // Permanente bloqueada por la política.
    expect(within(dialogo).getByRole('radio', { name: /Permanente/ })).toBeDisabled()
    // Temporal: inicio y fin con hora; Recurrente: días de la semana.
    await user.click(within(dialogo).getByRole('radio', { name: /Temporal/ }))
    expect(within(dialogo).getByLabelText('Fecha de finalización')).toBeInTheDocument()
    expect(within(dialogo).queryByLabelText('Día')).not.toBeInTheDocument()
    await user.click(within(dialogo).getByRole('radio', { name: /Recurrente/ }))
    expect(within(dialogo).getByRole('checkbox', { name: 'Lunes' })).toBeChecked()
    expect(within(dialogo).getByRole('checkbox', { name: 'Sábado' })).not.toBeChecked()

    await user.click(within(dialogo).getByRole('radio', { name: /Única/ }))
    await user.click(within(dialogo).getByRole('button', { name: /^Área o destino:/ }))
    await user.click(await screen.findByRole('option', { name: 'Gerencia' }))
    await user.type(within(dialogo).getByLabelText('Motivo'), 'Mantenimiento')
    await user.click(within(dialogo).getByRole('button', { name: 'Enviar solicitud' }))

    expect(await screen.findByText(/Queda pendiente de aprobación/)).toBeInTheDocument()
    expect(enviadas).toHaveLength(1)
    expect(enviadas[0]).toMatchObject({ idPersona: 11, modalidad: 'Unica', idArea: 3, idMembresiaResponsable: 5, fechaFin: null, diasSemana: [] })
    // Nunca se crea otra persona en este flujo.
    expect(fetch.mock.calls.some(([u, i]) => String(u).endsWith('/api/personas') && i?.method === 'POST')).toBe(false)
  }, 20_000)

  it('sin Autorizaciones · Editar la solicitud pendiente se consulta pero no ofrece aprobar ni rechazar', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson([moduloConAcciones('control_accesos', { consultar: true, anadir: true })])
      if (url.includes('/api/autorizaciones-acceso/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/api/autorizaciones-acceso/40')) return respuestaJson(PENDIENTE)
      return respuestaJson([PENDIENTE])
    })
    renderApp('/control-accesos')

    const fila = (await screen.findByText('Ana Gómez')).closest('tr')!
    expect(within(fila).getByText('Pendiente de aprobación')).toBeInTheDocument()
    await user.click(within(fila).getByRole('button', { name: 'Ver solicitud de Ana Gómez' }))
    expect(await screen.findByText(/La decide un administrador de la organización/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Aprobar' })).not.toBeInTheDocument()
  })

  it('el administrador rechaza con motivo obligatorio', async () => {
    const user = userEvent.setup()
    const decisiones: { url: string; body: unknown }[] = []
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('control_accesos', 'autorizaciones'))
      if (url.includes('/api/autorizaciones-acceso/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/rechazar')) {
        decisiones.push({ url, body: JSON.parse(String(init?.body)) })
        return respuestaJson({ ...PENDIENTE, estado: 'Rechazada', decididoPor: 'admin@ejemplo.test', decididoEn: '2026-10-28T14:00:00Z', motivoDecision: 'Sin planilla' })
      }
      if (url.includes('/api/autorizaciones-acceso/40')) return respuestaJson(PENDIENTE)
      return respuestaJson([PENDIENTE])
    })
    renderApp('/control-accesos')

    await user.click(await screen.findByRole('button', { name: 'Ver solicitud de Ana Gómez' }))
    await user.click(await screen.findByRole('button', { name: 'Rechazar' }))
    await user.click(screen.getByRole('button', { name: 'Confirmar: rechazar' }))
    expect(await screen.findByText('Indica el motivo del rechazo.')).toBeInTheDocument()
    expect(decisiones).toHaveLength(0)
    await user.type(screen.getByLabelText('Motivo del rechazo'), 'Sin planilla')
    await user.click(screen.getByRole('button', { name: 'Confirmar: rechazar' }))
    expect(await screen.findByText(/Se conserva en el historial/)).toBeInTheDocument()
    expect(decisiones[0]).toMatchObject({ url: expect.stringContaining('/api/autorizaciones-acceso/40/rechazar'), body: { motivo: 'Sin planilla' } })
  })
})
