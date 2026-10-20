import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { rutas } from '../app/rutas'
import { I18nProvider } from '../shared/i18n/i18n'

/** Renderiza la aplicación completa en memoria, sin reintentos para pruebas deterministas. */
export function renderApp(ruta = '/') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const router = createMemoryRouter(rutas, { initialEntries: [ruta] })
  return render(
    <QueryClientProvider client={queryClient}>
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>
    </QueryClientProvider>,
  )
}

export function respuestaJson(body: unknown, status = 200, contentType = 'application/json') {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': contentType } })
}
