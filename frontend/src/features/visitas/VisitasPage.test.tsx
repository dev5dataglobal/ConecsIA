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
const OPCIONES = {
  sedes: [{ id: 1, nombre: 'Sede Norte' }],
  areas: [{ id: 3, idSede: 1, nombre: 'Gerencia' }],
  responsables: [{ idMembresia: 5, nombre: 'gerente@ejemplo.test', areas: [3] }],
  puntosAcceso: [{ id: 8, idSede: 1, nombre: 'Portería principal', modoAcceso: 'Ambos' }],
  permiteSinAutorizacion: false,
}
const VISITA = {
  idVisita: 21, idPersona: 11, persona: 'Ana Gómez', documentoPersona: '1020304050', personaActiva: true, excepcionTipoAcceso: false, motivoExcepcion: null, idSede: 1, sede: 'Sede Norte', idArea: 3, area: 'Gerencia',
  idMembresiaResponsable: 5, responsable: 'gerente@ejemplo.test', motivo: 'Reunión', fechaEsperada: null, horaEsperadaInicio: null, horaEsperadaFin: null,
  idAutorizacion: 4, politicaAplicada: null, estado: 'Autorizada', creadoEn: '2026-10-26T13:00:00Z', entradaEn: null, salidaEn: null, zonaHoraria: 'America/Bogota',
}
const PAGINA = { registros: [VISITA], total: 1, pagina: 1, tamano: 20, indicadores: { pendientes: 2, autorizadas: 1, dentro: 4, finalizadas: 9 } }
const DETALLE = {
  visita: VISITA, registradaPor: 'recepcion@ejemplo.test', observacion: null, motivoCierre: null, actualizadaPor: null, actualizadaEn: null,
  autorizacion: { idAutorizacion: 4, estado: 'Aprobada', modalidad: 'Unica', fechaInicio: '2026-10-26', fechaFin: '2026-10-26', cupoIngresos: null, ingresosConsumidos: 0, horarios: [] },
  permanencia: null, movimientos: [], historial: [], evaluacion: { puedeIngresar: true, puedeSalir: false, motivos: [] },
}

