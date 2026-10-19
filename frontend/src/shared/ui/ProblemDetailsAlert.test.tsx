import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { I18nProvider } from '../i18n/i18n'
import { ProblemDetailsAlert } from './ProblemDetailsAlert'

function renderAlert(problem: Parameters<typeof ProblemDetailsAlert>[0]['problem']) {
  return render(
    <I18nProvider>
      <ProblemDetailsAlert problem={problem} />
    </I18nProvider>,
  )
}

describe('ProblemDetailsAlert', () => {
  it('en un 500 muestra la referencia de soporte pero no el detalle interno', () => {
    renderAlert({ status: 500, title: 'NullReference', detail: 'at Foo.Bar()', traceId: 't-1' })

    expect(screen.getByRole('alert')).toHaveTextContent('No se pudo completar la operación')
    expect(screen.getByText(/t-1/)).toBeInTheDocument()
    expect(screen.queryByText(/Foo\.Bar/)).not.toBeInTheDocument()
    expect(screen.queryByText('NullReference')).not.toBeInTheDocument()
  })

  it('en un 422 muestra título y detalle de negocio', () => {
    renderAlert({ status: 422, title: 'Regla de negocio', detail: 'La sede está inactiva.' })

    expect(screen.getByText('Regla de negocio')).toBeInTheDocument()
    expect(screen.getByText('La sede está inactiva.')).toBeInTheDocument()
  })

  it('pide revisar campos cuando hay errores de validación', () => {
    renderAlert({ status: 400, title: 'Validación', errors: { nombre: ['Requerido'] } })

    expect(screen.getByText('Revisa los campos marcados.')).toBeInTheDocument()
  })

  it('sin conexión indica que la operación no se guardó', () => {
    renderAlert({ status: 0, title: 'error.sinConexion' })

    expect(screen.getAllByText(/La operación no se guardó/).length).toBeGreaterThan(0)
  })
})
