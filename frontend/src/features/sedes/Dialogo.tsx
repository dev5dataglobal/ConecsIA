import { useEffect, useId, useRef, type ReactNode } from 'react'

/**
 * Diálogo modal accesible con el elemento nativo: foco atrapado, Escape para cerrar y título
 * asociado. Se usa abierto con showModal cuando el navegador lo soporta.
 */
export function Dialogo({
  titulo,
  onCerrar,
  children,
}: {
  titulo: string
  onCerrar: () => void
  children: ReactNode
}) {
  const ref = useRef<HTMLDialogElement>(null)
  const idTitulo = useId()

  useEffect(() => {
    const dialogo = ref.current
    if (dialogo && typeof dialogo.showModal === 'function' && !dialogo.open) dialogo.showModal()
  }, [])

  return (
    <dialog
      ref={ref}
      open={
        typeof HTMLDialogElement === 'undefined' || !('showModal' in HTMLDialogElement.prototype)
          ? true
          : undefined
      }
      aria-labelledby={idTitulo}
      onCancel={(evento) => {
        evento.preventDefault()
        onCerrar()
      }}
      className="m-auto w-[min(32rem,calc(100vw-2rem))] rounded-lg border border-linea p-0 shadow-xl backdrop:bg-tinta/50"
    >
      <div className="space-y-4 p-5">
        <h2 id={idTitulo} className="text-lg font-semibold">
          {titulo}
        </h2>
        {children}
      </div>
    </dialog>
  )
}
