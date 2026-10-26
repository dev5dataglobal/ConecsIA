import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { RouterProvider, type createBrowserRouter } from 'react-router'
import { I18nProvider } from '../shared/i18n/i18n'

function crearQueryClient() {
  return new QueryClient({
    defaultOptions: {
      // Las mutaciones nunca se reintentan solas: un reintento podría duplicar operaciones.
      mutations: { retry: false },
      queries: { staleTime: 15_000, refetchOnWindowFocus: true },
    },
  })
}

export function App({ router }: { router: ReturnType<typeof createBrowserRouter> }) {
  const [queryClient] = useState(crearQueryClient)
  useEffect(() => {
    const onSesionExpirada = () => {
      if (window.location.pathname !== '/ingresar') window.location.assign('/ingresar')
    }
    window.addEventListener('conecsia:sesion-expirada', onSesionExpirada)
    return () => window.removeEventListener('conecsia:sesion-expirada', onSesionExpirada)
  }, [])

  return (
    <QueryClientProvider client={queryClient}>
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>
    </QueryClientProvider>
  )
}
