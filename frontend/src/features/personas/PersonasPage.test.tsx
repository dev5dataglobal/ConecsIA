import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { guardarAccessToken, olvidarSesion } from '../sesion/sesion'
import { renderApp, respuestaJson } from '../../test/render'
import { moduloConAcciones, modulosPermitidos } from '../../test/modulos'

function tokenFalso(payload: Record<string, unknown>) {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/=+$/, '')
  return `${b64({ alg: 'none' })}.${b64(payload)}.firma`
}

const MIS_MODULOS = '/api/app-modulos/mios'
const CATALOGOS = {
  tiposPersona: [{ id: 3, nombre: 'Visitante' }],
  tiposDocumento: [{ id: 1, codigo: 'CC', nombre: 'Cédula de ciudadanía', patronValidacion: '^[0-9]{3,10}$' }],
  tiposTelefono: ['Móvil', 'Fijo', 'Trabajo', 'Emergencia', 'Otro'],
  tiposVehiculo: ['Automóvil', 'Motocicleta', 'Otro'],
  relacionesVehiculo: ['Propietario', 'Responsable', 'Conductor'],
}
const PERSONA = {
  idPersona: 11, idOrganizacion: 1, idTipoPersona: 3, tipoPersona: 'Visitante', idTipoDocumento: 1, tipoDocumento: 'CC', numeroDocumento: '1020304050',
  nombres: 'Ana', apellidos: 'Gómez', correo: 'ana@ejemplo.test', empresaProcedencia: null, observacion: null, telefonoPrincipal: '+57 3001234567',
  tieneFoto: false, activo: true, creadoEn: '2026-10-26T12:00:00Z',
}
const DETALLE = {
  ...PERSONA,
  registradoPor: 'operador@ejemplo.test',
  telefonos: [{ idTelefono: 5, idPersona: 11, codigoPais: '+57', numero: '3001234567', tipo: 'Móvil', principal: true, activo: true }],
  vehiculos: [{ idVehiculo: 8, idPersona: 11, placa: 'ABC123', tipo: 'Automóvil', marca: 'Renault', modelo: null, color: null, relacion: 'Propietario', activo: true, otrasPersonas: 1 }],
}

