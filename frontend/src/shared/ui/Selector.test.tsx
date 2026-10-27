import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { Selector } from './Selector'

const OPCIONES = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'Activo', texto: 'Activos' },
  { valor: 'Inactivo', texto: 'Inactivos' },
]

function Prueba() {
  const [valor, setValor] = useState('')
  return (
    <>
      <Selector etiqueta="Filtrar por estado" valor={valor} onChange={setValor} opciones={OPCIONES} />
      <output>{valor || 'ninguno'}</output>
      <button type="button">Afuera</button>
    </>
  )
}

describe('Selector personalizado', () => {
  it('se opera con teclado y devuelve el foco al botón', async () => {
    const user = userEvent.setup()
    render(<Prueba />)
    const boton = screen.getByRole('button', { name: /Filtrar por estado/ })

    boton.focus()
    await user.keyboard('{ArrowDown}')
    const lista = screen.getByRole('listbox', { name: 'Filtrar por estado' })
    expect(lista).toHaveFocus()
    expect(screen.getByRole('option', { name: 'Todos los estados' })).toHaveAttribute('aria-selected', 'true')

    await user.keyboard('{ArrowDown}{ArrowDown}{Enter}')
    expect(screen.queryByRole('listbox')).toBeNull()
    expect(screen.getByRole('status')).toHaveTextContent('Inactivo')
    expect(boton).toHaveFocus()
    expect(boton).toHaveAccessibleName('Filtrar por estado: Inactivos')

    await user.keyboard(' ')
    await user.keyboard('{Escape}')
    expect(screen.queryByRole('listbox')).toBeNull()
  })

  it('se opera con mouse y se cierra al hacer clic fuera', async () => {
    const user = userEvent.setup()
    render(<Prueba />)

    await user.click(screen.getByRole('button', { name: /Filtrar por estado/ }))
    await user.click(screen.getByRole('option', { name: 'Activos' }))
    expect(screen.getByRole('status')).toHaveTextContent('Activo')

    await user.click(screen.getByRole('button', { name: /Filtrar por estado/ }))
    expect(screen.getByRole('listbox')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Afuera' }))
    expect(screen.queryByRole('listbox')).toBeNull()
  })

  it('con búsqueda filtra sin distinguir tildes y elige con el teclado', async () => {
    const user = userEvent.setup()
    function ConBusqueda() {
      const [valor, setValor] = useState('')
      return (
        <>
          <Selector etiqueta="Ciudad" valor={valor} onChange={setValor} buscable
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, { valor: '05001', texto: 'Medellín, Antioquia' },
              { valor: '11001', texto: 'Bogotá, D.C., Bogotá, D.C.' }, { valor: '76001', texto: 'Cali, Valle del Cauca' }]} />
          <output>{valor || 'ninguno'}</output>
        </>
      )
    }
    render(<ConBusqueda />)

    await user.click(screen.getByRole('button', { name: /Ciudad/ }))
    const campo = screen.getByRole('combobox', { name: 'Buscar ciudad' })
    expect(campo).toHaveFocus()
    await user.type(campo, 'bogota')
    expect(screen.getAllByRole('option')).toHaveLength(1)
    await user.keyboard('{Enter}')
    expect(screen.getByRole('status')).toHaveTextContent('11001')
    expect(screen.queryByRole('listbox')).toBeNull()

    await user.click(screen.getByRole('button', { name: /Ciudad/ }))
    await user.type(screen.getByRole('combobox', { name: 'Buscar ciudad' }), 'zzz')
    expect(screen.getByText('Sin resultados')).toBeInTheDocument()
    await user.keyboard('{Escape}')
    expect(screen.queryByRole('listbox')).toBeNull()
    expect(screen.getByRole('status')).toHaveTextContent('11001')
  })
})
