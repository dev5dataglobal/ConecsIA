import type { AppModulo } from '../features/modulos/apiModulos'

const CATALOGO: Record<string, Omit<AppModulo, 'idAppModulo' | 'codigo'>> = {
  dashboard: { nombre: 'Dashboard', descripcion: null, ruta: '/dashboard', icono: 'dashboard', orden: 10, activo: true },
  autorizaciones: { nombre: 'Autorizaciones', descripcion: null, ruta: '/autorizaciones', icono: 'autorizaciones', orden: 40, activo: true },
  visitas: { nombre: 'Visitas', descripcion: null, ruta: null, icono: 'visitas', orden: 30, activo: true },
  personas: { nombre: 'Personas', descripcion: null, ruta: '/personas', icono: 'personas', orden: 50, activo: true },
  vehiculos: { nombre: 'Vehículos', descripcion: null, ruta: null, icono: 'vehiculos', orden: 60, activo: true },
  dispositivos: { nombre: 'Dispositivos', descripcion: null, ruta: null, icono: 'dispositivos', orden: 70, activo: true },
  usuarios: { nombre: 'Usuarios', descripcion: null, ruta: '/administracion/usuarios', icono: 'personas', orden: 120, activo: true },
  perfiles: { nombre: 'Perfiles', descripcion: null, ruta: '/administracion/perfiles', icono: 'autorizaciones', orden: 121, activo: true },
  organizaciones: { nombre: 'Organizaciones', descripcion: null, ruta: '/administracion/organizaciones', icono: 'sede', orden: 122, activo: true },
  zonas_empresariales: { nombre: 'Zonas empresariales', descripcion: null, ruta: '/administracion/zonas-empresariales', icono: 'administracion', orden: 123, activo: true },
}

type Codigo = keyof typeof CATALOGO

/** Respuesta simulada de GET /api/app-modulos/mios para los códigos indicados (con las tres acciones). */
export function modulosPermitidos(...codigos: Codigo[]): AppModulo[] {
  return codigos.map((codigo, indice) => ({ idAppModulo: indice + 1, codigo, ...CATALOGO[codigo]!, consultar: true, anadir: true, editar: true }))
}

/** Igual que {@link modulosPermitidos} pero con acciones explícitas para un módulo. */
export function moduloConAcciones(codigo: Codigo, acciones: { consultar?: boolean; anadir?: boolean; editar?: boolean }, id = 1): AppModulo {
  return { idAppModulo: id, codigo, ...CATALOGO[codigo]!, consultar: false, anadir: false, editar: false, ...acciones }
}
