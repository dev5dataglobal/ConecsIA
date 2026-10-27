import type { ReactNode } from 'react'
import type { NombreIcono } from '../../shared/ui/Icono'

export type Seccion = 'usuarios' | 'perfiles' | 'organizaciones' | 'zonas-empresariales' | 'sedes'

export interface SeccionProps {
  /** Pestañas de navegación entre secciones (se muestran dentro de la cabecera). */
  pestanas: ReactNode
}

/** Cada sección es un módulo propio (app_modulos) con su ruta; la pestaña solo aparece si el servidor lo concede. */
export const SECCIONES: { id: Seccion; codigo: string; etiqueta: string; icono: NombreIcono }[] = [
  { id: 'usuarios', codigo: 'usuarios', etiqueta: 'Usuarios', icono: 'personas' },
  { id: 'perfiles', codigo: 'perfiles', etiqueta: 'Perfiles', icono: 'autorizaciones' },
  { id: 'organizaciones', codigo: 'organizaciones', etiqueta: 'Organizaciones', icono: 'sede' },
  { id: 'zonas-empresariales', codigo: 'zonas_empresariales', etiqueta: 'Zonas empresariales', icono: 'administracion' },
  { id: 'sedes', codigo: 'sedes', etiqueta: 'Sedes y áreas', icono: 'sede' },
]
