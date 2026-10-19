import type { RouteObject } from 'react-router'
import { InicioPage } from '../features/inicio/InicioPage'
import { NoEncontradoPage } from '../features/inicio/NoEncontradoPage'
import { RolesPage } from '../features/roles/RolesPage'
import { SedesPage } from '../features/sedes/SedesPage'
import { IngresarPage } from '../features/sesion/IngresarPage'
import { OrganizacionesSaasPage } from '../features/saas/OrganizacionesSaasPage'
import { RequierePlataforma, RequiereSesion } from '../features/sesion/RequiereSesion'
import { AppShell } from './layout/AppShell'

export const rutas: RouteObject[] = [
  {
    element: <AppShell />,
    children: [
      { index: true, element: <InicioPage /> },
      { path: 'ingresar', element: <IngresarPage /> },
      {
        element: <RequiereSesion />,
        children: [
          { path: 'sedes', element: <SedesPage /> },
          { path: 'roles', element: <RolesPage /> },
        ],
      },
      {
        element: <RequierePlataforma />,
        children: [{ path: 'saas/organizaciones', element: <OrganizacionesSaasPage /> }],
      },
      { path: '*', element: <NoEncontradoPage /> },
    ],
  },
]
