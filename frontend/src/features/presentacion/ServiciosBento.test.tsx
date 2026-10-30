import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { SERVICIOS } from './contenido'
import { ServiciosBento } from './ServiciosBento'

describe('ServiciosBento', () => {
  it('muestra los nueve servicios con su texto, modalidades y módulo opcional', () => {
    render(<ServiciosBento />)
    for (const s of SERVICIOS) {
      expect(screen.getByRole('heading', { level: 3, name: s.titulo })).toBeInTheDocument()
      expect(screen.getByText(s.texto)).toBeInTheDocument()
    }
    const modalidades = screen.getByRole('list', { name: 'Modalidades de autorización' })
    expect(modalidades).toHaveTextContent('ÚnicaDiariaTemporalRecurrentePermanente')
    expect(screen.getByText('Módulo opcional según el plan de la organización')).toBeInTheDocument()
  })

  it('las vistas de ejemplo son decorativas y la principal se marca como ilustrativa', () => {
    render(<ServiciosBento />)
    for (const v of document.querySelectorAll('.lp-sv-vista'))
      expect(v).toHaveAttribute('aria-hidden', 'true')
    expect(screen.getByText('Ejemplo ilustrativo')).toBeInTheDocument()
  })
})
