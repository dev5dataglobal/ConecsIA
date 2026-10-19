import { describe, expect, it } from 'vitest'
import { crearTraductor } from './traductor'

describe('crearTraductor', () => {
  it('usa el diccionario base y reemplaza variables', () => {
    const t = crearTraductor()
    expect(t('error.referencia', { traceId: 'x1' })).toBe('Referencia para soporte: x1')
  })

  it('el vocabulario de la organización sobrescribe términos sin tocar el resto', () => {
    const t = crearTraductor({ 'contexto.sede': 'Campus' })
    expect(t('contexto.sede')).toBe('Campus')
    expect(t('contexto.organizacion')).toBe('Organización')
  })
})