describe('Visitas', () => {
  beforeEach(() => {
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
  })

  it('muestra indicadores y envía búsqueda, filtros y paginación al servidor', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('visitas', 'control_accesos'))
      if (url.includes('/api/visitas/opciones')) return respuestaJson(OPCIONES)
      return respuestaJson(PAGINA)
    })
    renderApp('/visitas')

    const fila = (await screen.findByText('Ana Gómez')).closest('tr')!
    expect(within(fila).getByText('Autorizada')).toBeInTheDocument()
    const resumen = screen.getByRole('group', { name: 'Resumen de visitas' })
    expect(within(resumen).getByText('4')).toBeInTheDocument()

    await user.click(within(resumen).getByRole('button', { name: /Pendientes/ }))
    await vi.waitFor(() => expect(fetch.mock.calls.some(([u]) => String(u).includes('/api/visitas?') && String(u).includes('estado=Pendiente') && String(u).includes('pagina=1'))).toBe(true))
    await user.type(screen.getByRole('searchbox', { name: 'Buscar visitas' }), '1020')
    await vi.waitFor(() => expect(fetch.mock.calls.some(([u]) => String(u).includes('buscar=1020'))).toBe(true))
  })

  it('registra la entrada con clave de idempotencia y muestra los motivos concretos si el servidor la deniega', async () => {
    const user = userEvent.setup()
    const claves: string[] = []
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('visitas', 'control_accesos'))
      if (url.includes('/api/visitas/opciones')) return respuestaJson(OPCIONES)
      if (url.endsWith('/api/visitas/21/entrada') && init?.method === 'POST') {
        claves.push(new Headers(init.headers).get('Idempotency-Key') ?? '')
        return respuestaJson({ title: 'Ingreso denegado', status: 409, detail: 'Fuera del horario autorizado (08:00–17:00).', motivos: ['Fuera del horario autorizado (08:00–17:00).'] }, 409)
      }
      if (url.endsWith('/api/visitas/21')) return respuestaJson(DETALLE)
      return respuestaJson(PAGINA)
    })
    renderApp('/visitas')

    await user.click(await screen.findByRole('button', { name: 'Ver visita de Ana Gómez' }))
    expect(await screen.findByText(/La autorización está vigente/)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Registrar entrada/ }))
    // Un único punto disponible queda preseleccionado.
    await user.click(screen.getByRole('button', { name: 'Confirmar entrada' }))

    const alerta = await screen.findByRole('alert')
    expect(within(alerta).getByText('Ingreso denegado')).toBeInTheDocument()
    expect(within(alerta).getByText('Fuera del horario autorizado (08:00–17:00).')).toBeInTheDocument()
    expect(claves).toHaveLength(1)
    expect(claves[0]).not.toBe('')

    // Reintentar el mismo intento reutiliza la clave: el servidor no duplica el movimiento.
    await user.click(screen.getByRole('button', { name: 'Confirmar entrada' }))
    await vi.waitFor(() => expect(claves).toHaveLength(2))
    expect(claves[1]).toBe(claves[0])
  })

  it('sin permiso de control de accesos no ofrece registrar entrada ni la pestaña de personas dentro', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('visitas'))
      if (url.includes('/api/visitas/opciones')) return respuestaJson(OPCIONES)
      if (url.endsWith('/api/visitas/21')) return respuestaJson(DETALLE)
      return respuestaJson(PAGINA)
    })
    renderApp('/visitas')

    await user.click(await screen.findByRole('button', { name: 'Ver visita de Ana Gómez' }))
    await screen.findByText(/La autorización está vigente/)
    expect(screen.queryByRole('button', { name: /Registrar entrada/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('tab', { name: /Personas dentro/ })).not.toBeInTheDocument()
  })

  it('nueva visita se presenta como ingreso extemporáneo y muestra el tipo de permiso de la persona', async () => {
    const user = userEvent.setup()
    const recurrente = {
      idPersona: 12, idOrganizacion: 1, idTipoPersona: 3, tipoPersona: 'Contratista', idTipoDocumento: 1, tipoDocumento: 'CC', numeroDocumento: '555666',
      nombres: 'Luis', apellidos: 'Pardo', correo: null, empresaProcedencia: null, observacion: null, telefonoPrincipal: null, tieneFoto: false, activo: true,
      creadoEn: '2026-10-26T12:00:00Z', tipoPermiso: 'Recurrente',
      accesosRecurrentes: [{ idAutorizacion: 40, modalidad: 'Recurrente', idSede: 1, sede: 'Sede Norte', fechaInicio: '2026-10-01', fechaFin: null }],
    }
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('visitas', 'personas'))
      if (url.includes('/api/visitas/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/api/personas/catalogos')) return respuestaJson({ tiposPersona: [], tiposDocumento: [{ id: 1, codigo: 'CC', nombre: 'Cédula', patronValidacion: null }], tiposTelefono: [], tiposVehiculo: [], relacionesVehiculo: [] })
      if (url.includes('/api/personas?')) return respuestaJson([recurrente])
      return respuestaJson(PAGINA)
    })
    renderApp('/visitas')

    await user.click(await screen.findByRole('button', { name: /Nueva visita/ }))
    expect(await screen.findByText('Ingreso extemporáneo')).toBeInTheDocument()
    await user.type(screen.getByLabelText('O busca por nombre'), 'Luis')
    const opcion = await screen.findByRole('option', { name: /Luis Pardo/ })
    expect(opcion).toHaveTextContent('Recurrente')
    await user.click(opcion)
    expect(screen.getByText(/Tipo de permiso:/).parentElement).toHaveTextContent('Recurrente')
    expect(screen.getByText(/solo podrá registrarse vinculando su autorización recurrente/)).toBeInTheDocument()
  })
})
