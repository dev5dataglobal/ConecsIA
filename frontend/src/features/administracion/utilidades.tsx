import { useEffect, useState } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { Icono } from '../../shared/ui/Icono'

/** Utilidades sin componentes de las secciones de administración. */

export function iniciales(texto: string) {
  return texto
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((parte) => parte.charAt(0).toUpperCase())
    .join('')
}

export function mensajeError(error: unknown, porDefecto = 'No se pudo completar la operación.') {
  if (!(error instanceof ApiError)) return porDefecto
  if (error.status === 403) return error.problem.detail ?? 'Tu perfil no tiene permiso para esta acción.'
  const errores = error.problem.errors ? Object.values(error.problem.errors).flat() : []
  return errores[0] ?? error.problem.detail ?? porDefecto
}

/** Mensaje de resultado (éxito) que desaparece solo. */
export function useAviso() {
  const [aviso, setAviso] = useState<string | null>(null)
  useEffect(() => {
    if (!aviso) return
    const temporizador = window.setTimeout(() => setAviso(null), 4000)
    return () => window.clearTimeout(temporizador)
  }, [aviso])
  const nodo = aviso ? (
    <div className="ad-aviso" role="status">
      <Icono nombre="entrada" /> {aviso}
    </div>
  ) : null
  return { avisar: setAviso, aviso: nodo }
}

/** Retrasa un valor (búsquedas) para no consultar en cada tecla. */
export function useDiferido<T>(valor: T, ms = 300) {
  const [diferido, setDiferido] = useState(valor)
  useEffect(() => {
    const t = window.setTimeout(() => setDiferido(valor), ms)
    return () => window.clearTimeout(t)
  }, [valor, ms])
  return diferido
}

export const ESTADOS = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'Activo', texto: 'Activos' },
  { valor: 'Inactivo', texto: 'Inactivos' },
]

/**
 * URL temporal para un Blob/File; se libera al cambiar o desmontar. La URL se crea en el mismo efecto que la libera:
 * creada en un useMemo, el desmontaje simulado de StrictMode la revocaba y la imagen quedaba rota.
 */
export function useUrlObjeto(contenido: Blob | null | undefined) {
  const [url, setUrl] = useState<string | null>(null)
  useEffect(() => {
    if (!contenido) { setUrl(null); return }
    const nueva = URL.createObjectURL(contenido)
    setUrl(nueva)
    return () => URL.revokeObjectURL(nueva)
  }, [contenido])
  return contenido ? url : null
}
