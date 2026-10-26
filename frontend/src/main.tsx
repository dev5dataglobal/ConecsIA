import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter } from 'react-router'
import { App } from './app/App'
import { rutas } from './app/rutas'
import { restaurarSesion } from './features/sesion/apiSesion'
import './styles.css'

const root = document.getElementById('root')
if (!root) throw new Error('No se encontró el elemento #root')

// La sesión se restaura (cookie HttpOnly) antes de evaluar las rutas protegidas.
void restaurarSesion().finally(() =>
  createRoot(root).render(
    <StrictMode>
      <App router={createBrowserRouter(rutas)} />
    </StrictMode>,
  ),
)
