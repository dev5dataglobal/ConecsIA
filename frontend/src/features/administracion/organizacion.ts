import type { OrganizacionAdmin, OrganizacionEntrada } from './apiAdministracion'

/** Valores vacíos para el formulario de organización (también lo usa el de zonas empresariales). */
export function organizacionVacia(): OrganizacionEntrada {
  return { nombre: '', idTipoOrganizacion: 0, idPlan: 0, identificacion: '', direccion: '', telefono: '', correo: '', idZonaEmpresarial: null, estado: 'Activa' }
}

export function organizacionAEntrada(o: OrganizacionAdmin): OrganizacionEntrada {
  return {
    nombre: o.nombre, idTipoOrganizacion: o.idTipoOrganizacion, idPlan: o.idPlan, identificacion: o.identificacion ?? '', direccion: o.direccion ?? '',
    telefono: o.telefono ?? '', correo: o.correo ?? '', idZonaEmpresarial: o.idZonaEmpresarial, estado: o.estado === 'Inactiva' ? 'Inactiva' : 'Activa',
  }
}

const TELEFONO = /^\+?[0-9 ()-]{7,30}$/
const CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function validarOrganizacion(datos: OrganizacionEntrada) {
  const errores: Partial<Record<keyof OrganizacionEntrada, string>> = {}
  if (!datos.nombre.trim()) errores.nombre = 'El nombre es obligatorio.'
  if (!datos.idTipoOrganizacion) errores.idTipoOrganizacion = 'Selecciona el tipo.'
  if (!datos.idPlan) errores.idPlan = 'Selecciona el plan.'
  if (!datos.identificacion.trim()) errores.identificacion = 'La identificación es obligatoria.'
  if (!TELEFONO.test(datos.telefono.trim())) errores.telefono = 'Ingresa un teléfono válido (7 a 30 dígitos).'
  if (!CORREO.test(datos.correo.trim())) errores.correo = 'Ingresa un correo válido.'
  return errores
}

export function limpiarOrganizacion(datos: OrganizacionEntrada): OrganizacionEntrada {
  return { ...datos, nombre: datos.nombre.trim(), identificacion: datos.identificacion.trim(), telefono: datos.telefono.trim(), correo: datos.correo.trim(), direccion: datos.direccion?.trim() || null }
}
