import { Navigate, NavLink } from 'react-router'
import { Icono } from '../../shared/ui/Icono'
import { useMisModulos } from '../modulos/apiModulos'
import { Cabecera, Estado } from './componentes'
import { OrganizacionesSeccion } from './OrganizacionesSeccion'
import { PerfilesSeccion } from './PerfilesSeccion'
import { UsuariosSeccion } from './UsuariosSeccion'
import { ZonasSeccion } from './ZonasSeccion'
import { SECCIONES, type Seccion } from './secciones'
import './administracion.css'



export function AdministracionPage({ seccion }: { seccion: Seccion }) {
  const modulos = useMisModulos()
  const visibles = SECCIONES.filter((s) => modulos.data?.some((m) => m.codigo === s.codigo))
  const pestanas = (
    <nav className="ad-tabs" aria-label="Secciones de administración">
      {visibles.map((s) => (
        <NavLink key={s.id} to={`/administracion/${s.id}`} aria-current={s.id === seccion ? 'page' : undefined}>
          <Icono nombre={s.icono} />
          {s.etiqueta}
        </NavLink>
      ))}
    </nav>
  )
  const Componente = { usuarios: UsuariosSeccion, perfiles: PerfilesSeccion, organizaciones: OrganizacionesSeccion, 'zonas-empresariales': ZonasSeccion }[seccion]
  return <Componente pestanas={pestanas} />
}

/** /administracion → primera sección permitida. */
export function AdministracionInicio() {
  const modulos = useMisModulos()
  if (modulos.isLoading) return <div className="sh-acceso" aria-busy="true" aria-label="Verificando acceso" />
  const primera = SECCIONES.find((s) => modulos.data?.some((m) => m.codigo === s.codigo))
  if (primera) return <Navigate to={`/administracion/${primera.id}`} replace />
  return (
    <div className="ad">
      <Cabecera titulo="Administración" descripcion="Gestiona usuarios, perfiles, organizaciones y zonas empresariales según tus permisos." />
      <section className="ad-tarjeta">
        <Estado tipo="permiso" texto="Tu perfil no tiene permisos de administración." />
      </section>
    </div>
  )
}
