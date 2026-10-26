import { Navigate, useLocation, type RouteObject } from 'react-router'
import type { ReactNode } from 'react'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { AdministracionInicio, AdministracionPage } from '../features/administracion/AdministracionPage'
import { SECCIONES } from '../features/administracion/secciones'
import { IngresarPage } from '../features/sesion/IngresarPage'
import { haySesion } from '../features/sesion/sesion'
import { AppShell } from './shell/AppShell'
import { RutaModulo } from './shell/RutaModulo'

export function RutaProtegida({ children }: { children: ReactNode }) {
  const location = useLocation()
  if (!haySesion()) return <Navigate to="/ingresar" state={{ desde: location.pathname }} replace />
  return children
}

export const rutas: RouteObject[] = [
  { index: true, element: <Navigate to="/dashboard" replace /> },
  { path: 'ingresar', element: <IngresarPage /> },
  {
    element: <RutaProtegida><AppShell /></RutaProtegida>,
    children: [
      { path: 'dashboard', element: <RutaModulo codigo="dashboard"><DashboardPage /></RutaModulo> },
      { path: 'administracion', element: <AdministracionInicio /> },
      ...SECCIONES.map((s) => ({
        path: `administracion/${s.id}`,
        element: <RutaModulo codigo={s.codigo}><AdministracionPage seccion={s.id} /></RutaModulo>,
      })),
    ],
  },
  { path: '*', element: <Navigate to="/dashboard" replace /> },
]
