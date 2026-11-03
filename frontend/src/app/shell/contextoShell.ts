import { useOutletContext } from 'react-router'

export interface ContextoShell {
  abrirPerfil: () => void
  correo: string | null
}

export function useShell() {
  return useOutletContext<ContextoShell>()
}
