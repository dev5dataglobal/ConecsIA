import { describe, expect, it, vi } from 'vitest'
import { respuestaJson } from '../../test/render'
import { apiFetch } from './http'
import { ApiError, fieldErrors } from './problemDetails'

describe('apiFetch', () => {
  it('devuelve el cuerpo JSON en respuestas exitosas', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(respuestaJson({ status: 'Healthy' }))

    await expect(apiFetch('/api/v1/health')).resolves.toEqual({ status: 'Healthy' })
  })

  it('convierte un 400 con errores por campo en ApiError con Problem Details', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      respuestaJson(
        { title: 'Validación', status: 400, traceId: 'abc', errors: { Documento: ['Requerido'] } },
        400,
        'application/problem+json',
      ),
    )

    const error = await apiFetch('/api/v1/x').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    const problem = (error as ApiError).problem
    expect(problem.status).toBe(400)
    expect(problem.traceId).toBe('abc')
    expect(fieldErrors(problem, 'documento')).toEqual(['Requerido'])
  })

  it('ignora propiedades con tipos inesperados en el cuerpo de error', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      respuestaJson({ title: 42, errors: 'nope' }, 500, 'application/problem+json'),
    )

    const error = (await apiFetch('/api/v1/x').catch((e: unknown) => e)) as ApiError

    expect(error.problem).toMatchObject({ status: 500, title: undefined, errors: undefined })
  })

  it('una falla de red no se presenta como éxito', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'))

    const error = (await apiFetch('/api/v1/x').catch((e: unknown) => e)) as ApiError

    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(0)
  })
})
