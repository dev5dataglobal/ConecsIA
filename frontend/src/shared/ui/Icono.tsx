/** Íconos de trazo (24×24) usados en el shell y las pantallas. Decorativos: siempre aria-hidden. */
const trazos = {
  inicio: 'M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z',
  sede: 'M4 21V5a1 1 0 0 1 1-1h9a1 1 0 0 1 1 1v16M15 9h4a1 1 0 0 1 1 1v11M3 21h18M8 8h3M8 12h3M8 16h3',
  menu: 'M4 6h16M4 12h16M4 18h16',
  cerrar: 'M6 6l12 12M18 6 6 18',
  salir: 'M15 4h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-3M10 17l5-5-5-5M15 12H3',
  mas: 'M12 5v14M5 12h14',
  editar: 'M4 20h4L19 9l-4-4L4 16zM13.5 6.5l4 4',
  apagar: 'M12 3v9M6.3 6.3a8 8 0 1 0 11.4 0',
  colapsar: 'M15 6l-6 6 6 6',
  check: 'M5 12.5l4.5 4.5L19 7.5',
  activas: 'M5 12.5l4.5 4.5L19 7.5',
  inactivas: 'M12 3v9M6.3 6.3a8 8 0 1 0 11.4 0',
  escudo: 'M12 3l7 3v5c0 4.5-3 8.3-7 10-4-1.7-7-5.5-7-10V6z M9 12l2 2 4-4',
} as const

export type NombreIcono = keyof typeof trazos

export function Icono({
  nombre,
  className = 'size-5',
}: {
  nombre: NombreIcono
  className?: string
}) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d={trazos[nombre]} />
    </svg>
  )
}
