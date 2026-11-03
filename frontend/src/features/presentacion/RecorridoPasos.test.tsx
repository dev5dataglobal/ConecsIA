import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { PASOS } from './contenido'
import { RecorridoPasos } from './RecorridoPasos'

describe('RecorridoPasos', () => {
  it('presenta los cinco pasos en orden, con su número accesible', () => {
    render(<RecorridoPasos />)
    const titulos = screen.getAllByRole('heading', { level: 3 })
    expect(titulos).toHaveLength(PASOS.length)
    PASOS.forEach((p, i) => {
      expect(titulos[i]).toHaveTextContent(`Paso ${i + 1} de ${PASOS.length}: ${p.titulo}`)
      expect(screen.getByText(p.texto)).toBeInTheDocument()
    })
    // Lo decorativo (trayectoria, números visuales, iconos) queda fuera del árbol accesible.
    expect(document.querySelector('.lp-trayecto')).toHaveAttribute('aria-hidden', 'true')
    expect(document.querySelector('.lp-nodo')).toHaveAttribute('aria-hidden', 'true')
  })
})
