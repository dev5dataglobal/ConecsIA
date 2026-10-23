import type { AppModulo } from '../../features/modulos/apiModulos'
import type { NombreIcono } from '../../shared/ui/Icono'

export interface ItemNavegacion {
  codigo: string
  etiqueta: string
  icono: NombreIcono
  /** Ruta implementada. `null` = módulo autorizado pero aún sin pantalla (se muestra como «Pronto»). */
  ruta: string | null
}

export interface GrupoNavegacion {
  titulo: string
  items: ItemNavegacion[]
}

/** Agrupación visual por código de módulo (solo presentación; el acceso lo decide el backend). */
const GRUPO_POR_CODIGO: Record<string, string> = {
  dashboard: 'General',
  control_accesos: 'Operación',
  visitas: 'Operación',
  autorizaciones: 'Operación',
  personas: 'Operación',
  vehiculos: 'Operación',
  dispositivos: 'Operación',
  alertas: 'Operación',
}
const ORDEN_GRUPOS = ['General', 'Operación', 'Gestión']

const ICONOS: Record<string, NombreIcono> = {
  dashboard: 'dashboard', accesos: 'accesos', visitas: 'visitas', autorizaciones: 'autorizaciones',
  personas: 'personas', vehiculos: 'vehiculos', dispositivos: 'dispositivos', alertas: 'alertas',
  sede: 'sede', reportes: 'reportes', configuracion: 'configuracion', administracion: 'administracion',
}

/** Rutas con pantalla en este frontend; un módulo con otra ruta se muestra como «Pronto». */
const RUTAS_IMPLEMENTADAS = new Set(['/dashboard', '/administracion'])

export function construirNavegacion(modulos: AppModulo[]): GrupoNavegacion[] {
  const grupos = new Map<string, ItemNavegacion[]>()
  for (const modulo of [...modulos].sort((a, b) => a.orden - b.orden)) {
    const titulo = GRUPO_POR_CODIGO[modulo.codigo] ?? 'Gestión'
    const lista = grupos.get(titulo) ?? []
    lista.push({
      codigo: modulo.codigo,
      etiqueta: modulo.nombre,
      icono: ICONOS[modulo.icono ?? ''] ?? 'dashboard',
      ruta: modulo.ruta && RUTAS_IMPLEMENTADAS.has(modulo.ruta) ? modulo.ruta : null,
    })
    grupos.set(titulo, lista)
  }
  return ORDEN_GRUPOS.filter((t) => grupos.has(t)).map((titulo) => ({ titulo, items: grupos.get(titulo)! }))
}
