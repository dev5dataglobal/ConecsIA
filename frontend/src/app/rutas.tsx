import type { RouteObject } from 'react-router'
import { InicioPage } from '../features/inicio/InicioPage'
import { NoEncontradoPage } from '../features/inicio/NoEncontradoPage'
import { SedesPage } from '../features/sedes/SedesPage'
import { IngresarPage } from '../features/sesion/IngresarPage'
import { RequiereSesion } from '../features/sesion/RequiereSesion'
import { AppShell } from './layout/AppShell'

export const rutas: RouteObject[] = [
  // El ingreso ocupa la pantalla completa, fuera del AppShell.
  { path: 'ingresar', element: <IngresarPage /> },
  {
    element: <AppShell />,
    children: [
      { index: true, element: <InicioPage /> },
      {
        element: <RequiereSesion />,
        children: [{ path: 'sedes', element: <SedesPage /> }],
      },
      { path: '*', element: <NoEncontradoPage /> },
    ],
  },
]
