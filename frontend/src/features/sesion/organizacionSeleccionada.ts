/**
 * Organización elegida por el usuario cuando tiene varias membresías (HU-007, D-03b). Se envía en
 * la cabecera X-Organizacion-Id; el servidor solo la acepta si corresponde a una membresía vigente.
 * No es un control de seguridad.
 */
const CLAVE = 'conecsia.organizacion'
export const CABECERA_ORGANIZACION = 'X-Organizacion-Id'

export function leerOrganizacionSeleccionada(): string | null {
  try {
    return sessionStorage.getItem(CLAVE)
  } catch {
    return null
  }
}

export function guardarOrganizacionSeleccionada(organizacionId: string) {
  try {
    sessionStorage.setItem(CLAVE, organizacionId)
  } catch {
    // Sin almacenamiento: el servidor volverá a pedir la selección.
  }
}

export function olvidarOrganizacionSeleccionada() {
  try {
    sessionStorage.removeItem(CLAVE)
  } catch {
    // ignorado
  }
}
