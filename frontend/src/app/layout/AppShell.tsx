import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { cerrarSesion as cerrarSesionApi } from '../../features/sesion/apiSesion'
import { guardarOrganizacionSeleccionada } from '../../features/sesion/organizacionSeleccionada'
import { useContexto, useMembresias } from '../../features/sesion/useContexto'
import { IndicadorConexion } from '../../features/sistema/IndicadorConexion'
import type { ClaveTexto } from '../../shared/i18n/es'
import { useT } from '../../shared/i18n/traductor'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'

/** Solo módulos implementados: el menú no ofrece pantallas que no existen. */
const enlaces: { to: string; clave: ClaveTexto; icono: NombreIcono }[] = [
  { to: '/', clave: 'nav.inicio', icono: 'inicio' },
  { to: '/sedes', clave: 'nav.sedes', icono: 'sede' },
]

function iniciales(nombre: string) {
  return nombre
    .replace(/\(.*\)/, '')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join('')
}

/**
 * Estructura base según el mockup de Figma: barra lateral oscura, cabecera clara con la
 * organización derivada por el servidor (HU-007) y el usuario. Con varias membresías (D-03b) la
 * cabecera ofrece un selector; el servidor verifica la elección en cada petición.
 */
export function AppShell() {
  const t = useT()
  const [menuAbierto, setMenuAbierto] = useState(false)
  const [colapsado, setColapsado] = useState(false)
  const { data: contexto } = useContexto()
  const { data: membresias } = useMembresias(contexto !== undefined)
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  function cambiarOrganizacion(organizacionId: string) {
    guardarOrganizacionSeleccionada(organizacionId)
    void queryClient.invalidateQueries()
  }

  async function cerrarSesion() {
    await cerrarSesionApi()
    queryClient.clear()
    void navigate('/ingresar')
  }

  const navegacion = (compacto: boolean) => (
    <ul className="flex flex-col gap-1">
      {enlaces.map((enlace) => (
        <li key={enlace.to}>
          <NavLink
            to={enlace.to}
            end
            title={compacto ? t(enlace.clave) : undefined}
            onClick={() => setMenuAbierto(false)}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-acento ${
                isActive
                  ? 'bg-marca text-white'
                  : 'text-white/75 hover:bg-white/10 hover:text-white'
              } ${compacto ? 'justify-center' : ''}`
            }
          >
            <Icono nombre={enlace.icono} className="size-5 shrink-0" />
            <span className={compacto ? 'sr-only' : ''}>{t(enlace.clave)}</span>
          </NavLink>
        </li>
      ))}
    </ul>
  )

  const marca = (compacto: boolean) => (
    <div className={`flex items-center gap-3 px-2 ${compacto ? 'justify-center' : ''}`}>
      <span className="grid size-9 shrink-0 place-items-center rounded-full bg-marca text-white">
        <Icono nombre="escudo" className="size-5" />
      </span>
      {!compacto && (
        <span className="leading-tight">
          <span className="block text-sm font-semibold text-white">{t('app.nombre')}</span>
          <span className="block text-xs text-white/60">{t('app.lema')}</span>
        </span>
      )}
    </div>
  )

  return (
    <div className="min-h-dvh bg-superficie md:flex">
      <a
        href="#contenido"
        className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded focus:bg-white focus:px-3 focus:py-2"
      >
        {t('app.saltarContenido')}
      </a>

      {/* Barra lateral (escritorio) */}
      <aside
        className={`sticky top-0 hidden h-dvh shrink-0 flex-col justify-between bg-tinta p-3 md:flex ${
          colapsado ? 'w-[72px]' : 'w-60'
        }`}
      >
        <div className="space-y-6">
          <div className="pt-2">{marca(colapsado)}</div>
          <nav aria-label={t('nav.principal')}>{navegacion(colapsado)}</nav>
        </div>
        <button
          type="button"
          onClick={() => setColapsado((c) => !c)}
          aria-expanded={!colapsado}
          className={`flex items-center gap-3 rounded-lg px-3 py-2 text-xs text-white/60 hover:bg-white/10 hover:text-white ${
            colapsado ? 'justify-center' : ''
          }`}
        >
          <Icono nombre="colapsar" className={`size-4 ${colapsado ? 'rotate-180' : ''}`} />
          <span className={colapsado ? 'sr-only' : ''}>{t('nav.colapsar')}</span>
        </button>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 border-b border-linea bg-fondo">
          <div className="flex items-center justify-between gap-3 px-4 py-3 md:px-6">
            <div className="flex items-center gap-3">
              <button
                type="button"
                className="rounded-lg p-2 text-tinta hover:bg-superficie md:hidden"
                aria-expanded={menuAbierto}
                aria-controls="menu-movil"
                aria-label={menuAbierto ? t('nav.cerrarMenu') : t('nav.abrirMenu')}
                onClick={() => setMenuAbierto((abierto) => !abierto)}
              >
                <Icono nombre={menuAbierto ? 'cerrar' : 'menu'} />
              </button>
              <span className="text-base font-semibold md:hidden">{t('app.nombre')}</span>
            </div>

            <div className="flex min-w-0 items-center gap-3">
              <IndicadorConexion />
              {contexto ? (
                <>
                  <div
                    className="hidden min-w-0 items-center gap-2 rounded-lg border border-linea bg-fondo px-3 py-1.5 sm:flex"
                    aria-label={t('contexto.organizacion')}
                  >
                    <Icono nombre="sede" className="size-4 shrink-0 text-marca" />
                    <span className="min-w-0 leading-tight">
                      <span className="block text-[10px] font-medium uppercase tracking-wide text-tinta-suave">
                        {t('contexto.organizacion')}
                      </span>
                      {membresias && membresias.length > 1 ? (
                        <select
                          aria-label={t('contexto.organizacion')}
                          value={contexto.organizacion.id}
                          onChange={(ev) => cambiarOrganizacion(ev.target.value)}
                          className="block max-w-48 truncate bg-transparent text-xs font-semibold focus-visible:outline-2 focus-visible:outline-acento"
                        >
                          {membresias.map((m) => (
                            <option key={m.organizacionId} value={m.organizacionId}>
                              {m.nombre}
                            </option>
                          ))}
                        </select>
                      ) : (
                        <span className="block truncate text-xs font-semibold">
                          {contexto.organizacion.nombre}
                        </span>
                      )}
                    </span>
                  </div>
                  <div
                    className="flex min-w-0 items-center gap-2"
                    aria-label={t('contexto.usuario')}
                  >
                    <span
                      aria-hidden="true"
                      className="grid size-9 shrink-0 place-items-center rounded-full bg-marca text-xs font-semibold text-white"
                    >
                      {iniciales(contexto.usuario.nombre)}
                    </span>
                    <span className="hidden min-w-0 leading-tight lg:block">
                      <span className="block truncate text-sm font-semibold">
                        {contexto.usuario.nombre}
                      </span>
                      <span className="block truncate text-xs text-tinta-suave">
                        {contexto.usuario.correo}
                      </span>
                    </span>
                  </div>
                  <button
                    type="button"
                    onClick={() => void cerrarSesion()}
                    className="flex shrink-0 items-center gap-1.5 rounded-lg px-2 py-1.5 text-xs font-medium text-tinta-suave hover:bg-superficie hover:text-tinta"
                  >
                    <Icono nombre="salir" className="size-4" />
                    {t('sesion.cerrar')}
                  </button>
                </>
              ) : (
                <div
                  className="hidden truncate rounded-lg border border-dashed border-gris px-3 py-1.5 text-xs text-tinta-suave sm:block"
                  aria-label={`${t('contexto.organizacion')} / ${t('contexto.sede')}`}
                >
                  {t('contexto.sinSesion')}
                </div>
              )}
            </div>
          </div>

          {menuAbierto && (
            <div id="menu-movil" className="space-y-4 bg-tinta p-3 md:hidden">
              {marca(false)}
              <nav aria-label={t('nav.principal')}>{navegacion(false)}</nav>
            </div>
          )}
        </header>

        <main id="contenido" className="min-w-0 flex-1 px-4 py-6 md:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
