import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderApp, respuestaJson } from '../../test/render'

describe('AppShell', () => {
  it('muestra servidor disponible cuando la API responde', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      respuestaJson({ status: 'Healthy', checks: [{ name: 'postgresql', status: 'Healthy' }] }),
    )

    renderApp()

    expect(await screen.findByText('Servidor disponible')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Plataforma de control de accesos' })).toBeVisible()
    expect(screen.getByText('Sin sesión iniciada')).toBeInTheDocument()
  })

  it('muestra sin conexión y la referencia cuando la API falla', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      respuestaJson(
        { status: 503, title: 'Unhealthy', traceId: 'tr-9' },
        503,
        'application/problem+json',
      ),
    )

    renderApp()

    expect(await screen.findByText('Sin conexión con el servidor')).toBeInTheDocument()
    expect(await screen.findByText(/tr-9/)).toBeInTheDocument()
  })

  it('el menú móvil se abre y cierra con su botón accesible', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(respuestaJson({ status: 'Healthy' }))
    const user = userEvent.setup()

    renderApp()
    const boton = screen.getByRole('button', { name: 'Abrir menú' })
    await user.click(boton)

    expect(boton).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByRole('button', { name: 'Cerrar menú' })).toBeInTheDocument()
  })

  it('una ruta desconocida muestra la página de no encontrado', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(respuestaJson({ status: 'Healthy' }))

    renderApp('/no-existe')

    expect(await screen.findByText('Página no encontrada')).toBeInTheDocument()
  })
})
