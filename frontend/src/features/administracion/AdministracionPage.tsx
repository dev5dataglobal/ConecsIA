import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { esAdministradorGeneral, leerOrganizacion, tienePermiso } from '../sesion/sesion'
import {
  consultarOrganizaciones,
  consultarPerfiles,
  consultarUsuarios,
  crearOrganizacion,
  crearPerfil,
  crearUsuario,
  type OrganizacionAdmin,
  type PerfilAdmin,
  type UsuarioAdmin,
} from './apiAdministracion'
import { ModulosRolModal } from './ModulosRolModal'
import './administracion.css'

type Tab = 'usuarios' | 'organizaciones' | 'perfiles'
type Modal = 'usuario' | 'organizacion' | 'perfil'

/** Refleja en la UI las reglas del backend (AdministracionAuthorization); el servidor sigue siendo quien decide. */
function puede(permiso: string) {
  return esAdministradorGeneral() || tienePermiso(permiso) || tienePermiso(`saas.${permiso}`)
}

const TABS: { id: Tab; etiqueta: string; icono: NombreIcono; visible: () => boolean }[] = [
  { id: 'usuarios', etiqueta: 'Usuarios', icono: 'personas', visible: () => puede('usuarios.consultar') },
  {
    id: 'organizaciones',
    etiqueta: 'Organizaciones',
    icono: 'sede',
    visible: () => puede('organizaciones.consultar') || tienePermiso('configuracion.consultar'),
  },
  { id: 'perfiles', etiqueta: 'Perfiles', icono: 'autorizaciones', visible: () => puede('roles.consultar') },
]

const DESCRIPCION: Record<Tab, string> = {
  usuarios: 'Cuentas con acceso a la plataforma y sus perfiles por organización.',
  organizaciones: 'Organizaciones registradas dentro de tu alcance.',
  perfiles: 'Perfiles de acceso y la cantidad de permisos que agrupan.',
}

export function AdministracionPage() {
  const tabsVisibles = TABS.filter((t) => t.visible())
  const [tab, setTab] = useState<Tab>(tabsVisibles[0]?.id ?? 'usuarios')
  const [buscar, setBuscar] = useState('')
  const [organizacionSeleccionada, setOrganizacionSeleccionada] = useState<number>()
  const [modal, setModal] = useState<Modal | null>(null)
  const [perfilModulos, setPerfilModulos] = useState<PerfilAdmin | null>(null)
  const verUsuarios = puede('usuarios.consultar')
  const verOrganizaciones = TABS[1]!.visible()
  const usuarios = useQuery({ queryKey: ['admin-usuarios', buscar], queryFn: () => consultarUsuarios({ buscar }), enabled: verUsuarios })
  const organizaciones = useQuery({ queryKey: ['admin-organizaciones'], queryFn: consultarOrganizaciones, enabled: verOrganizaciones })
  // Sin permiso para listar organizaciones se usa la organización activa del token.
  const orgId = organizacionSeleccionada ?? organizaciones.data?.[0]?.idOrganizacion ?? leerOrganizacion()
  const perfiles = useQuery({
    queryKey: ['admin-perfiles', orgId],
    queryFn: () => consultarPerfiles(orgId!),
    enabled: tab === 'perfiles' && Boolean(orgId),
  })

  const accion: Record<Tab, { texto: string; modal: Modal; permitido: boolean; deshabilitado?: boolean }> = {
    usuarios: { texto: 'Nuevo usuario', modal: 'usuario', permitido: puede('usuarios.crear') },
    organizaciones: { texto: 'Nueva organización', modal: 'organizacion', permitido: puede('organizaciones.administrar') },
    perfiles: { texto: 'Nuevo perfil', modal: 'perfil', permitido: puede('roles.administrar'), deshabilitado: !orgId },
  }
  const actual = accion[tab]
  const conteo: Partial<Record<Tab, number>> = {
    usuarios: usuarios.data?.length,
    organizaciones: organizaciones.data?.length,
    perfiles: perfiles.data?.length,
  }

  if (tabsVisibles.length === 0) {
    return (
      <div className="ad">
        <Cabecera titulo="Administración" descripcion="Gestiona usuarios, organizaciones y perfiles según tus permisos." />
        <section className="ad-tarjeta">
          <Estado tipo="permiso" texto="Tu perfil no tiene permisos de administración." />
        </section>
      </div>
    )
  }

  return (
    <div className="ad">
      <Cabecera
        titulo={TABS.find((t) => t.id === tab)!.etiqueta}
        descripcion={DESCRIPCION[tab]}
        accion={
          actual.permitido && (
            <button className="ad-boton" type="button" disabled={actual.deshabilitado} onClick={() => setModal(actual.modal)}>
              <span aria-hidden="true">+</span> {actual.texto}
            </button>
          )
        }
      >
        <div className="ad-tabs" role="tablist" aria-label="Secciones de administración">
          {tabsVisibles.map((t) => (
            <button
              key={t.id}
              type="button"
              role="tab"
              id={`ad-tab-${t.id}`}
              aria-selected={tab === t.id}
              aria-controls="ad-panel"
              onClick={() => setTab(t.id)}
            >
              <Icono nombre={t.icono} />
              {t.etiqueta}
              {conteo[t.id] !== undefined && <span className="ad-tab-conteo">{conteo[t.id]}</span>}
            </button>
          ))}
        </div>
      </Cabecera>

      <section id="ad-panel" role="tabpanel" aria-labelledby={`ad-tab-${tab}`} className="ad-tarjeta">
        {tab === 'usuarios' && (
          <UsuariosPanel data={usuarios.data} loading={usuarios.isLoading} error={usuarios.error} buscar={buscar} setBuscar={setBuscar} />
        )}
        {tab === 'organizaciones' && (
          <OrganizacionesPanel data={organizaciones.data} loading={organizaciones.isLoading} error={organizaciones.error} />
        )}
        {tab === 'perfiles' && (
          <PerfilesPanel
            data={perfiles.data}
            loading={perfiles.isLoading}
            error={perfiles.error}
            organizaciones={organizaciones.data}
            orgId={orgId}
            onOrgChange={setOrganizacionSeleccionada}
            onModulos={setPerfilModulos}
          />
        )}
      </section>

      {modal && <AdminModal tipo={modal} orgId={orgId} onClose={() => setModal(null)} />}
      {perfilModulos && (
        <ModulosRolModal perfil={perfilModulos} editable={puede('roles.administrar')} onClose={() => setPerfilModulos(null)} />
      )}
    </div>
  )
}

