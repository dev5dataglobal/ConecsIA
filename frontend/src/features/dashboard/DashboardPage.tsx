import { useQuery } from '@tanstack/react-query'
import { useMemo, useState, type CSSProperties } from 'react'
import { Link } from 'react-router'
import { useShell } from '../../app/shell/contextoShell'
import { Icono, type NombreIcono } from '../../shared/ui/Icono'
import { useMisModulos } from '../modulos/apiModulos'
import { consultarDashboard, type PeriodoDashboard } from './apiDashboard'
import './dashboard.css'

const formatoHora = new Intl.DateTimeFormat('es-CO', { hour: '2-digit', minute: '2-digit' })
const formatoDia = new Intl.DateTimeFormat('es-CO', { weekday: 'short' })
const formatoFecha = new Intl.DateTimeFormat('es-CO', { weekday: 'long', day: 'numeric', month: 'long' })
const COLORES_TIPO = ['#7c6cf2', '#c9f26b', '#56c4ff', '#ff7ab6', '#ffb84d', '#a6a5b2']

export function DashboardPage() {
  const { abrirPerfil, correo } = useShell()
  const [periodo, setPeriodo] = useState<PeriodoDashboard>('7d')
  const [sedeId, setSedeId] = useState<number | undefined>()
  const [hoy] = useState(() => new Date())
  const dashboard = useQuery({
    queryKey: ['dashboard', periodo, sedeId],
    queryFn: () => consultarDashboard(periodo, sedeId),
  })
  // La lista de sedes se toma de la consulta sin filtro para que el selector no se reduzca a la sede elegida.
  const sedes = useQuery({ queryKey: ['dashboard', periodo, undefined], queryFn: () => consultarDashboard(periodo), enabled: sedeId !== undefined })
  const datos = dashboard.data
  const listaSedes = (sedeId === undefined ? datos?.sedes : sedes.data?.sedes) ?? datos?.sedes ?? []
  const modulos = useMisModulos()
  const puedeAdministrar = modulos.data?.some((m) => m.codigo === 'administracion') ?? false
  const nombre = correo?.split('@')[0]
  const textoPeriodo = periodo === 'hoy' ? 'hoy' : 'los últimos 7 días'

  const maxVisitas = Math.max(...(datos?.visitasPorDia.map((item) => item.total) ?? [0]), 1)
  const totalDistribucion = datos?.distribucionPorTipo.reduce((total, item) => total + item.total, 0) ?? 0
  const donut = useMemo(() => {
    if (!datos || totalDistribucion === 0) return undefined
    let inicio = 0
    const tramos = datos.distribucionPorTipo.map((item, indice) => {
      const fin = inicio + (item.total / totalDistribucion) * 100
      const tramo = `${COLORES_TIPO[indice % COLORES_TIPO.length]} ${inicio}% ${fin}%`
      inicio = fin
      return tramo
    })
    return `conic-gradient(${tramos.join(', ')})`
  }, [datos, totalDistribucion])

  return (
    <div className="db">
      <section className="db-hero" aria-labelledby="db-titulo">
        <div className="db-hero-fondo" aria-hidden="true" />
        <div className="db-hero-copy">
          <nav className="db-migas" aria-label="Ruta de navegación">
            Inicio <span aria-hidden="true">/</span> Dashboard
          </nav>
          <h1 id="db-titulo">
            Hola{nombre ? `, ${nombre}` : ''} <span aria-hidden="true">👋</span>
          </h1>
          <p className="db-hero-sub">
            Resumen de accesos y visitantes · <span className="db-capital">{formatoFecha.format(hoy)}</span>
          </p>
          <div className="db-contexto">
            {datos && (
              <span className="db-chip">
                <Icono nombre="administracion" /> {datos.organizacion.nombre}
              </span>
            )}
            <span className="db-chip">
              <Icono nombre="sede" />{' '}
              {sedeId ? (listaSedes.find((sede) => sede.idSede === sedeId)?.nombre ?? 'Sede') : 'Todas las sedes'}
            </span>
            {dashboard.dataUpdatedAt > 0 && (
              <span className="db-chip db-chip-vivo">
                <i aria-hidden="true" /> Actualizado {formatoHora.format(dashboard.dataUpdatedAt)}
              </span>
            )}
          </div>
        </div>

        <div className="db-hero-controles">
          <div className="db-filtros">
            <label className="db-select">
              <span className="sr-only">Sede</span>
              <Icono nombre="sede" />
              <select value={sedeId ?? ''} onChange={(event) => setSedeId(event.target.value ? Number(event.target.value) : undefined)}>
                <option value="">Todas las sedes</option>
                {listaSedes.map((sede) => (
                  <option key={sede.idSede} value={sede.idSede}>
                    {sede.nombre}
                  </option>
                ))}
              </select>
            </label>
            <div className="db-segmento" role="radiogroup" aria-label="Periodo">
              {(['hoy', '7d'] as const).map((valor) => (
                <button
                  key={valor}
                  type="button"
                  role="radio"
                  aria-checked={periodo === valor}
                  onClick={() => setPeriodo(valor)}
                >
                  {valor === 'hoy' ? 'Hoy' : '7 días'}
                </button>
              ))}
            </div>
          </div>
          <div className="db-acciones" aria-label="Accesos rápidos">
            <button type="button" className="db-accion" onClick={() => void dashboard.refetch()} disabled={dashboard.isFetching}>
              <Icono nombre="actualizar" className={dashboard.isFetching ? 'db-girando' : undefined} />
              {dashboard.isFetching ? 'Actualizando…' : 'Actualizar'}
            </button>
            {puedeAdministrar && (
              <Link to="/administracion" className="db-accion">
                <Icono nombre="administracion" /> Administración
              </Link>
            )}
            <button type="button" className="db-accion" onClick={abrirPerfil}>
              <Icono nombre="perfil" /> Mi perfil
            </button>
          </div>
        </div>
      </section>

      {dashboard.isLoading && <Esqueleto />}

      {dashboard.isError && (
        <section className="db-tarjeta db-estado-error" role="alert">
          <span className="db-estado-icono">
            <Icono nombre="alertas" />
          </span>
          <h2>No se pudo cargar el dashboard</h2>
          <p>Verifica tu sesión o inténtalo de nuevo.</p>
          <button type="button" className="db-boton" onClick={() => void dashboard.refetch()}>
            Reintentar
          </button>
        </section>
      )}

      {datos && (
        <>
          <section className="db-metricas" aria-label="Indicadores principales">
            <Metrica
              icono="personas"
              etiqueta="Personas actualmente dentro"
              valor={datos.resumen.personasDentro}
              nota="Permanencias abiertas"
              tono="lima"
              destacada
            />
            <Metrica
              icono="visitas"
              etiqueta="Visitas registradas"
              valor={datos.resumen.visitas}
              nota={periodo === 'hoy' ? 'Durante hoy' : 'Últimos 7 días'}
              tono="violeta"
            />
            <Metrica
              icono="autorizaciones"
              etiqueta="Autorizaciones activas"
              valor={datos.resumen.autorizacionesActivas}
              nota="Con vigencia actual"
              tono="azul"
            />
            <Metrica
              icono="alertas"
              etiqueta="Alertas pendientes"
              valor={datos.resumen.alertasPendientes}
              nota={datos.resumen.alertasPendientes ? 'Requieren revisión' : 'Sin pendientes'}
              tono={datos.resumen.alertasPendientes ? 'rojo' : 'neutro'}
            />
          </section>

          <section className="db-rejilla">
            <article className="db-tarjeta">
              <Encabezado titulo={`Visitas de ${textoPeriodo}`} subtitulo="Registros confirmados por día" />
              {datos.visitasPorDia.length === 0 ? (
                <Vacio texto="No hay visitas en este periodo" />
              ) : (
                <div className="db-barras" role="img" aria-label={`Visitas por día: ${datos.visitasPorDia.map((d) => `${d.fecha} ${d.total}`).join(', ')}`}>
                  {datos.visitasPorDia.map((item) => (
                    <div className="db-barra" key={item.fecha}>
                      <span className="db-barra-valor">{item.total}</span>
                      <div className="db-barra-pista">
                        <i style={{ '--alto': `${Math.max((item.total / maxVisitas) * 100, 4)}%` } as CSSProperties} />
                      </div>
                      <small>{formatoDia.format(new Date(`${item.fecha}T12:00:00`)).replace('.', '')}</small>
                    </div>
                  ))}
                </div>
              )}
            </article>

            <article className="db-tarjeta">
              <Encabezado titulo="Ingresos por tipo de persona" subtitulo={`Entradas confirmadas de ${textoPeriodo}`} />
              {totalDistribucion === 0 ? (
                <Vacio texto="No hay ingresos registrados" />
              ) : (
                <div className="db-distribucion">
                  <div className="db-dona" style={{ background: donut }} role="img" aria-label={`${totalDistribucion} ingresos`}>
                    <div>
                      <strong>{totalDistribucion}</strong>
                      <small>ingresos</small>
                    </div>
                  </div>
                  <ul className="db-leyenda">
                    {datos.distribucionPorTipo.map((item, indice) => (
                      <li key={item.tipo}>
                        <i style={{ background: COLORES_TIPO[indice % COLORES_TIPO.length] }} />
                        <span>{item.tipo}</span>
                        <b>{Math.round((item.total / totalDistribucion) * 100)}%</b>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </article>

            <article className="db-tarjeta">
              <Encabezado
                titulo="Personas actualmente dentro"
                subtitulo="Permanencias abiertas más recientes"
                insignia={datos.resumen.personasDentro}
              />
              {datos.personasDentro.length === 0 ? (
                <Vacio texto="No hay personas dentro en este momento" />
              ) : (
                <div className="db-tabla-wrap">
                  <table className="db-tabla">
                    <thead>
                      <tr>
                        <th scope="col">Persona</th>
                        <th scope="col">Tipo</th>
                        <th scope="col">Entrada</th>
                        <th scope="col">Zona / destino</th>
                        <th scope="col">Estado</th>
                      </tr>
                    </thead>
                    <tbody>
                      {datos.personasDentro.map((persona) => (
                        <tr key={persona.idPersona}>
                          <td>
                            <span className="db-persona">
                              <span className="db-avatar" aria-hidden="true">
                                {iniciales(persona.persona)}
                              </span>
                              <strong>{persona.persona}</strong>
                            </span>
                          </td>
                          <td>{persona.tipo}</td>
                          <td>{formatoHora.format(new Date(persona.entradaEn))}</td>
                          <td>{persona.zonaDestino ?? '—'}</td>
                          <td>
                            <span className="db-pildora">{persona.estado}</span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </article>

            <article className="db-tarjeta">
              <Encabezado titulo="Actividad reciente" subtitulo="Entradas, salidas y alertas del periodo" />
              {datos.actividadReciente.length === 0 ? (
                <Vacio texto="No hay actividad reciente" />
              ) : (
                <ol className="db-actividad">
                  {datos.actividadReciente.map((actividad, indice) => (
                    <li key={`${actividad.ocurrioEn}-${indice}`} data-tipo={actividad.tipo}>
                      <span className="db-actividad-icono">
                        <Icono nombre={actividad.tipo === 'alerta' ? 'alertas' : actividad.tipo === 'salida' ? 'salida' : 'entrada'} />
                      </span>
                      <div>
                        <strong>{actividad.titulo}</strong>
                        <span>{actividad.detalle ?? 'Sin detalle'}</span>
                      </div>
                      <time dateTime={actividad.ocurrioEn}>{formatoHora.format(new Date(actividad.ocurrioEn))}</time>
                    </li>
                  ))}
                </ol>
              )}
            </article>
          </section>
        </>
      )}
    </div>
  )
}

function iniciales(nombre: string) {
  return nombre
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((parte) => parte.charAt(0).toUpperCase())
    .join('')
}

type Tono = 'lima' | 'violeta' | 'azul' | 'rojo' | 'neutro'

function Metrica({ icono, etiqueta, valor, nota, tono, destacada }: { icono: NombreIcono; etiqueta: string; valor: number; nota: string; tono: Tono; destacada?: boolean }) {
  return (
    <article className="db-metrica" data-tono={tono} data-destacada={destacada || undefined}>
      <span className="db-metrica-icono">
        <Icono nombre={icono} />
      </span>
      <span className="db-metrica-etiqueta">{etiqueta}</span>
      <strong className="db-metrica-valor">{valor.toLocaleString('es-CO')}</strong>
      <small className="db-metrica-nota">{nota}</small>
    </article>
  )
}

function Encabezado({ titulo, subtitulo, insignia }: { titulo: string; subtitulo: string; insignia?: number }) {
  return (
    <header className="db-tarjeta-cab">
      <div>
        <h2>
          {titulo}
          {insignia !== undefined && insignia > 0 && <span className="db-insignia">{insignia}</span>}
        </h2>
        <p>{subtitulo}</p>
      </div>
    </header>
  )
}

function Vacio({ texto }: { texto: string }) {
  return (
    <div className="db-vacio">
      <span>
        <Icono nombre="vacio" />
      </span>
      <p>{texto}</p>
    </div>
  )
}

function Esqueleto() {
  return (
    <div className="db-esqueleto" aria-busy="true" aria-label="Cargando dashboard">
      <div /><div /><div /><div />
      <div className="db-esqueleto-ancho" /><div className="db-esqueleto-ancho" />
    </div>
  )
}
