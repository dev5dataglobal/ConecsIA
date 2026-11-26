import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { guardarAccessToken, olvidarSesion } from '../sesion/sesion'
import { renderApp, respuestaJson } from '../../test/render'
import { moduloConAcciones, modulosPermitidos } from '../../test/modulos'
import { cuerpoAcceso, validarRegistroAcceso, type DatosAcceso } from './reglasAcceso'
import type { CategoriaAcceso } from './apiControlAccesos'

function tokenFalso(payload: Record<string, unknown>) {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/=+$/, '')
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`
}

const MIS_MODULOS = '/api/app-modulos/mios'
const VISITANTE: CategoriaAcceso = {
  idCategoriaAcceso: 7, codigo: 'VISITANTE', nombre: 'Visitante', descripcion: null, flujoAprobacion: 'Anfitrion', modalidadesPermitidas: ['Unica', 'Diaria', 'Temporal'],
  exigeAnfitrion: true, exigeMotivo: true, permiteVehiculos: true, permiteDispositivos: true, orden: 1, activo: true,
}
const CONTRATISTA: CategoriaAcceso = { ...VISITANTE, idCategoriaAcceso: 8, codigo: 'RECURRENTE', nombre: 'Contratista', flujoAprobacion: 'Seguridad', modalidadesPermitidas: ['Recurrente', 'Permanente'] }
const OPCIONES = {
  categorias: [VISITANTE, CONTRATISTA],
  sedes: [{ id: 1, nombre: 'Sede Norte' }],
  areas: [{ id: 3, idSede: 1, nombre: 'Gerencia' }],
  anfitriones: [{ id: 5, nombre: 'gerente@ejemplo.test' }],
  puntosAcceso: [{ id: 9, idSede: 1, nombre: 'Portería', modoAcceso: 'Ambos' }],
  permitePermanente: false,
  fueraDeHorario: 'Bloquear',
}
const PERSONA = {
  idPersona: 11, idOrganizacion: 1, idTipoPersona: 3, tipoPersona: 'Visitante', idTipoDocumento: 1, tipoDocumento: 'CC', numeroDocumento: '1020304050',
  nombres: 'Ana', apellidos: 'Gómez', correo: 'ana@ejemplo.test', empresaProcedencia: null, observacion: null, telefonoPrincipal: null,
  tieneFoto: false, activo: true, creadoEn: '2026-10-26T12:00:00Z', registradoPor: null, telefonos: [],
  vehiculos: [{ idVehiculo: 21, placa: 'ABC123', tipo: 'Automóvil', marca: 'Mazda', modelo: null, color: 'Rojo', relacion: 'Propietario', activo: true }],
}
const REGISTRO = {
  idAutorizacion: 40, idCategoriaAcceso: 7, categoria: 'Visitante', flujoAprobacion: 'Anfitrion', idPersona: 11, persona: 'Ana Gómez', documentoPersona: '1020304050', personaActiva: true,
  idSede: 1, sede: 'Sede Norte', idArea: 3, area: 'Gerencia', idMembresiaResponsable: 5, anfitrion: 'gerente@ejemplo.test', motivo: 'Reunión', observacion: null,
  modalidad: 'Unica', fechaInicio: '2026-11-05', fechaFin: '2026-11-05', horaInicio: null, horaFin: null, usaHorarioBase: false, cupoIngresos: 1, ingresosConsumidos: 0,
  estado: 'Aprobada', vigencia: 'Vigente', origen: 'Manual', creadoPor: 'porteria@ejemplo.test', creadoEn: '2026-11-04T13:00:00Z', decididoPor: 'gerente@ejemplo.test',
  decididoEn: '2026-11-04T13:05:00Z', motivoDecision: null, dentro: false, zonaHoraria: 'America/Bogota', horario: [],
  objetos: [{ idAutorizacionObjeto: 60, tipoObjeto: 'Vehiculo', idObjeto: 21, objeto: 'ABC123 · Mazda · Rojo', estado: 'Pendiente', motivoDecision: null, decididoPor: null, decididoEn: null }],
  historial: [{ estadoAnterior: null, estadoNuevo: 'Pendiente', actor: 'porteria@ejemplo.test', observacion: 'Reunión', ocurridoEn: '2026-11-04T13:00:00Z' }],
  movimientos: [], motivosIngreso: [],
}

describe('Control de accesos unificado', () => {
  beforeEach(() => {
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
  })
  afterEach(() => vi.restoreAllMocks())

  it('valida solo lo que aplica al tipo de permanencia y a la categoría', () => {
    const base: DatosAcceso = {
      idCategoriaAcceso: 7, idSede: 1, idArea: 3, idMembresiaResponsable: 5, modalidad: 'Unica', fechaInicio: '2026-11-05', fechaFin: null, horaInicio: null, horaFin: null,
      franjas: [], usaHorarioBase: false, motivo: 'Reunión', observacion: null, objetos: [],
    }
    const hoy = '2026-11-04'
    expect(validarRegistroAcceso(11, base, VISITANTE, false, hoy)).toEqual({})
    expect(validarRegistroAcceso(11, { ...base, modalidad: 'Recurrente' }, VISITANTE, false, hoy).modalidad).toMatch(/no admite/)
    expect(validarRegistroAcceso(11, { ...base, modalidad: 'Recurrente', idCategoriaAcceso: 8 }, CONTRATISTA, false, hoy).franjas).toMatch(/franja/)
    const solapadas = [{ diaSemana: 1, horaInicio: '07:00', horaFin: '12:00' }, { diaSemana: 1, horaInicio: '11:00', horaFin: '14:00' }]
    expect(validarRegistroAcceso(11, { ...base, modalidad: 'Recurrente', franjas: solapadas }, CONTRATISTA, false, hoy).franjas).toMatch(/solapan/)
    expect(validarRegistroAcceso(11, { ...base, modalidad: 'Recurrente', usaHorarioBase: true }, CONTRATISTA, false, hoy)).toEqual({})
    expect(validarRegistroAcceso(11, { ...base, modalidad: 'Permanente' }, CONTRATISTA, false, hoy).modalidad).toMatch(/política/)
    expect(validarRegistroAcceso(0, { ...base, motivo: ' ' }, undefined, false, hoy)).toMatchObject({ persona: expect.any(String), categoria: expect.any(String), motivo: expect.any(String) })
    // Solo viajan los campos del tipo elegido y las horas con segundos.
    expect(cuerpoAcceso(11, { ...base, modalidad: 'Recurrente', franjas: [{ diaSemana: 2, horaInicio: '08:00', horaFin: '12:00' }], horaInicio: '09:00' }))
      .toMatchObject({ horaInicio: null, franjas: [{ diaSemana: 2, horaInicio: '08:00:00', horaFin: '12:00:00' }] })
  })

  it('crea el acceso desde un único formulario con la persona, la categoría y su vehículo', async () => {
    const user = userEvent.setup()
    const enviadas: unknown[] = []
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('personas', 'control_accesos', 'vehiculos'))
      if (url.endsWith('/api/personas/11')) return respuestaJson(PERSONA)
      if (url.includes('/api/accesos/opciones')) return respuestaJson(OPCIONES)
      if (url.endsWith('/api/accesos') && init?.method === 'POST') { enviadas.push(JSON.parse(String(init.body))); return respuestaJson({ ...REGISTRO, estado: 'Pendiente' }, 201) }
      if (url.includes('/api/accesos/40')) return respuestaJson(REGISTRO)
      if (url.includes('/api/accesos?')) return respuestaJson({ registros: [], total: 0, pagina: 1, tamano: 20 })
      return respuestaJson([])
    })
    renderApp('/control-accesos?persona=11')

    const dialogo = await screen.findByRole('dialog', { name: /Nuevo registro de acceso/ }, { timeout: 5000 })
    expect(await within(dialogo).findByText('Ana Gómez')).toBeInTheDocument()
    await user.click(within(dialogo).getByRole('radio', { name: /Visitante/ }))
    // La categoría limita los tipos de permanencia.
    expect(within(dialogo).queryByRole('radio', { name: /Recurrente/ })).not.toBeInTheDocument()
    expect(within(dialogo).getByLabelText('Día')).toBeInTheDocument()
    await user.click(within(dialogo).getByRole('button', { name: /^Destino:/ }))
    await user.click(await screen.findByRole('option', { name: 'Gerencia' }))
    await user.click(within(dialogo).getByRole('button', { name: /^Anfitrión o responsable:/ }))
    await user.click(await screen.findByRole('option', { name: 'gerente@ejemplo.test' }))
    await user.type(within(dialogo).getByLabelText('Motivo'), 'Reunión')
    await user.click(within(dialogo).getByRole('checkbox', { name: /ABC123/ }))
    await user.click(within(dialogo).getByRole('button', { name: 'Registrar acceso' }))

    expect(await screen.findByText(/Queda pendiente de aprobación/)).toBeInTheDocument()
    expect(enviadas[0]).toMatchObject({ idPersona: 11, idCategoriaAcceso: 7, modalidad: 'Unica', idArea: 3, idMembresiaResponsable: 5, franjas: [], objetos: [{ tipoObjeto: 'Vehiculo', idObjeto: 21 }] })
    // El formulario reutiliza la persona: nunca la vuelve a crear.
    expect(fetch.mock.calls.some(([u, i]) => String(u).endsWith('/api/personas') && i?.method === 'POST')).toBe(false)
  }, 20_000)

  it('rechaza el vehículo con motivo obligatorio sin cambiar la decisión sobre la persona', async () => {
    const user = userEvent.setup()
    const decisiones: { url: string; body: unknown }[] = []
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('control_accesos', 'autorizaciones'))
      if (url.includes('/api/accesos/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/objetos/60/rechazar')) {
        decisiones.push({ url, body: JSON.parse(String(init?.body)) })
        return respuestaJson({ ...REGISTRO, objetos: [{ ...REGISTRO.objetos[0], estado: 'Rechazada', motivoDecision: 'Sin SOAT' }] })
      }
      if (url.includes('/api/accesos/40')) return respuestaJson(REGISTRO)
      return respuestaJson({ registros: [REGISTRO], total: 1, pagina: 1, tamano: 20 })
    })
    renderApp('/control-accesos')

    const fila = (await screen.findByText('Ana Gómez')).closest('tr')!
    await user.click(within(fila).getByRole('button', { name: 'Ver acceso de Ana Gómez' }))
    const objeto = (await screen.findByText('ABC123 · Mazda · Rojo')).closest('li')!
    await user.click(within(objeto).getByRole('button', { name: 'Rechazar' }))
    await user.click(within(objeto).getByRole('button', { name: 'Confirmar' }))
    expect(within(objeto).getByText('Indica el motivo.')).toBeInTheDocument()
    expect(decisiones).toHaveLength(0)
    await user.type(within(objeto).getByLabelText('Motivo'), 'Sin SOAT')
    await user.click(within(objeto).getByRole('button', { name: 'Confirmar' }))
    expect(await screen.findByText('Elemento rechazado.')).toBeInTheDocument()
    expect(decisiones[0]!.body).toEqual({ motivo: 'Sin SOAT' })
    expect(screen.getAllByText('Aprobado').length).toBeGreaterThan(0)
  }, 20_000)

  it('una entrada denegada muestra los motivos del servidor', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson([moduloConAcciones('control_accesos', { consultar: true, anadir: true })])
      if (url.includes('/api/accesos/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/api/accesos/40/entrada'))
        return respuestaJson({ status: 409, title: 'Ingreso denegado', motivos: ['Fuera del horario autorizado (07:00–12:00).'] }, 409, 'application/problem+json')
      if (url.includes('/api/accesos/40')) return respuestaJson({ ...REGISTRO, objetos: [] })
      return respuestaJson({ registros: [REGISTRO], total: 1, pagina: 1, tamano: 20 })
    })
    renderApp('/control-accesos')

    const fila = (await screen.findByText('Ana Gómez')).closest('tr')!
    await user.click(within(fila).getByRole('button', { name: 'Ver acceso de Ana Gómez' }))
    // Sin Autorizaciones · Editar no se ofrece revocar.
    expect(screen.queryByRole('button', { name: 'Revocar' })).not.toBeInTheDocument()
    await user.click(await screen.findByRole('button', { name: 'Registrar entrada' }))
    expect(await screen.findByText('Fuera del horario autorizado (07:00–12:00).')).toBeInTheDocument()
  }, 20_000)

  it('registra la entrada del vehículo aprobado de una persona que ya está dentro, con clave de idempotencia', async () => {
    const user = userEvent.setup()
    const movimientos: { url: string; clave: string | null; cuerpo: unknown }[] = []
    const dentro = { ...REGISTRO, dentro: true, objetos: [{ ...REGISTRO.objetos[0], estado: 'Aprobada', dentro: false, entradaEn: null }] }
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('control_accesos', 'vehiculos'))
      if (url.includes('/api/accesos/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/objetos/60/entrada')) {
        movimientos.push({ url, clave: new Headers(init?.headers).get('Idempotency-Key'), cuerpo: JSON.parse(String(init?.body)) })
        return respuestaJson({ idAutorizacion: 40, idAutorizacionObjeto: 60, tipoObjeto: 'Vehiculo', idMovimientoObjeto: 1, idVisita: 70, tipoMovimiento: 'Entrada', ocurridoEn: '2026-11-11T15:00:00Z', duracionSegundos: null, personaFuera: false })
      }
      if (url.includes('/api/accesos/40')) return respuestaJson(dentro)
      return respuestaJson({ registros: [dentro], total: 1, pagina: 1, tamano: 20 })
    })
    renderApp('/control-accesos')

    const fila = (await screen.findByText('Ana Gómez')).closest('tr')!
    await user.click(within(fila).getByRole('button', { name: 'Ver acceso de Ana Gómez' }))
    const objeto = (await screen.findByText('ABC123 · Mazda · Rojo')).closest('li')!
    await user.click(within(objeto).getByRole('button', { name: 'Registrar entrada' }))
    await user.click(within(objeto).getByRole('button', { name: 'Confirmar entrada' }))
    expect(await screen.findByText('Entrada del vehículo registrada.')).toBeInTheDocument()
    expect(movimientos).toHaveLength(1)
    expect(movimientos[0]!.cuerpo).toEqual({ idPuntoAcceso: 9, observacion: null })
    expect(movimientos[0]!.clave).toBeTruthy()
  }, 20_000)

  it('sin el módulo de vehículos no ofrece registrar su movimiento', async () => {
    const user = userEvent.setup()
    const dentro = { ...REGISTRO, dentro: true, objetos: [{ ...REGISTRO.objetos[0], estado: 'Aprobada', dentro: false, entradaEn: null }] }
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('control_accesos'))
      if (url.includes('/api/accesos/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/api/accesos/40')) return respuestaJson(dentro)
      return respuestaJson({ registros: [dentro], total: 1, pagina: 1, tamano: 20 })
    })
    renderApp('/control-accesos')
    const fila = (await screen.findByText('Ana Gómez')).closest('tr')!
    await user.click(within(fila).getByRole('button', { name: 'Ver acceso de Ana Gómez' }))
    const objeto = (await screen.findByText('ABC123 · Mazda · Rojo')).closest('li')!
    expect(within(objeto).queryByRole('button', { name: 'Registrar entrada' })).not.toBeInTheDocument()
  }, 20_000)

  it('en personas dentro avisa que el vehículo sigue dentro al salir la persona y lista los objetos dentro', async () => {
    const user = userEvent.setup()
    const PRESENCIA = {
      idPermanencia: 80, idAutorizacion: 40, idPersona: 11, persona: 'Ana Gómez', documentoPersona: '1020304050', categoria: 'Visitante', idSede: 1, sede: 'Sede Norte',
      area: 'Gerencia', anfitrion: null, puntoEntrada: 'Portería', entradaEn: '2026-11-11T14:00:00Z', segundosDentro: 600, zonaHoraria: 'America/Bogota',
      objetosDentro: ['Vehículo ABC123 · Mazda · Rojo'],
    }
    const OBJETO = {
      idMovimientoEntrada: 1, idAutorizacion: 40, idAutorizacionObjeto: 60, tipoObjeto: 'Vehiculo', objeto: 'Vehículo ABC123 · Mazda · Rojo', idPersona: 11, persona: 'Ana Gómez',
      documentoPersona: '1020304050', personaDentro: true, idSede: 1, sede: 'Sede Norte', puntoEntrada: 'Portería', entradaEn: '2026-11-11T14:05:00Z', segundosDentro: 300, zonaHoraria: 'America/Bogota',
    }
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('control_accesos', 'vehiculos'))
      if (url.includes('/api/accesos/opciones')) return respuestaJson(OPCIONES)
      if (url.includes('/api/accesos/presencia')) return respuestaJson([PRESENCIA])
      if (url.includes('/api/accesos/objetos-dentro')) return respuestaJson([OBJETO])
      if (url.includes('/permanencias/80/salida'))
        return respuestaJson({ idAutorizacion: 40, idVisita: 70, idMovimiento: 5, idPermanencia: 80, tipoMovimiento: 'Salida', ocurridoEn: '2026-11-11T15:00:00Z', alertas: [], objetosDentro: PRESENCIA.objetosDentro })
      return respuestaJson({ registros: [], total: 0, pagina: 1, tamano: 20 })
    })
    renderApp('/control-accesos')

    await user.click(await screen.findByRole('tab', { name: /Personas dentro/ }))
    expect(await screen.findByText('Con Vehículo ABC123 · Mazda · Rojo')).toBeInTheDocument()
    const seccion = await screen.findByRole('region', { name: 'Vehículos y dispositivos dentro' })
    expect(within(seccion).getByText('Vehículo ABC123 · Mazda · Rojo')).toBeInTheDocument()
    expect(within(seccion).getByRole('button', { name: 'Registrar salida' })).toBeInTheDocument()

    const persona = screen.getAllByRole('button', { name: 'Ver acceso de Ana Gómez' })[0]!.closest('li')!
    await user.click(within(persona).getByRole('button', { name: 'Registrar salida' }))
    expect(screen.getByRole('note')).toHaveTextContent(/Siguen dentro: Vehículo ABC123/)
    await user.click(screen.getByRole('button', { name: 'Confirmar salida' }))
    expect(await screen.findByText(/Salida registrada\. Siguen dentro: Vehículo ABC123/)).toBeInTheDocument()
  }, 20_000)
})
