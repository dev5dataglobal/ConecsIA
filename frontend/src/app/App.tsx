import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState } from 'react'
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
  return (
    <QueryClientProvider client={queryClient}>
      <I18nProvider>
        <RouterProvider router={router} />
      </I18nProvider>
    </QueryClientProvider>
  )
}
