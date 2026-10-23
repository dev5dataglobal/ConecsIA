/** Iconos de trazo (24×24, currentColor) compartidos por el shell y las páginas. */
const TRAZOS = {
  dashboard: 'M4 4h7v7H4zM13 4h7v4h-7zM13 10h7v10h-7zM4 13h7v7H4z',
  personas: 'M16 20v-1.5a3.5 3.5 0 0 0-3.5-3.5h-5A3.5 3.5 0 0 0 4 18.5V20M10 11a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM20 20v-1.5a3.5 3.5 0 0 0-2.5-3.35M15.5 4.15a3.5 3.5 0 0 1 0 6.7',
  autorizaciones: 'M12 3 5 6v5c0 4.5 3 8 7 9.5 4-1.5 7-5 7-9.5V6l-7-3ZM9 12l2 2 4-4',
  visitas: 'M8 3v3M16 3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1ZM9 14l2 2 4-4',
  accesos: 'M14 4h4a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-4M9 16l4-4-4-4M13 12H3',
  vehiculos: 'M5 16h14M6 16v2M18 16v2M4 16v-3l2-5h12l2 5v3M7.5 13h.01M16.5 13h.01',
  dispositivos: 'M4 5h16v10H4zM9 19h6M12 15v4',
  alertas: 'M12 4 3 19h18L12 4ZM12 10v4M12 17h.01',
  reportes: 'M5 20V10M12 20V4M19 20v-7',
  configuracion: 'M4 7h10M18 7h2M4 17h4M12 17h8M14 5v4M8 15v4',
  administracion: 'M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM19 12a7 7 0 0 0-.1-1.2l2-1.5-2-3.4-2.3.9a7 7 0 0 0-2.1-1.2L14 3h-4l-.5 2.6a7 7 0 0 0-2.1 1.2l-2.3-.9-2 3.4 2 1.5a7 7 0 0 0 0 2.4l-2 1.5 2 3.4 2.3-.9a7 7 0 0 0 2.1 1.2L10 21h4l.5-2.6a7 7 0 0 0 2.1-1.2l2.3.9 2-3.4-2-1.5c.1-.4.1-.8.1-1.2Z',
  menu: 'M4 7h16M4 12h16M4 17h16',
  cerrar: 'M6 6l12 12M18 6 6 18',
  colapsar: 'M15 6l-6 6 6 6',
  perfil: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM5 20a7 7 0 0 1 14 0',
  salir: 'M10 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4M15 16l4-4-4-4M19 12H9',
  actualizar: 'M20 11a8 8 0 0 0-14.6-4.5L4 8M4 4v4h4M4 13a8 8 0 0 0 14.6 4.5L20 16M20 20v-4h-4',
  entrada: 'M7 7l10 10M17 9v8H9',
  salida: 'M17 17 7 7M7 15V7h8',
  sede: 'M4 20V8l8-4 8 4v12M9 20v-6h6v6M3 20h18',
  buscar: 'M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14ZM20 20l-4-4',
  flecha: 'M5 12h14M13 6l6 6-6 6',
  vacio: 'M4 13h4l2 3h4l2-3h4M5.5 6.5 4 13v5a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-5l-1.5-6.5A1 1 0 0 0 17.5 6h-11a1 1 0 0 0-1 .5Z',
} as const

export type NombreIcono = keyof typeof TRAZOS

export function Icono({ nombre, className }: { nombre: NombreIcono; className?: string }) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      width="1em"
      height="1em"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={TRAZOS[nombre]} />
    </svg>
  )
}
