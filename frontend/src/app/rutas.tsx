import { Navigate, useLocation, type RouteObject } from 'react-router'
import type { ReactNode } from 'react'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { IngresarPage } from '../features/sesion/IngresarPage'
import { haySesion } from '../features/sesion/sesion'

export function RutaProtegida({ children }: { children: ReactNode }) {
  const location = useLocation()
  if (!haySesion()) return <Navigate to="/ingresar" state={{ desde: location.pathname }} replace />
  return children
}

export const rutas: RouteObject[] = [
  { index: true, element: <Navigate to="/dashboard" replace /> },
  { path: 'ingresar', element: <IngresarPage /> },
  { path: 'dashboard', element: <RutaProtegida><DashboardPage /></RutaProtegida> },
  { path: '*', element: <Navigate to="/dashboard" replace /> },
]
