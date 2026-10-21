import { useMemo, type ReactNode } from 'react'
import { crearTraductor, I18nContext, type Vocabulario } from './traductor'

export function I18nProvider({
  vocabulario,
  children,
}: {
  vocabulario?: Vocabulario
  children: ReactNode
}) {
  const t = useMemo(() => crearTraductor(vocabulario), [vocabulario])
  return <I18nContext.Provider value={t}>{children}</I18nContext.Provider>
}
