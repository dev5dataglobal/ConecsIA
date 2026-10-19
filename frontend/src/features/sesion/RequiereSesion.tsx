import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useReducer } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router'
import { ApiError } from '../../shared/api/problemDetails'
import { EVENTO_SESION_EXPIRADA, haySesion, olvidarSesion } from './sesion'
import { ElegirOrganizacion } from './ElegirOrganizacion'
import { olvidarOrganizacionSeleccionada } from './organizacionSeleccionada'
import { CODIGOS_ELEGIR_ORGANIZACION, useContexto } from './useContexto'

/**
 * Rutas que necesitan sesión. No protege datos (eso lo hace la API): lleva al inicio de sesión
 * cuando no hay sesión o cuando cualquier petición recibe 401 (sesión vencida o revocada).
 */
export function RequiereSesion() {
  const location = useLocation()
  const queryClient = useQueryClient()
  const [, refrescar] = useReducer((n: number) => n + 1, 0)
  const { error } = useContexto()
  const rechazada = error instanceof ApiError && error.status === 401
  const elegir =
    error instanceof ApiError &&
    error.status === 403 &&
    CODIGOS_ELEGIR_ORGANIZACION.includes(error.problem.code ?? '')
  const noPermitida = elegir && error.problem.code === 'ORGANIZACION_NO_PERMITIDA'

  useEffect(() => {
    function expirada() {
      queryClient.removeQueries({ queryKey: ['me'] })
      refrescar()
    }
    window.addEventListener(EVENTO_SESION_EXPIRADA, expirada)
    return () => window.removeEventListener(EVENTO_SESION_EXPIRADA, expirada)
  }, [queryClient])

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
  return <Outlet />
}
