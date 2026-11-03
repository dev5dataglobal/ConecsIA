import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { CarruselSectores } from './CarruselSectores'
import { SECTORES } from './contenido'

const central = () => document.querySelector('[data-central] h3')?.textContent

describe('CarruselSectores', () => {
  it('cicla entre el último y el primero con botones, indicadores y teclado', async () => {
    render(<CarruselSectores />)
    const u = userEvent.setup()
    const n = SECTORES.length
    expect(central()).toBe(SECTORES[0]!.nombre)
    expect(screen.getByText(`${SECTORES[0]!.nombre}, 1 de ${n}`)).toBeInTheDocument()

    await u.click(screen.getByRole('button', { name: 'Tipo de organización anterior' }))
    expect(central()).toBe(SECTORES[n - 1]!.nombre)

    await u.click(screen.getByRole('button', { name: 'Tipo de organización siguiente' }))
    expect(central()).toBe(SECTORES[0]!.nombre)

    await u.click(screen.getByRole('button', { name: `Ver ${SECTORES[3]!.nombre}` }))
    expect(central()).toBe(SECTORES[3]!.nombre)
    expect(screen.getByRole('button', { name: `Ver ${SECTORES[3]!.nombre}` })).toHaveAttribute(
      'aria-current',
      'true',
    )

    screen.getByLabelText(/usa las flechas/).focus()
    await u.keyboard('{End}')
    expect(central()).toBe(SECTORES[n - 1]!.nombre)
    await u.keyboard('{ArrowRight}')
    expect(central()).toBe(SECTORES[0]!.nombre)
  })

  it('solo la tarjeta activa queda expuesta a tecnologías de asistencia', () => {
    render(<CarruselSectores />)
    expect(document.querySelectorAll('.lp-cs-tarjeta:not([aria-hidden])')).toHaveLength(1)
    expect(screen.getByText('Seleccionado')).toBeInTheDocument()
  })
})