describe('Personas', () => {
  beforeEach(() => {
    olvidarSesion()
    guardarAccessToken(tokenFalso({ sub: '9', organization_id: '1' }))
  })

  it('lista personas y oculta acciones sin permiso de añadir o editar', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson([moduloConAcciones('personas', { consultar: true })])
      if (url.includes('/api/personas/catalogos')) return respuestaJson(CATALOGOS)
      return respuestaJson([PERSONA])
    })
    renderApp('/personas')

    expect(await screen.findByText('Ana Gómez')).toBeInTheDocument()
    expect(screen.getByText('1020304050')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Nueva persona/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Inactivar Ana Gómez/ })).not.toBeInTheDocument()
  })

  it('valida campos obligatorios, correo y formato del documento antes de enviar', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('personas'))
      if (url.includes('/api/personas/catalogos')) return respuestaJson(CATALOGOS)
      if (url.includes('buscar-documento')) return new Response(null, { status: 404 })
      return respuestaJson([])
    })
    renderApp('/personas')

    await user.click(await screen.findByRole('button', { name: /Nueva persona/ }))
    const dialogo = await screen.findByRole('dialog', { name: 'Nueva persona' })
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))
    expect(within(dialogo).getByText('Los nombres son obligatorios.')).toBeInTheDocument()
    expect(within(dialogo).getByText('Selecciona el tipo de documento.')).toBeInTheDocument()

    await user.click(within(dialogo).getByRole('button', { name: /^Tipo de documento:/ }))
    await user.click(await screen.findByRole('option', { name: /Cédula de ciudadanía/ }))
    await user.type(within(dialogo).getByLabelText('Número de documento'), '12AB')
    await user.type(within(dialogo).getByLabelText('Correo electrónico'), 'no-es-correo')
    await user.click(within(dialogo).getByRole('button', { name: 'Guardar' }))
    expect(within(dialogo).getByText(/no tiene un formato válido para Cédula/)).toBeInTheDocument()
    expect(within(dialogo).getByText('El correo no es válido.')).toBeInTheDocument()
    expect(fetch.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(false)
  })

  it('muestra en el detalle los teléfonos y vehículos de la persona, sin vehículos si el módulo no está asignado', async () => {
    const user = userEvent.setup()
    vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('personas'))
      if (url.includes('/api/personas/catalogos')) return respuestaJson(CATALOGOS)
      if (url.endsWith('/api/personas/11')) return respuestaJson(DETALLE)
      return respuestaJson([PERSONA])
    })
    renderApp('/personas')

    await user.click(await screen.findByRole('button', { name: 'Ver detalle de Ana Gómez' }))
    const telefonos = await screen.findByRole('region', { name: 'Teléfonos de Ana Gómez' })
    expect(within(telefonos).getByText('+57 3001234567')).toBeInTheDocument()
    expect(within(telefonos).getByText(/Principal/)).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Vehículos de Ana Gómez' })).not.toBeInTheDocument()
    expect(screen.getByText('El módulo de vehículos no está habilitado para tu perfil.')).toBeInTheDocument()
  })

  it('agrega un teléfono validando el formato colombiano', async () => {
    const user = userEvent.setup()
    const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
      const url = String(entrada)
      if (url.includes(MIS_MODULOS)) return respuestaJson(modulosPermitidos('personas', 'vehiculos'))
      if (url.includes('/api/personas/catalogos')) return respuestaJson(CATALOGOS)
      if (url.endsWith('/api/personas/11/telefonos') && init?.method === 'POST')
        return respuestaJson({ idTelefono: 6, idPersona: 11, codigoPais: '+57', numero: '6015550000', tipo: 'Fijo', principal: false, activo: true }, 201)
      if (url.endsWith('/api/personas/11')) return respuestaJson(DETALLE)
      return respuestaJson([PERSONA])
    })
    renderApp('/personas')

    await user.click(await screen.findByRole('button', { name: 'Ver detalle de Ana Gómez' }))
    expect(await screen.findByRole('region', { name: 'Vehículos de Ana Gómez' })).toBeInTheDocument()
    expect(screen.getByText(/compartido con 1 persona/)).toBeInTheDocument()
    const telefonos = screen.getByRole('region', { name: 'Teléfonos de Ana Gómez' })
    await user.click(within(telefonos).getByRole('button', { name: '+ Agregar teléfono' }))
    const subformulario = within(telefonos).getByRole('group', { name: 'Nuevo teléfono' })
    await user.type(within(subformulario).getByLabelText('Número'), '12345')
    await user.click(within(subformulario).getByRole('button', { name: 'Guardar teléfono' }))
    expect(within(subformulario).getByText(/10 dígitos/)).toBeInTheDocument()

    await user.click(within(subformulario).getByRole('button', { name: /^Tipo de teléfono:/ }))
    await user.click(await screen.findByRole('option', { name: 'Fijo' }))
    await user.clear(within(subformulario).getByLabelText('Número'))
    await user.type(within(subformulario).getByLabelText('Número'), '601 555 0000')
    await user.click(within(subformulario).getByRole('button', { name: 'Guardar teléfono' }))

    const envio = fetch.mock.calls.find(([url, init]) => String(url).endsWith('/api/personas/11/telefonos') && init?.method === 'POST')
    expect(JSON.parse(String(envio?.[1]?.body))).toMatchObject({ codigoPais: '+57', numero: '601 555 0000', tipo: 'Fijo' })
  })

  describe('foto y dispositivos', () => {
    beforeEach(() => {
      let n = 0
      vi.stubGlobal('URL', Object.assign(URL, { createObjectURL: vi.fn(() => `blob:foto-${++n}`), revokeObjectURL: vi.fn() }))
    })
    afterEach(() => vi.unstubAllGlobals())

    const CON_FOTO = { ...PERSONA, tieneFoto: true }
    const DISPOSITIVOS = [
      { idDispositivo: 4, idPersona: 11, idTipoDispositivo: 2, tipoDispositivo: 'Portátil', codigoIdentificador: 'SN-123', serial: null, marca: 'Dell', modelo: null, color: null, descripcion: null, activo: true, creadoEn: '2026-10-26T12:00:00Z' },
    ]

    function simular(modulos = modulosPermitidos('personas', 'vehiculos', 'dispositivos')) {
      return vi.spyOn(globalThis, 'fetch').mockImplementation(async (entrada, init) => {
        const url = String(entrada)
        if (url.includes(MIS_MODULOS)) return respuestaJson(modulos)
        if (url.includes('/api/personas/catalogos')) return respuestaJson(CATALOGOS)
        if (url.endsWith('/api/personas/11/foto')) return new Response('png', { status: 200, headers: { 'Content-Type': 'image/png' } })
        if (url.endsWith('/api/dispositivos/tipos')) return respuestaJson([{ idTipoDispositivo: 2, nombre: 'Portátil', descripcion: null, activo: true }])
        if (url.endsWith('/api/personas/11/dispositivos') && init?.method === 'POST')
          return respuestaJson({ ...DISPOSITIVOS[0], idDispositivo: 5, codigoIdentificador: 'TAB-9' }, 201)
        if (url.endsWith('/api/personas/11/dispositivos')) return respuestaJson(DISPOSITIVOS)
        if (url.endsWith('/api/personas/11')) return respuestaJson({ ...DETALLE, tieneFoto: true })
        return respuestaJson([CON_FOTO])
      })
    }

    it('muestra la foto guardada en el detalle y en la edición, y quién registró a la persona', async () => {
      const user = userEvent.setup()
      simular()
      renderApp('/personas')

      await user.click(await screen.findByRole('button', { name: 'Ver detalle de Ana Gómez' }))
      const dialogo = await screen.findByRole('dialog', { name: 'Ana Gómez' })
      expect(await within(dialogo).findByRole('img', { name: 'Foto de Ana Gómez' })).toHaveAttribute('src', expect.stringMatching(/^blob:/))
      expect(within(dialogo).getByText(/operador@ejemplo\.test/)).toBeInTheDocument()

      await user.click(within(dialogo).getByRole('button', { name: 'Editar datos' }))
      const edicion = await screen.findByRole('dialog', { name: 'Editar persona' })
      expect(await within(edicion).findByRole('img', { name: 'Foto de Ana Gómez' })).toBeInTheDocument()
      expect(within(edicion).getByRole('button', { name: 'Quitar' })).toBeInTheDocument()
    })

    it('previsualiza la nueva foto, valida formato y tamaño y al descartarla conserva la actual', async () => {
      const user = userEvent.setup({ applyAccept: false })
      const fetch = simular()
      renderApp('/personas')

      await user.click(await screen.findByRole('button', { name: 'Editar Ana Gómez' }))
      const edicion = await screen.findByRole('dialog', { name: 'Editar persona' })
      const input = within(edicion).getByLabelText('Seleccionar foto')

      await user.upload(input, new File(['gif'], 'foto.gif', { type: 'image/gif' }))
      expect(within(edicion).getByRole('alert')).toHaveTextContent('La foto debe ser PNG, JPEG o WebP.')
      await user.upload(input, new File([new Uint8Array(2 * 1024 * 1024 + 1)], 'grande.png', { type: 'image/png' }))
      expect(within(edicion).getByRole('alert')).toHaveTextContent('La foto no puede superar 2 MB.')

      await user.upload(input, new File(['png'], 'nueva.png', { type: 'image/png' }))
      expect(within(edicion).getByRole('img', { name: 'Vista previa de la foto' })).toBeInTheDocument()
      await user.click(within(edicion).getByRole('button', { name: 'Descartar selección' }))
      expect(await within(edicion).findByRole('img', { name: 'Foto de Ana Gómez' })).toBeInTheDocument()

      await user.click(within(edicion).getByRole('button', { name: 'Guardar' }))
      await vi.waitFor(() => expect(fetch.mock.calls.some(([u, i]) => String(u).endsWith('/api/personas/11') && i?.method === 'PUT')).toBe(true))
      // Sin reemplazo, la foto actual no se carga de nuevo ni se elimina.
      expect(fetch.mock.calls.some(([u, i]) => String(u).endsWith('/foto') && (i?.method === 'POST' || i?.method === 'DELETE'))).toBe(false)
    })

    it('lista y agrega dispositivos de la persona validando los obligatorios', async () => {
      const user = userEvent.setup()
      const fetch = simular()
      renderApp('/personas')

      await user.click(await screen.findByRole('button', { name: 'Ver detalle de Ana Gómez' }))
      const seccion = await screen.findByRole('region', { name: 'Dispositivos de Ana Gómez' })
      expect(await within(seccion).findByText('SN-123')).toBeInTheDocument()

      await user.click(within(seccion).getByRole('button', { name: '+ Agregar dispositivo' }))
      const formulario = within(seccion).getByRole('group', { name: 'Nuevo dispositivo' })
      await user.click(within(formulario).getByRole('button', { name: 'Guardar dispositivo' }))
      expect(within(formulario).getByText('Selecciona el tipo de dispositivo.')).toBeInTheDocument()
      expect(within(formulario).getByText('El identificador es obligatorio.')).toBeInTheDocument()

      await user.click(within(formulario).getByRole('button', { name: /^Tipo de dispositivo:/ }))
      await user.click(await screen.findByRole('option', { name: 'Portátil' }))
      await user.type(within(formulario).getByLabelText('Identificador'), 'TAB-9')
      await user.click(within(formulario).getByRole('button', { name: 'Guardar dispositivo' }))

      await vi.waitFor(() => expect(fetch.mock.calls.some(([u, i]) => String(u).endsWith('/api/personas/11/dispositivos') && i?.method === 'POST')).toBe(true))
      const envio = fetch.mock.calls.find(([u, i]) => String(u).endsWith('/api/personas/11/dispositivos') && i?.method === 'POST')
      const cuerpo = JSON.parse(String(envio?.[1]?.body))
      expect(cuerpo).toMatchObject({ idTipoDispositivo: 2, codigoIdentificador: 'TAB-9' })
      expect(cuerpo).not.toHaveProperty('idPersonaResponsable')
      expect(cuerpo).not.toHaveProperty('idOrganizacion')
    })

    it('sin el módulo de dispositivos no muestra la sección', async () => {
      const user = userEvent.setup()
      simular(modulosPermitidos('personas'))
      renderApp('/personas')

      await user.click(await screen.findByRole('button', { name: 'Ver detalle de Ana Gómez' }))
      expect(await screen.findByText('El módulo de dispositivos no está habilitado para tu perfil.')).toBeInTheDocument()
      expect(screen.queryByRole('region', { name: 'Dispositivos de Ana Gómez' })).not.toBeInTheDocument()
    })
  })
})
