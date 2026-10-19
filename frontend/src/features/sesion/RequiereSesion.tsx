import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useReducer } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { useT } from '../../shared/i18n/traductor'
import { EVENTO_SESION_EXPIRADA, haySesion, olvidarSesion } from './sesion'
import { ElegirOrganizacion } from './ElegirOrganizacion'
import { olvidarOrganizacionSeleccionada } from './organizacionSeleccionada'
import { CODIGOS_ELEGIR_ORGANIZACION, useContexto, usePlataforma } from './useContexto'

/** Escucha la expiración de la sesión (cualquier 401) para volver a evaluar la ruta. */
function useSesionExpirada() {
  const queryClient = useQueryClient()
  const [, refrescar] = useReducer((n: number) => n + 1, 0)
  useEffect(() => {
    function expirada() {
      queryClient.removeQueries({ queryKey: ['me'] })
      refrescar()
    }
    window.addEventListener(EVENTO_SESION_EXPIRADA, expirada)
    return () => window.removeEventListener(EVENTO_SESION_EXPIRADA, expirada)
  }, [queryClient])
}

/**
 * Rutas que necesitan sesión y organización. No protege datos (eso lo hace la API): lleva al
 * inicio de sesión ante 401, pide elegir organización con varias membresías y envía a la consola
 * SaaS a quien solo tiene permisos de plataforma (HU-009).
 */
export function RequiereSesion() {
  const t = useT()
  const location = useLocation()
  const queryClient = useQueryClient()
  useSesionExpirada()
  const { error } = useContexto()
  const rechazada = error instanceof ApiError && error.status === 401
  const elegir =
    error instanceof ApiError &&
    error.status === 403 &&
    CODIGOS_ELEGIR_ORGANIZACION.includes(error.problem.code ?? '')
  const noPermitida = elegir && error.problem.code === 'ORGANIZACION_NO_PERMITIDA'
  const sinMembresia =
    error instanceof ApiError && error.status === 403 && error.problem.code === 'SIN_MEMBRESIA'
  const { data: plataforma } = usePlataforma(sinMembresia)

  useEffect(() => {
    if (rechazada) {
      olvidarSesion()
      queryClient.removeQueries({ queryKey: ['me'] })
    }
  }, [rechazada, queryClient])

  useEffect(() => {
    // Selección guardada que el servidor ya no acepta (membresía inactivada u organización ajena).
    if (noPermitida) olvidarOrganizacionSeleccionada()
  }, [noPermitida])

  if (!haySesion() || rechazada) {
    return <Navigate to="/ingresar" replace state={{ desde: location.pathname }} />
  }
  if (elegir) return <ElegirOrganizacion />
  // Sin membresía: se espera a saber si tiene permisos de plataforma antes de decidir.
  if (sinMembresia && !plataforma) return null
  if (sinMembresia && plataforma && (plataforma.permisosSaas ?? []).length > 0) {
    return <Navigate to="/saas/organizaciones" replace />
  }
  if (sinMembresia && plataforma) {
    return <p className="py-10 text-center text-sm text-tinta-suave">{t('sesion.sinMembresia')}</p>
  }
  return <Outlet />
}

/** Consola SaaS: requiere sesión, no organización. La API exige los permisos del plano SaaS. */
export function RequierePlataforma() {
  const location = useLocation()
  useSesionExpirada()
  const { error } = usePlataforma()
  if (!haySesion() || (error instanceof ApiError && error.status === 401)) {
    return <Navigate to="/ingresar" replace state={{ desde: location.pathname }} />
  }
  return <Outlet />
}
