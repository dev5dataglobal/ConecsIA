import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useReducer, useRef, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { cerrarSesion } from '../../features/sesion/apiSesion'
import { useMisModulos } from '../../features/modulos/apiModulos'
import { consultarOrganizaciones } from '../../features/administracion/apiAdministracion'
import { esAdministradorGeneral, leerCorreo, leerOrganizacion, leerOrganizacionSeleccionada, seleccionarOrganizacion } from '../../features/sesion/sesion'
import { PerfilModal } from '../../features/usuario/PerfilModal'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import type { ContextoShell } from './contextoShell'
import { construirNavegacion } from './navegacion'
import '../../features/usuario/usuario.css'
import '../../features/usuario/usuarioMenu.css'
import './appShell.css'

const CLAVE_COLAPSADO = 'conecsia.menuColapsado'

function leerColapsado() {
  try {
    return localStorage.getItem(CLAVE_COLAPSADO) === '1'
  } catch {
    return false
  }
}

export function AppShell() {
  const [colapsado, setColapsado] = useState(leerColapsado)
  const [drawerAbierto, setDrawerAbierto] = useState(false)
  const [menuUsuarioAbierto, setMenuUsuarioAbierto] = useState(false)
  const [perfilAbierto, setPerfilAbierto] = useState(false)
  const [confirmarCierre, setConfirmarCierre] = useState(false)
  const menuUsuarioRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const correo = leerCorreo()
  const modulos = useMisModulos()
  const grupos = construirNavegacion(modulos.data ?? [])

  useEffect(() => {
    function alHacerClickFuera(event: MouseEvent) {
      if (menuUsuarioRef.current && !menuUsuarioRef.current.contains(event.target as Node)) setMenuUsuarioAbierto(false)
    }
    function alPresionarTecla(event: KeyboardEvent) {
      if (event.key !== 'Escape') return
      setDrawerAbierto(false)
      setMenuUsuarioAbierto(false)
    }
    document.addEventListener('mousedown', alHacerClickFuera)
    document.addEventListener('keydown', alPresionarTecla)
    return () => {
      document.removeEventListener('mousedown', alHacerClickFuera)
      document.removeEventListener('keydown', alPresionarTecla)
    }
  }, [])

  function alternarColapsado() {
    setColapsado((valor) => {
      try {
        localStorage.setItem(CLAVE_COLAPSADO, valor ? '0' : '1')
      } catch {
        // Preferencia solo de presentación; si no se puede guardar se usa en memoria.
      }
      return !valor
    })
  }

  async function confirmarCerrarSesion() {
    // Se espera la revocación en el servidor antes de salir de la página.
    await cerrarSesion()
    // Nada del usuario anterior (módulos, acciones, listados) debe sobrevivir a la siguiente sesión.
    queryClient.clear()
    setConfirmarCierre(false)
    setMenuUsuarioAbierto(false)
    void navigate('/ingresar', { replace: true })
    window.history.replaceState(null, '', '/ingresar')
  }

  const contexto: ContextoShell = { abrirPerfil: () => setPerfilAbierto(true), correo }
  const inicial = (correo ?? 'U').charAt(0).toUpperCase()

  return (
    <div className="sh" data-colapsado={colapsado || undefined} data-drawer={drawerAbierto || undefined}>
      <a className="sh-saltar" href="#contenido">
        Saltar al contenido
      </a>

      <aside className="sh-lateral" aria-label="Menú lateral">
        <div className="sh-lateral-fondo" aria-hidden="true" />
        <div className="sh-marca">
          <span className="sh-marca-icono" aria-hidden="true">
            <Icono nombre="autorizaciones" />
          </span>
          <span className="sh-marca-texto">
            <strong>ConecsIA</strong>
            <small>Visitantes y control de accesos</small>
          </span>
          <button type="button" className="sh-cerrar-drawer" onClick={() => setDrawerAbierto(false)} aria-label="Cerrar menú">
            <Icono nombre="cerrar" />
          </button>
        </div>

        <nav className="sh-nav" aria-label="Navegación principal" aria-busy={modulos.isLoading}>
          {modulos.isLoading && <div className="sh-nav-cargando" aria-label="Cargando menú"><span /><span /><span /></div>}
          {modulos.isError && <p className="sh-nav-error">No se pudo cargar el menú.</p>}
          {grupos.map((grupo) => (
            <div key={grupo.titulo} className="sh-grupo">
              <span className="sh-grupo-titulo">{grupo.titulo}</span>
              <ul>
                {grupo.items.map((item) => (
                  <li key={item.codigo}>
                    {item.ruta ? (
                      <NavLink to={item.ruta} className="sh-item" title={colapsado ? item.etiqueta : undefined} onClick={() => setDrawerAbierto(false)}>
                        <Icono nombre={item.icono} className="sh-item-icono" />
                        <span className="sh-item-texto">{item.etiqueta}</span>
                      </NavLink>
                    ) : (
                      <span
                        className="sh-item"
                        aria-disabled="true"
                        title={`${item.etiqueta}: módulo aún no disponible`}
                      >
                        <Icono nombre={item.icono} className="sh-item-icono" />
                        <span className="sh-item-texto">{item.etiqueta}</span>
                        <span className="sh-pronto">Pronto</span>
                      </span>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </nav>

        <button
          type="button"
          className="sh-colapsar"
          onClick={alternarColapsado}
          aria-pressed={colapsado}
          aria-label={colapsado ? 'Expandir menú' : 'Colapsar menú'}
        >
          <Icono nombre="colapsar" className="sh-colapsar-icono" />
          <span className="sh-item-texto">Colapsar menú</span>
        </button>
      </aside>

      <div className="sh-velo" aria-hidden="true" onClick={() => setDrawerAbierto(false)} />

      <div className="sh-principal">
        <header className="sh-barra">
          <button
            type="button"
            className="sh-boton-icono sh-abrir-drawer"
            onClick={() => setDrawerAbierto(true)}
            aria-label="Abrir menú"
            aria-expanded={drawerAbierto}
          >
            <Icono nombre="menu" />
          </button>
          <span className="sh-barra-marca">ConecsIA</span>
          {esAdministradorGeneral() && <SelectorOrganizacion />}

          <div className="sh-usuario" ref={menuUsuarioRef}>
            <button
              type="button"
              className="sh-usuario-boton"
              aria-label="Abrir menú de usuario"
              aria-haspopup="menu"
              aria-expanded={menuUsuarioAbierto}
              onClick={() => setMenuUsuarioAbierto((valor) => !valor)}
            >
              <span className="sh-avatar" aria-hidden="true">
                {inicial}
              </span>
              {correo && <span className="sh-usuario-correo">{correo}</span>}
            </button>
            {menuUsuarioAbierto && (
              <div className="user-menu" role="menu">
                <button
                  type="button"
                  role="menuitem"
                  onClick={() => {
                    setPerfilAbierto(true)
                    setMenuUsuarioAbierto(false)
                  }}
                >
                  <Icono nombre="perfil" /> <span>Ver perfil</span>
                </button>
                <button type="button" role="menuitem" onClick={() => setConfirmarCierre(true)}>
                  <Icono nombre="salir" /> <span>Cerrar sesión</span>
                </button>
              </div>
            )}
          </div>
        </header>

        <main id="contenido" className="sh-contenido" tabIndex={-1}>
          <Outlet context={contexto} />
        </main>
      </div>

      {perfilAbierto && <PerfilModal onClose={() => setPerfilAbierto(false)} />}
      {confirmarCierre && (
        <div
          className="logout-overlay"
          role="presentation"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) setConfirmarCierre(false)
          }}
        >
          <section className="logout-dialog" role="alertdialog" aria-modal="true" aria-labelledby="logout-title">
            <div className="logout-icon">
              <Icono nombre="salir" />
            </div>
            <h2 id="logout-title">¿Cerrar sesión?</h2>
            <p>Se cerrará tu sesión actual y tendrás que ingresar nuevamente para continuar.</p>
            <div className="logout-actions">
              <button type="button" className="logout-cancel" onClick={() => setConfirmarCierre(false)} autoFocus>
                Cancelar
              </button>
              <button type="button" className="logout-confirm" onClick={() => void confirmarCerrarSesion()}>
                Cerrar sesión
              </button>
            </div>
          </section>
        </div>
      )}
    </div>
  )
}

/**
 * Solo el administrador de plataforma cambia de organización. La selección viaja en cada petición
 * (cabecera validada por el servidor) y, al cambiarla, se descartan los datos en caché de la anterior.
 */
function SelectorOrganizacion() {
  const queryClient = useQueryClient()
  const [, refrescar] = useReducer((n: number) => n + 1, 0)
  const seleccion = leerOrganizacionSeleccionada() ?? leerOrganizacion()
  const organizaciones = useQuery({
    queryKey: ['organizaciones-selector'],
    queryFn: () => consultarOrganizaciones({ estado: 'Activa' }),
    staleTime: 60_000,
  })

  // Si la organización elegida dejó de estar activa, se vuelve a la propia.
  useEffect(() => {
    const lista = organizaciones.data
    if (lista && seleccion !== undefined && !lista.some((o) => o.idOrganizacion === seleccion) && leerOrganizacionSeleccionada() !== undefined) {
      seleccionarOrganizacion(undefined)
      void queryClient.resetQueries()
    }
  }, [organizaciones.data, seleccion, queryClient])

  function cambiar(id: number) {
    seleccionarOrganizacion(id === leerOrganizacion() ? undefined : id)
    refrescar()
    // Evita mostrar datos de la organización anterior: se descartan y se vuelven a consultar.
    void queryClient.resetQueries()
  }

  if (!organizaciones.data?.length) return null
  return (
    <Selector
      className="sel-shell"
      etiqueta="Organización activa"
      icono={<Icono nombre="sede" />}
      valor={String(seleccion ?? '')}
      onChange={(v) => cambiar(Number(v))}
      opciones={organizaciones.data.map((o) => ({ valor: String(o.idOrganizacion), texto: o.nombre }))}
    />
  )
}
