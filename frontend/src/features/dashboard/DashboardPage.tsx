import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { consultarDashboard, type PeriodoDashboard } from './apiDashboard'
import './dashboard.css'

const formatoHora = new Intl.DateTimeFormat('es-CO', { hour: '2-digit', minute: '2-digit' })
const formatoDia = new Intl.DateTimeFormat('es-CO', { weekday: 'short' })

function Icon({ children }: { children: ReactNode }) { return <span className="dashboard-icon" aria-hidden="true">{children}</span> }

export function DashboardPage() {
  const [periodo, setPeriodo] = useState<PeriodoDashboard>('7d')
  const [sedeId, setSedeId] = useState<number | undefined>()
  const [menuAbierto, setMenuAbierto] = useState(false)
  const dashboard = useQuery({ queryKey: ['dashboard', periodo, sedeId], queryFn: () => consultarDashboard(periodo, sedeId) })

  const datos = dashboard.data
  const maxVisitas = Math.max(...(datos?.visitasPorDia.map((item) => item.total) ?? [0]), 1)
  const totalDistribucion = datos?.distribucionPorTipo.reduce((total, item) => total + item.total, 0) ?? 0
  const donut = useMemo(() => {
    if (!datos || totalDistribucion === 0) return 'conic-gradient(#ececed 0 100%)'
    let start = 0
    const colors = ['#6953da', '#9c8be7', '#d5f379', '#b9b9b9']
    const stops = datos.distribucionPorTipo.map((item, index) => {
      const end = start + (item.total / totalDistribucion) * 100
      const stop = `${colors[index % colors.length]} ${start}% ${end}%`
      start = end
      return stop
    })
    return `conic-gradient(${stops.join(', ')})`
  }, [datos, totalDistribucion])

  return (
    <div className="dashboard-shell">
      <aside className={`dashboard-sidebar ${menuAbierto ? 'is-open' : ''}`}>
        <div className="dashboard-brand"><span className="dashboard-brand-mark">✦</span><span><strong>ConecsIA</strong><small>Visitantes y control de accesos</small></span></div>
        <nav aria-label="Navegación principal">
          {['Dashboard', 'Personas', 'Autorizaciones', 'Visitas', 'Control de accesos', 'Vehículos', 'Dispositivos', 'Alertas', 'Reportes', 'Configuración'].map((item, index) => <a key={item} className={index === 0 ? 'active' : ''} href={index === 0 ? '/dashboard' : '#'} onClick={(event) => { if (index !== 0) event.preventDefault() }}><Icon>{['⌂', '♙', '▣', '◷', '⌁', '▱', '▥', '△', '▤', '⚙'][index]}</Icon>{item}{item === 'Alertas' && <span className="nav-badge">{datos?.resumen.alertasPendientes ?? 0}</span>}</a>)}
        </nav>
        <button className="sidebar-collapse" type="button">↔ <span>Colapsar menú</span></button>
      </aside>

      <main className="dashboard-main">
        <header className="dashboard-topbar">
          <button className="mobile-menu" type="button" onClick={() => setMenuAbierto((value) => !value)} aria-label="Abrir menú">☰</button>
          <label className="dashboard-search"><span aria-hidden="true">⌕</span><input placeholder="Buscar persona, documento o visita..." aria-label="Buscar" /></label>
          <div className="dashboard-top-actions"><button className="top-selector">▣ <span>Organización<br /><strong>{datos?.organizacion.nombre ?? 'ConecsIA'}</strong></span>⌄</button><button className="top-selector">⌑ <span>Sede<br /><strong>{sedeId ? datos?.sedes.find((sede) => sede.idSede === sedeId)?.nombre : 'Todas las sedes'}</strong></span>⌄</button><button className="notification-button" aria-label="Notificaciones">♧</button><span className="user-avatar">U</span></div>
        </header>

        <div className="dashboard-content">
          <div className="dashboard-heading"><div><div className="breadcrumb">Inicio <span>/</span> Dashboard</div><h1>Dashboard</h1><p>Resumen general de accesos y visitantes</p></div><div className="dashboard-filters"><label><span className="sr-only">Sede</span><select value={sedeId ?? ''} onChange={(event) => setSedeId(event.target.value ? Number(event.target.value) : undefined)}><option value="">Todas las sedes</option>{datos?.sedes.map((sede) => <option key={sede.idSede} value={sede.idSede}>{sede.nombre}</option>)}</select></label><label><span className="sr-only">Periodo</span><select value={periodo} onChange={(event) => setPeriodo(event.target.value as PeriodoDashboard)}><option value="hoy">Hoy</option><option value="7d">Últimos 7 días</option></select></label></div></div>

          {dashboard.isLoading && <DashboardSkeleton />}
          {dashboard.isError && <section className="dashboard-state"><Icon>!</Icon><h2>No se pudo cargar el dashboard</h2><p>Verifica tu sesión o inténtalo de nuevo.</p><button type="button" onClick={() => void dashboard.refetch()}>Reintentar</button></section>}
          {datos && <>
            <section className="metric-grid" aria-label="Indicadores principales"><Metric icon="♙" label="Personas actualmente dentro" value={datos.resumen.personasDentro} note="Permanencias abiertas" tone="purple" /><Metric icon="◷" label="Visitas registradas" value={datos.resumen.visitas} note={periodo === 'hoy' ? 'Durante hoy' : 'Durante los últimos 7 días'} tone="purple" /><Metric icon="▣" label="Autorizaciones activas" value={datos.resumen.autorizacionesActivas} note="Con vigencia actual" tone="purple" /><Metric icon="△" label="Alertas pendientes" value={datos.resumen.alertasPendientes} note={datos.resumen.alertasPendientes ? 'Requieren revisión' : 'Sin pendientes'} tone="red" /></section>
            <section className="dashboard-grid dashboard-grid-top"><article className="dashboard-card visits-card"><CardTitle title={`Visitas de ${periodo === 'hoy' ? 'hoy' : 'los últimos 7 días'}`} subtitle="Total de registros confirmados por día" action="Actualizar" /><div className="bar-chart" role="img" aria-label="Visitas por día">{datos.visitasPorDia.length === 0 ? <Empty text="No hay visitas en este periodo" /> : datos.visitasPorDia.map((item) => <div className="bar-column" key={item.fecha}><span>{item.total}</span><div className="bar-track"><i style={{ height: `${Math.max((item.total / maxVisitas) * 100, 5)}%` }} /></div><small>{formatoDia.format(new Date(`${item.fecha}T12:00:00`)).replace('.', '')}</small></div>)}</div></article><article className="dashboard-card distribution-card"><CardTitle title="Distribución por tipo" subtitle="Ingresos del periodo seleccionado" />{totalDistribucion === 0 ? <Empty text="No hay ingresos registrados" /> : <><div className="donut-wrap"><div className="donut" style={{ background: donut }}><div>{totalDistribucion}<small>ingresos</small></div></div></div><div className="legend">{datos.distribucionPorTipo.slice(0, 4).map((item, index) => <span key={item.tipo}><i className={`legend-dot dot-${index}`} />{item.tipo}<b>{Math.round((item.total / totalDistribucion) * 100)}%</b></span>)}</div></>}</article></section>
            <section className="dashboard-grid dashboard-grid-bottom"><article className="dashboard-card inside-card"><CardTitle title="Personas actualmente dentro" subtitle="Actualizado al consultar el dashboard" action="Ver todas" /><div className="table-wrap">{datos.personasDentro.length === 0 ? <Empty text="No hay personas dentro en este momento" /> : <table><thead><tr><th>Persona</th><th>Tipo</th><th>Entrada</th><th>Zona / destino</th><th>Estado</th></tr></thead><tbody>{datos.personasDentro.map((persona) => <tr key={persona.idPersona}><td><strong>{persona.persona}</strong></td><td>{persona.tipo}</td><td>{formatoHora.format(new Date(persona.entradaEn))}</td><td>{persona.zonaDestino ?? '—'}</td><td><span className="status-pill">{persona.estado}</span></td></tr>)}</tbody></table>}</div></article><article className="dashboard-card activity-card"><CardTitle title="Actividad reciente" subtitle="Eventos de la organización" />{datos.actividadReciente.length === 0 ? <Empty text="No hay actividad reciente" /> : <div className="activity-list">{datos.actividadReciente.map((actividad, index) => <div className="activity-item" key={`${actividad.ocurrioEn}-${index}`}><span className={`activity-icon ${actividad.tipo}`}><Icon>{actividad.tipo === 'alerta' ? '△' : actividad.tipo === 'salida' ? '↗' : '↘'}</Icon></span><div><strong>{actividad.titulo}</strong><span>{actividad.detalle ?? 'Sin detalle'}</span></div><time>{formatoHora.format(new Date(actividad.ocurrioEn))}</time></div>)}</div>}</article></section>
          </>}
        </div>
      </main>
    </div>
  )
}

function Metric({ icon, label, value, note, tone }: { icon: string; label: string; value: number; note: string; tone: string }) { return <article className="metric-card"><div className={`metric-icon ${tone}`}><Icon>{icon}</Icon></div><div><span>{label}</span><strong>{value}</strong><small>{note}</small></div><em>—</em></article> }
function CardTitle({ title, subtitle, action }: { title: string; subtitle: string; action?: string }) { return <div className="card-title"><div><h2>{title}</h2><p>{subtitle}</p></div>{action && <button type="button">{action}</button>}</div> }
function Empty({ text }: { text: string }) { return <div className="empty-state"><span>○</span><p>{text}</p></div> }
function DashboardSkeleton() { return <div className="dashboard-loading"><div /><div /><div /><div /></div> }
