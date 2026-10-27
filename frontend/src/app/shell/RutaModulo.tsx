import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { useMisModulos } from '../../features/modulos/apiModulos'
import { Icono } from '../../shared/ui/Icono'

/**
 * Muestra la página solo si el servidor concede el módulo. Es coherencia visual:
 * los endpoints del módulo responden 403 por sí mismos si el acceso no existe.
 */
export function RutaModulo({ codigo, children }: { codigo: string; children: ReactNode }) {
  const modulos = useMisModulos()
  if (modulos.isLoading) return <div className="sh-acceso" aria-busy="true" aria-label="Verificando acceso" />
  if (modulos.isError) return <EstadoAcceso titulo="No se pudo verificar tu acceso" texto="Inténtalo de nuevo en unos segundos." />
  if (!modulos.data?.some((m) => m.codigo === codigo))
    return <EstadoAcceso titulo="Acceso denegado" texto="Tu rol no tiene asignado este módulo. Solicítalo al administrador de tu organización." />
  return children
}

function EstadoAcceso({ titulo, texto }: { titulo: string; texto: string }) {
  return (
    <section className="sh-acceso" role="alert">
      <span aria-hidden="true"><Icono nombre="autorizaciones" /></span>
      <h1>{titulo}</h1>
      <p>{texto}</p>
      <Link to="/dashboard">Volver al inicio</Link>
    </section>
  )
}
