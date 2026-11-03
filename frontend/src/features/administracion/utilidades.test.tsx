import { render, screen } from '@testing-library/react'
import { StrictMode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useUrlObjeto } from './utilidades'

function Imagen({ blob }: { blob: Blob | null }) {
  const url = useUrlObjeto(blob)
  return url ? <img alt="foto" src={url} /> : <span>Sin foto</span>
}

describe('useUrlObjeto', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('mantiene válida la URL mostrada aunque StrictMode desmonte y vuelva a montar los efectos', () => {
    let contador = 0
    const revocadas = new Set<string>()
    vi.stubGlobal('URL', Object.assign(URL, {
      createObjectURL: vi.fn(() => `blob:prueba-${++contador}`),
      revokeObjectURL: vi.fn((url: string) => revocadas.add(url)),
    }))

    const { rerender, unmount } = render(<StrictMode><Imagen blob={new Blob(['x'], { type: 'image/png' })} /></StrictMode>)
    const src = screen.getByRole('img', { name: 'foto' }).getAttribute('src')!
    expect(revocadas.has(src)).toBe(false)

    rerender(<StrictMode><Imagen blob={null} /></StrictMode>)
    expect(screen.getByText('Sin foto')).toBeInTheDocument()
    expect(revocadas.has(src)).toBe(true)
    unmount()
  })
})
