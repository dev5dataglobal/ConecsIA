import type { RouteObject } from 'react-router'
import { IngresarPage } from '../features/sesion/IngresarPage'

export const rutas: RouteObject[] = [
  { index: true, element: <IngresarPage /> },
  { path: 'ingresar', element: <IngresarPage /> },
  // La pantalla posterior al ingreso pertenece al resto de la aplicación y no se incluye aquí.
  { path: '*', element: null },
]