function Cabecera({ titulo, descripcion, accion, children }: { titulo: string; descripcion: string; accion?: ReactNode; children?: ReactNode }) {
  return (
    <header className="ad-hero">
      <div className="ad-hero-fondo" aria-hidden="true" />
      <div className="ad-hero-fila">
        <div>
          <nav className="ad-migas" aria-label="Ruta de navegación">
            Inicio <span aria-hidden="true">/</span> Administración
          </nav>
          <h1>{titulo}</h1>
          <p>{descripcion}</p>
        </div>
        {accion}
      </div>
      {children}
    </header>
  )
}

function iniciales(texto: string) {
  return texto
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((parte) => parte.charAt(0).toUpperCase())
    .join('')
}

function Estatus({ estado }: { estado: string }) {
  const clave = estado.toLowerCase()
  const tono = clave.startsWith('activ') ? 'activo' : clave.startsWith('inactiv') ? 'inactivo' : 'otro'
  return <span className="ad-estatus" data-tono={tono}>{estado}</span>
}

function UsuariosPanel({ data, loading, error, buscar, setBuscar }: { data?: UsuarioAdmin[]; loading: boolean; error: unknown; buscar: string; setBuscar: (value: string) => void }) {
  return (
    <>
      <div className="ad-herramientas">
        <label className="ad-buscar">
          <Icono nombre="buscar" />
          <span className="sr-only">Buscar usuarios</span>
          <input type="search" value={buscar} onChange={(event) => setBuscar(event.target.value)} placeholder="Buscar por nombre, correo o documento" />
        </label>
      </div>
      <EstadoConsulta loading={loading} error={error} empty={!data?.length} vacio={buscar ? 'Ningún usuario coincide con la búsqueda.' : 'Aún no hay usuarios registrados.'} />
      {data && data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead>
              <tr>
                <th scope="col">Usuario</th>
                <th scope="col">Organización</th>
                <th scope="col">Perfiles</th>
                <th scope="col">Estado</th>
              </tr>
            </thead>
            <tbody>
              {data.map((usuario) => (
                <tr key={`${usuario.idUsuario}-${usuario.idMembresia}`}>
                  <td>
                    <span className="ad-persona">
                      <span className="ad-avatar" aria-hidden="true">{iniciales(usuario.nombre ?? usuario.correo)}</span>
                      <span>
                        <strong>{usuario.nombre ?? 'Sin persona vinculada'}</strong>
                        <small>{usuario.correo}{usuario.documento ? ` · ${usuario.documento}` : ''}</small>
                      </span>
                    </span>
                  </td>
                  <td>{usuario.organizacion}</td>
                  <td>
                    {usuario.perfiles.length ? (
                      <span className="ad-etiquetas">
                        {usuario.perfiles.map((perfil) => <span key={perfil} className="ad-etiqueta">{perfil}</span>)}
                      </span>
                    ) : (
                      <span className="ad-tenue">Sin perfiles</span>
                    )}
                  </td>
                  <td><Estatus estado={usuario.estado} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function OrganizacionesPanel({ data, loading, error }: { data?: OrganizacionAdmin[]; loading: boolean; error: unknown }) {
  return (
    <>
      <EstadoConsulta loading={loading} error={error} empty={!data?.length} vacio="No hay organizaciones registradas." />
      {data && data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead>
              <tr>
                <th scope="col">Organización</th>
                <th scope="col">Identificación</th>
                <th scope="col">Correo</th>
                <th scope="col">Estado</th>
              </tr>
            </thead>
            <tbody>
              {data.map((org) => (
                <tr key={org.idOrganizacion}>
                  <td>
                    <span className="ad-persona">
                      <span className="ad-avatar" data-tipo="org" aria-hidden="true"><Icono nombre="sede" /></span>
                      <span>
                        <strong>{org.nombre}</strong>
                        <small>Creada el {new Date(org.creadoEn).toLocaleDateString('es-CO')}</small>
                      </span>
                    </span>
                  </td>
                  <td>{org.identificacion ?? '—'}</td>
                  <td>{org.correo ?? '—'}</td>
                  <td><Estatus estado={org.estado} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function PerfilesPanel({ data, loading, error, organizaciones, orgId, onOrgChange, onModulos }: { data?: PerfilAdmin[]; loading: boolean; error: unknown; organizaciones?: OrganizacionAdmin[]; orgId?: number; onOrgChange: (value: number) => void; onModulos: (perfil: PerfilAdmin) => void }) {
  return (
    <>
      {organizaciones && organizaciones.length > 0 && (
        <div className="ad-herramientas">
          <label className="ad-select">
            <Icono nombre="sede" />
            <span className="sr-only">Organización</span>
            <select value={orgId ?? ''} onChange={(event) => onOrgChange(Number(event.target.value))}>
              {organizaciones.map((org) => <option key={org.idOrganizacion} value={org.idOrganizacion}>{org.nombre}</option>)}
            </select>
          </label>
        </div>
      )}
      {!orgId && !loading ? (
        <Estado tipo="vacio" texto="Selecciona una organización para ver sus perfiles." />
      ) : (
        <EstadoConsulta loading={loading} error={error} empty={!data?.length} vacio="Esta organización aún no tiene perfiles." />
      )}
      {data && data.length > 0 && (
        <div className="ad-perfiles">
          {data.map((perfil) => (
            <article className="ad-perfil" key={perfil.idRol}>
              <header>
                <span className="ad-perfil-icono" aria-hidden="true"><Icono nombre="autorizaciones" /></span>
                <Estatus estado={perfil.activo ? 'Activo' : 'Inactivo'} />
              </header>
              <h2>{perfil.nombre}</h2>
              <p>{perfil.descripcion ?? 'Sin descripción'}</p>
              <footer>
                <span>
                  <strong>{perfil.permisos.length}</strong> {perfil.permisos.length === 1 ? 'permiso configurado' : 'permisos configurados'}
                </span>
                <button type="button" className="ad-enlace" onClick={() => onModulos(perfil)}>
                  <Icono nombre="dashboard" /> Módulos
                </button>
              </footer>
            </article>
          ))}
        </div>
      )}
    </>
  )
}

function EstadoConsulta({ loading, error, empty, vacio }: { loading: boolean; error: unknown; empty: boolean; vacio: string }) {
  if (loading) return <div className="ad-cargando" aria-busy="true" aria-label="Cargando"><div /><div /><div /></div>
  if (error) {
    const status = error instanceof ApiError ? error.status : 0
    if (status === 403) return <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar esta información." />
    const detalle = error instanceof ApiError ? error.problem.detail : undefined
    return <Estado tipo="error" texto={detalle ?? 'No se pudo cargar la información. Inténtalo de nuevo.'} />
  }
  return empty ? <Estado tipo="vacio" texto={vacio} /> : null
}

function Estado({ tipo, texto }: { tipo: 'vacio' | 'error' | 'permiso'; texto: string }) {
  return (
    <div className="ad-estado" data-tipo={tipo} role={tipo === 'vacio' ? undefined : 'alert'}>
      <span><Icono nombre={tipo === 'vacio' ? 'vacio' : tipo === 'permiso' ? 'autorizaciones' : 'alertas'} /></span>
      <p>{texto}</p>
    </div>
  )
}

function AdminModal({ tipo, orgId, onClose }: { tipo: Modal; orgId?: number; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [nombre, setNombre] = useState('')
  const [correo, setCorreo] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [mensaje, setMensaje] = useState('')
  const alFallar = (error: unknown) => setMensaje(error instanceof ApiError ? error.problem.detail ?? 'No se pudo guardar.' : 'No se pudo guardar.')
  const orgMutation = useMutation({
    mutationFn: () => crearOrganizacion({ nombre, idTipoOrganizacion: 1, idPlan: 1, estado: 'Activa' }),
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['admin-organizaciones'] }); onClose() },
    onError: alFallar,
  })
  const userMutation = useMutation({
    mutationFn: () => crearUsuario({ correo, contrasena, idOrganizacion: orgId, idRoles: [] }),
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['admin-usuarios'] }); onClose() },
    onError: alFallar,
  })
  const perfilMutation = useMutation({
    mutationFn: () => crearPerfil(orgId!, { nombre, idPermisos: [], activo: true }),
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['admin-perfiles', orgId] }); onClose() },
    onError: alFallar,
  })
  const pendiente = orgMutation.isPending || userMutation.isPending || perfilMutation.isPending

  useEffect(() => {
    const alPresionarTecla = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose() }
    document.addEventListener('keydown', alPresionarTecla)
    return () => document.removeEventListener('keydown', alPresionarTecla)
  }, [onClose])

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (pendiente) return
    setMensaje('')
    if (tipo === 'organizacion') orgMutation.mutate()
    else if (tipo === 'usuario') userMutation.mutate()
    else if (orgId) perfilMutation.mutate()
  }
  const titulo = tipo === 'usuario' ? 'Nuevo usuario' : tipo === 'organizacion' ? 'Nueva organización' : 'Nuevo perfil'

  return (
    <div className="ad-velo" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
      <form className="ad-modal" onSubmit={submit} role="dialog" aria-modal="true" aria-labelledby="ad-modal-titulo">
        <header>
          <span className="ad-modal-icono" aria-hidden="true">
            <Icono nombre={tipo === 'usuario' ? 'personas' : tipo === 'organizacion' ? 'sede' : 'autorizaciones'} />
          </span>
          <div>
            <span className="ad-modal-eyebrow">Administración</span>
            <h2 id="ad-modal-titulo">{titulo}</h2>
          </div>
          <button type="button" className="ad-cerrar" onClick={onClose} aria-label="Cerrar"><Icono nombre="cerrar" /></button>
        </header>
        {tipo === 'usuario' && (
          <>
            <label className="ad-campo">Correo electrónico<input type="email" required autoFocus autoComplete="off" value={correo} onChange={(event) => setCorreo(event.target.value)} /></label>
            <label className="ad-campo">Contraseña inicial<input type="password" required minLength={8} autoComplete="new-password" value={contrasena} onChange={(event) => setContrasena(event.target.value)} /></label>
          </>
        )}
        {tipo !== 'usuario' && (
          <label className="ad-campo">Nombre<input required autoFocus value={nombre} onChange={(event) => setNombre(event.target.value)} /></label>
        )}
        {mensaje && <p className="ad-mensaje" role="alert">{mensaje}</p>}
        <footer>
          <button type="button" className="ad-boton-sec" onClick={onClose}>Cancelar</button>
          <button className="ad-boton" type="submit" disabled={pendiente}>{pendiente ? 'Guardando…' : 'Guardar'}</button>
        </footer>
      </form>
    </div>
  )
}
