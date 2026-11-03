import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ComparativaRetos } from './ComparativaRetos'
import { RETOS } from './contenido'

describe('ComparativaRetos', () => {
  it('relaciona cada problema con su solución en el mismo elemento de lista', () => {
    render(<ComparativaRetos />)
    expect(
      screen.getByRole('heading', { level: 2, name: /memoria de la portería/ }),
    ).toBeInTheDocument()
    const filas = screen.getAllByRole('listitem')
    expect(filas).toHaveLength(RETOS.length)
    RETOS.forEach((r, i) => {
      const fila = within(filas[i]!)
      expect(fila.getByText(r.reto, { exact: false })).toBeInTheDocument()
      expect(fila.getByText(r.respuesta, { exact: false })).toBeInTheDocument()
      // Las etiquetas por bloque dan contexto sin depender del color ni del encabezado visual.
      expect(filas[i]).toHaveTextContent(/Hoy.*Con ConecsIA/)
    })
  })
})
