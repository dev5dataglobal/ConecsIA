import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { rutas } from '../../app/rutas'
import { CONTACTO_VACIO, enviarContacto, validarContacto } from './contacto'

function abrir(ruta = '/presentacion') {
  render(<RouterProvider router={createMemoryRouter(rutas, { initialEntries: [ruta] })} />)
}

// Carga el árbol de rutas completo: en máquinas cargadas supera el límite por defecto de 5 s.
describe('PresentacionPage', { timeout: 20000 }, () => {
  it('se abre sin sesión y enlaza al inicio de sesión', async () => {
    abrir()
    expect(
      await screen.findByRole('heading', { level: 1, name: /Cada acceso bajo control/ }),
    ).toBeInTheDocument()
  })

  it('la raíz muestra la presentación a visitantes sin sesión', async () => {
    abrir('/')
    expect(
      await screen.findByRole('heading', { level: 1, name: /Cada acceso bajo control/ }),
    ).toBeInTheDocument()
    for (const a of screen.getAllByRole('link', { name: 'Iniciar sesión' }))
      expect(a).toHaveAttribute('href', '/ingresar')
  })

  it('valida el formulario y envía al endpoint público', async () => {
    const fetchSpy = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValue(new Response(null, { status: 202 }))
    abrir()
    const u = userEvent.setup()
    await u.click(screen.getByRole('button', { name: /Enviar solicitud/ }))
    expect(screen.getByLabelText('Nombre de la organización')).toHaveAttribute(
      'aria-invalid',
      'true',
    )
    expect(screen.getByLabelText('Nombre de la organización')).toHaveFocus()

    await u.type(screen.getByLabelText('Nombre de la organización'), 'Organización demo')
    await u.type(screen.getByLabelText('Persona de contacto'), 'Persona demo')
    await u.type(screen.getByLabelText('Correo electrónico'), 'demo@ejemplo.test')
    await u.selectOptions(screen.getByLabelText('Tipo de organización'), 'Coworking')
    await u.type(
      screen.getByLabelText('¿Qué necesitas gestionar?'),
      'Controlar visitas en dos sedes.',
    )
    await u.click(screen.getByRole('button', { name: /Enviar solicitud/ }))

    expect(await screen.findByText('Recibimos tu mensaje')).toBeInTheDocument()
    const [url, init] = fetchSpy.mock.calls[0]!
    expect(url).toBe('/api/publico/contactos')
    expect(JSON.parse(String(init?.body))).toMatchObject({
      organizacion: 'Organización demo',
      tipoOrganizacion: 'Coworking',
      numeroSedes: null,
      sitioWeb: null,
    })
    fetchSpy.mockRestore()
  })
})

describe('contacto', () => {
  it('exige los campos obligatorios y acepta opcionales vacíos', () => {
    expect(Object.keys(validarContacto(CONTACTO_VACIO)).sort()).toEqual([
      'correo',
      'mensaje',
      'nombre',
      'organizacion',
      'tipo',
    ])
    expect(validarContacto({ ...CONTACTO_VACIO, telefono: 'abc', sedes: '2,5' })).toMatchObject({
      telefono: expect.any(String),
      sedes: expect.any(String),
    })
  })

  it('traduce 400, 429 y errores de red sin dar el envío por hecho', async () => {
    const spy = vi.spyOn(globalThis, 'fetch')
    spy.mockResolvedValueOnce(new Response(null, { status: 400 }))
    await expect(enviarContacto(CONTACTO_VACIO)).rejects.toBe('datos')
    spy.mockResolvedValueOnce(new Response(null, { status: 429 }))
    await expect(enviarContacto(CONTACTO_VACIO)).rejects.toBe('limite')
    spy.mockResolvedValueOnce(new Response(null, { status: 500 }))
    await expect(enviarContacto(CONTACTO_VACIO)).rejects.toBe('red')
    spy.mockRejectedValueOnce(new TypeError('offline'))
    await expect(enviarContacto(CONTACTO_VACIO)).rejects.toBe('red')
    spy.mockRestore()
  })
})
