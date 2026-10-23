import type { AppModulo } from '../features/modulos/apiModulos'

const CATALOGO: Record<string, Omit<AppModulo, 'idAppModulo' | 'codigo'>> = {
  dashboard: { nombre: 'Dashboard', descripcion: null, ruta: '/dashboard', icono: 'dashboard', orden: 10, activo: true },
  visitas: { nombre: 'Visitas', descripcion: null, ruta: null, icono: 'visitas', orden: 30, activo: true },
  personas: { nombre: 'Personas', descripcion: null, ruta: null, icono: 'personas', orden: 50, activo: true },
  administracion: { nombre: 'Administración', descripcion: null, ruta: '/administracion', icono: 'administracion', orden: 120, activo: true },
}

/** Respuesta simulada de GET /api/app-modulos/mios para los códigos indicados. */
export function modulosPermitidos(...codigos: (keyof typeof CATALOGO)[]): AppModulo[] {
  return codigos.map((codigo, indice) => ({ idAppModulo: indice + 1, codigo, ...CATALOGO[codigo]! }))
}
