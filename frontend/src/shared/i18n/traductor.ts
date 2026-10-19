import { createContext, useContext } from 'react'
import { es, type ClaveTexto } from './es'

/**
 * Textos por clave. Las organizaciones podrán sobrescribir términos sectoriales
 * (p. ej. "sede" → "campus") sin cambiar el núcleo (HU-012, D-01).
 */
export type Vocabulario = Partial<Record<ClaveTexto, string>>

export type Traductor = (clave: ClaveTexto, valores?: Record<string, string | number>) => string

export const I18nContext = createContext<Traductor | null>(null)

export function crearTraductor(vocabulario: Vocabulario = {}): Traductor {
  return (clave, valores) => {
    const plantilla = vocabulario[clave] ?? es[clave]
    if (!valores) return plantilla
    return plantilla.replace(/\{(\w+)\}/g, (coincidencia, nombre: string) =>
      nombre in valores ? String(valores[nombre]) : coincidencia,
    )
  }
}

export function useT(): Traductor {
  const t = useContext(I18nContext)
  if (!t) throw new Error('useT debe usarse dentro de I18nProvider')
  return t
}

export function esClaveTexto(valor: string): valor is ClaveTexto {
  return valor in es
}
