import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { ApiError } from '../../shared/api/problemDetails'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { buscarPorDocumento, consultarCatalogosPersona, consultarPersonas, type PersonaLista } from '../personas/apiPersonas'
import { PermisoAcceso, PersonaFormulario } from '../personas/PersonasPage'
import { leerOrganizacionActiva } from '../sesion/sesion'
import { BotonIcono, BotonNuevo, Buscador, Campo, Dialogo, Estado, EstadoConsulta, Filtro, Marco } from '../administracion/componentes'
import { mensajeError, useAviso, useDiferido } from '../administracion/utilidades'
import '../administracion/administracion.css'
import '../personas/personas.css'
import '../autorizaciones/autorizaciones.css'
import './visitas.css'
import {
  cancelarVisita, consultarAutorizacionesDisponibles, consultarOpcionesVisita, consultarPresencia, consultarVisita, consultarVisitas, nuevaClave,
  registrarMovimiento, registrarVisita, vincularAutorizacion,
  type EstadoVisita, type OpcionesVisita, type VisitaDetalle, type VisitaEntrada, type VisitaLista,
} from './apiVisitas'

/*
 * Recepción y portería: visitas de personas externas, su entrada, permanencia y salida. Todo lo que decide si una
 * persona puede ingresar (estado, vigencia, horario, alcance, cupo) lo evalúa el servidor; aquí solo se presenta.
 */

const CLAVE = 'visitas'
const ETIQUETA: Record<EstadoVisita, string> = {
  Pendiente: 'Pendiente', Autorizada: 'Autorizada', Ingreso: 'Dentro', Finalizada: 'Finalizada', Rechazada: 'Rechazada', Cancelada: 'Cancelada', Vencida: 'Vencida',
}
const ESTADOS = [{ valor: '', texto: 'Todos los estados' }, ...(Object.keys(ETIQUETA) as EstadoVisita[]).map((e) => ({ valor: e, texto: ETIQUETA[e] }))]
const ORDENES = [
  { valor: '', texto: 'Más recientes' },
  { valor: 'creado_asc', texto: 'Más antiguas' },
  { valor: 'persona', texto: 'Persona (A–Z)' },
  { valor: 'entrada_desc', texto: 'Última entrada' },
  { valor: 'estado', texto: 'Estado' },
]
const TAMANO = 20

/** Instante en la zona horaria de la sede (los instantes viajan en UTC). */
function enSede(iso: string, zona: string, opciones: Intl.DateTimeFormatOptions = { dateStyle: 'medium', timeStyle: 'short' }) {
  try { return new Date(iso).toLocaleString('es-CO', { ...opciones, timeZone: zona }) } catch { return new Date(iso).toLocaleString('es-CO', opciones) }
}
const fecha = (iso: string) => new Date(`${iso}T00:00:00`).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' })
const hora = (valor: string | null) => valor?.slice(0, 5) ?? ''
function duracion(segundos: number) {
  const s = Math.max(0, Math.floor(segundos))
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  return h > 0 ? `${h} h ${m} min` : `${m} min`
}
const hoyIso = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Motivos de una denegación (lista del servidor) o el mensaje general. */
function motivosError(error: unknown): string[] {
  if (error instanceof ApiError && error.problem.motivos?.length) return error.problem.motivos
  return [mensajeError(error)]
}

export function EstadoVisitaEtiqueta({ estado }: { estado: EstadoVisita }) {
  return <span className="ad-estatus vi-estado" data-estado={estado}>{ETIQUETA[estado]}</span>
}

type Pestana = 'visitas' | 'presencia'

export function VisitasPage() {
  const control = useAcciones('control_accesos')
  const [pestana, setPestana] = useState<Pestana>('visitas')
  const pestanas = (
    <div className="ad-tabs" role="tablist" aria-label="Secciones de visitas">
      <button type="button" role="tab" aria-selected={pestana === 'visitas'} onClick={() => setPestana('visitas')}><Icono nombre="visitas" /> Visitas</button>
      {control.consultar && (
        <button type="button" role="tab" aria-selected={pestana === 'presencia'} onClick={() => setPestana('presencia')}><Icono nombre="accesos" /> Personas dentro</button>
      )}
    </div>
  )
  return pestana === 'presencia' && control.consultar ? <PresenciaSeccion pestanas={pestanas} /> : <ListaVisitas pestanas={pestanas} />
}

function useOpciones(habilitado: boolean) {
  return useQuery({ queryKey: [CLAVE, 'opciones', leerOrganizacionActiva()], queryFn: consultarOpcionesVisita, enabled: habilitado, staleTime: 60_000 })
}

const TEXTO_EVENTO: Record<string, string> = {
  Registrar: 'Visita registrada',
  VincularAutorizacion: 'Autorización vinculada',
  Cancelar: 'Visita cancelada',
  ActualizarEstado: 'Estado actualizado',
}

// ---------- Listado ----------

/** Filtro de texto que, al cambiar, vuelve a la primera página. */
function useFiltro(setPagina: (p: number) => void) {
  const [valor, asignar] = useState('')
  return [valor, (v: string) => { asignar(v); setPagina(1) }] as const
}

function ListaVisitas({ pestanas }: { pestanas: ReactNode }) {
  const acciones = useAcciones('visitas')
  const [pagina, setPagina] = useState(1)
    const [buscar, setBuscar] = useFiltro(setPagina)
  const [estado, setEstado] = useFiltro(setPagina)
  const [idSede, setIdSede] = useFiltro(setPagina)
  const [idArea, setIdArea] = useFiltro(setPagina)
  const [idResponsable, setIdResponsable] = useFiltro(setPagina)
  const [desde, setDesde] = useFiltro(setPagina)
  const [hasta, setHasta] = useFiltro(setPagina)
  const [orden, setOrden] = useFiltro(setPagina)
  const [nueva, setNueva] = useState(false)
  const [detalle, setDetalle] = useState<number | null>(null)
  const { avisar, aviso } = useAviso()
  const texto = useDiferido(buscar)
  const opciones = useOpciones(acciones.consultar)
  const filtros = { buscar: texto, estado, idSede, idArea, idResponsable, desde, hasta, orden }
  const lista = useQuery({
    queryKey: [CLAVE, 'lista', leerOrganizacionActiva(), filtros, pagina],
    queryFn: () => consultarVisitas({ ...filtros, pagina, tamano: TAMANO }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })
  const datos = lista.data
  const paginas = datos ? Math.max(1, Math.ceil(datos.total / datos.tamano)) : 1
  const filtrando = Boolean(buscar || estado || idSede || idArea || idResponsable || desde || hasta)
  const areas = (opciones.data?.areas ?? []).filter((a) => !idSede || String(a.idSede) === idSede)

  const indicadores: { estado: EstadoVisita; texto: string; valor: number | undefined }[] = [
    { estado: 'Pendiente', texto: 'Pendientes', valor: datos?.indicadores.pendientes },
    { estado: 'Autorizada', texto: 'Autorizadas', valor: datos?.indicadores.autorizadas },
    { estado: 'Ingreso', texto: 'Dentro de la sede', valor: datos?.indicadores.dentro },
    { estado: 'Finalizada', texto: 'Finalizadas', valor: datos?.indicadores.finalizadas },
  ]

  return (
    <Marco titulo="Visitas" migas="Operación" pestanas={pestanas}
      descripcion="Registra a quien llega, confirma que su autorización está vigente y controla su entrada y salida."
      accion={acciones.anadir && <BotonNuevo texto="Nueva visita" onClick={() => setNueva(true)} />}>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar visitas." />
      ) : (
        <>
          <div className="vi-indicadores" role="group" aria-label="Resumen de visitas">
            {indicadores.map((i) => (
              <button key={i.estado} type="button" className="vi-indicador" data-estado={i.estado} aria-pressed={estado === i.estado}
                onClick={() => setEstado(estado === i.estado ? '' : i.estado)}>
                <strong>{i.valor ?? '—'}</strong><span>{i.texto}</span>
              </button>
            ))}
          </div>
          <div className="ad-herramientas au-filtros">
            <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar visitas" placeholder="Documento o nombre del visitante" />
            <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS} />
            <Filtro etiqueta="Filtrar por sede" valor={idSede} onChange={(v) => { setIdSede(v); setIdArea('') }}
              opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(opciones.data?.sedes ?? []).map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
            <Filtro etiqueta="Filtrar por destino" valor={idArea} onChange={setIdArea}
              opciones={[{ valor: '', texto: 'Todos los destinos' }, ...areas.map((a) => ({ valor: String(a.id), texto: a.nombre }))]} />
            <Filtro etiqueta="Filtrar por responsable" valor={idResponsable} onChange={setIdResponsable}
              opciones={[{ valor: '', texto: 'Todos los responsables' }, ...(opciones.data?.responsables ?? []).map((r) => ({ valor: String(r.idMembresia), texto: r.nombre }))]} />
            <label className="au-fecha">Desde<input type="date" value={desde} onChange={(e) => setDesde(e.target.value)} /></label>
            <label className="au-fecha">Hasta<input type="date" value={hasta} onChange={(e) => setHasta(e.target.value)} /></label>
            <Filtro etiqueta="Ordenar" valor={orden} onChange={setOrden} opciones={ORDENES} />
          </div>
          <EstadoConsulta loading={lista.isLoading} error={lista.error} empty={!datos?.registros.length}
            vacio={filtrando ? 'Ninguna visita coincide con los filtros.' : 'Aún no hay visitas registradas.'} />
        </>
      )}
      {datos && datos.registros.length > 0 && (
        <>
          <div className="ad-tabla-wrap">
            <table className="ad-tabla vi-tabla">
              <thead>
                <tr>
                  <th scope="col">Visitante</th>
                  <th scope="col">Sede / destino</th>
                  <th scope="col">Responsable</th>
                  <th scope="col">Fecha</th>
                  <th scope="col">Estado</th>
                  <th scope="col"><span className="sr-only">Acciones</span></th>
                </tr>
              </thead>
              <tbody>
                {datos.registros.map((v) => (
                  <tr key={v.idVisita}>
                    <td data-columna="Visitante"><strong>{v.persona}</strong><small className="au-sub">{v.documentoPersona}{v.personaActiva ? '' : ' · inactiva'}</small></td>
                    <td data-columna="Sede / destino">{v.sede}<small className="au-sub">{v.area}</small></td>
                    <td data-columna="Responsable">{v.responsable ?? '—'}</td>
                    <td data-columna="Fecha">
                      {v.entradaEn ? `Entró ${enSede(v.entradaEn, v.zonaHoraria)}` : v.fechaEsperada ? `${fecha(v.fechaEsperada)}${v.horaEsperadaInicio ? ` · ${hora(v.horaEsperadaInicio)}` : ''}` : enSede(v.creadoEn, v.zonaHoraria)}
                      {v.salidaEn && <small className="au-sub">Salió {enSede(v.salidaEn, v.zonaHoraria, { timeStyle: 'short' })}</small>}
                    </td>
                    <td data-columna="Estado"><EstadoVisitaEtiqueta estado={v.estado} /></td>
                    <td className="ad-acciones">
                      <BotonIcono etiqueta={`Ver visita de ${v.persona}`} icono="flecha" onClick={() => setDetalle(v.idVisita)} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <nav className="vi-paginacion" aria-label="Paginación de visitas">
            <span>{datos.total} visita{datos.total === 1 ? '' : 's'} · página {datos.pagina} de {paginas}</span>
            <button type="button" className="ad-boton-sec" disabled={pagina <= 1} onClick={() => setPagina((p) => p - 1)}>Anterior</button>
            <button type="button" className="ad-boton-sec" disabled={pagina >= paginas} onClick={() => setPagina((p) => p + 1)}>Siguiente</button>
          </nav>
        </>
      )}
      {nueva && opciones.data && (
        <NuevaVisita opciones={opciones.data} onClose={() => setNueva(false)}
          onCreada={(v) => { setNueva(false); avisar(v.visita.estado === 'Autorizada' ? 'Visita registrada y autorizada.' : 'Visita registrada. Queda pendiente de autorización.'); setDetalle(v.visita.idVisita) }} />
      )}
      {detalle !== null && <DetalleVisita id={detalle} opciones={opciones.data} onClose={() => setDetalle(null)} onAviso={avisar} />}
      {aviso}
    </Marco>
  )
}

// ---------- Detalle y operación ----------

type Operacion = 'entrada' | 'salida' | 'cancelar' | 'autorizacion'

function DetalleVisita({ id, opciones, onClose, onAviso }: { id: number; opciones: OpcionesVisita | undefined; onClose: () => void; onAviso: (m: string) => void }) {
  const queryClient = useQueryClient()
  const visitas = useAcciones('visitas')
  const control = useAcciones('control_accesos')
  const detalle = useQuery({ queryKey: [CLAVE, 'detalle', id], queryFn: () => consultarVisita(id) })
  const [operacion, setOperacion] = useState<Operacion | null>(null)
  const d = detalle.data
  const v = d?.visita

  function aplicado(resultado: VisitaDetalle, mensaje: string) {
    queryClient.setQueryData([CLAVE, 'detalle', id], resultado)
    void queryClient.invalidateQueries({ queryKey: [CLAVE], predicate: (q) => q.queryKey[1] !== 'detalle' && q.queryKey[1] !== 'opciones' })
    setOperacion(null)
    onAviso(mensaje)
  }

  const puedeCancelar = visitas.editar && v && (v.estado === 'Pendiente' || v.estado === 'Autorizada')
  const puedeVincular = visitas.editar && v?.estado === 'Pendiente' && !v.idAutorizacion

  return (
    <Dialogo titulo={v ? v.persona : 'Visita'} eyebrow="Visita" icono="visitas" ancho="matriz" onClose={onClose} onSubmit={(e) => e.preventDefault()}
      pie={(
        <>
          <button type="button" className="ad-boton-sec" onClick={onClose}>Cerrar</button>
          {d && operacion === null && (
            <>
              {puedeVincular && <button type="button" className="ad-boton-sec" onClick={() => setOperacion('autorizacion')}>Vincular autorización</button>}
              {puedeCancelar && <button type="button" className="ad-boton-sec au-peligro" onClick={() => setOperacion('cancelar')}>Cancelar visita</button>}
              {control.editar && d.evaluacion.puedeSalir && <button type="button" className="ad-boton" onClick={() => setOperacion('salida')}><Icono nombre="salida" /> Registrar salida</button>}
              {control.anadir && !d.evaluacion.puedeSalir && v && (v.estado === 'Pendiente' || v.estado === 'Autorizada') && (
                <button type="button" className="ad-boton" disabled={!d.evaluacion.puedeIngresar} onClick={() => setOperacion('entrada')}><Icono nombre="entrada" /> Registrar entrada</button>
              )}
            </>
          )}
        </>
      )}>
      <EstadoConsulta loading={detalle.isLoading} error={detalle.error} empty={false} vacio="" />
      {d && v && (
        <>
          {!d.evaluacion.puedeIngresar && !d.evaluacion.puedeSalir && d.evaluacion.motivos.length > 0 && (
            <div className="vi-alerta" role="alert">
              <strong>{v.estado === 'Pendiente' || v.estado === 'Autorizada' ? 'No puede ingresar todavía' : 'Visita cerrada'}</strong>
              <ul>{d.evaluacion.motivos.map((m) => <li key={m}>{m}</li>)}</ul>
            </div>
          )}
          {d.evaluacion.puedeIngresar && <p className="vi-ok" role="status"><Icono nombre="check" /> La autorización está vigente: puede registrarse la entrada.</p>}
          <dl className="dt-franja">
            <div><dt>Estado</dt><dd><EstadoVisitaEtiqueta estado={v.estado} /></dd></div>
            <div><dt>Documento</dt><dd>{v.documentoPersona}{v.personaActiva ? '' : <small> · persona inactiva</small>}</dd></div>
          </dl>
          <div className="dt-bloques">
            <section className="dt-bloque" aria-label="Destino">
              <h3>Destino</h3>
              <dl>
                <div><dt>Sede / destino</dt><dd>{v.sede}<small>{v.area}</small></dd></div>
                {v.fechaEsperada && <div><dt>Esperada</dt><dd>{fecha(v.fechaEsperada)}{v.horaEsperadaInicio ? ` · ${hora(v.horaEsperadaInicio)} a ${hora(v.horaEsperadaFin)}` : ''}</dd></div>}
              </dl>
            </section>
            <section className="dt-bloque" aria-label="Trazabilidad">
              <h3>Trazabilidad</h3>
              <dl>
                <div><dt>Responsable</dt><dd>{v.responsable ?? '—'}</dd></div>
                <div><dt>Registrada por</dt><dd>{d.registradaPor ?? '—'}<small>{enSede(v.creadoEn, v.zonaHoraria)}</small></dd></div>
              </dl>
            </section>
          </div>
          {(v.motivo || v.excepcionTipoAcceso || d.motivoCierre) && (
            <section className="dt-bloque" aria-label="Motivos">
              <h3>Motivos</h3>
              <dl>
                {v.motivo && <div><dt>Motivo</dt><dd><p className="dt-nota">{v.motivo}</p></dd></div>}
                {v.excepcionTipoAcceso && <div><dt>Excepción al tipo de permiso</dt><dd><p className="dt-nota">Registrada como visita aunque la persona tenía permiso recurrente (clasificación retirada el 2026-10-28). Motivo: {v.motivoExcepcion}</p></dd></div>}
                {d.motivoCierre && <div><dt>Motivo de cierre</dt><dd><p className="dt-nota">{d.motivoCierre}</p></dd></div>}
              </dl>
            </section>
          )}

          <section className="dt-bloque" aria-label="Autorización">
            <h3>Autorización</h3>
            {d.autorizacion ? (
              <p className="vi-linea">
                #{d.autorizacion.idAutorizacion} · {d.autorizacion.modalidad} · <strong>{d.autorizacion.estado}</strong> · {fecha(d.autorizacion.fechaInicio)}
                {d.autorizacion.fechaFin ? ` – ${fecha(d.autorizacion.fechaFin)}` : ' · sin fecha de fin'}
                {d.autorizacion.cupoIngresos !== null && ` · ${d.autorizacion.ingresosConsumidos}/${d.autorizacion.cupoIngresos} ingresos`}
                {d.autorizacion.horarios.length > 0 && ` · ${d.autorizacion.horarios.length} franja(s) horaria(s)`}
                {d.autorizacion.zonas.length > 0 && ` · zonas: ${d.autorizacion.zonas.join(', ')}`}
              </p>
            ) : v.politicaAplicada ? (
              <p className="vi-linea">Sin autorización previa: la permitió la política de la organización <code>{v.politicaAplicada}</code>. Nadie la aprobó.</p>
            ) : (
              <p className="vi-linea ad-tenue">Sin autorización vinculada. La decisión corresponde al responsable del destino en el módulo de autorizaciones.</p>
            )}
          </section>

          {operacion === 'entrada' || operacion === 'salida' ? (
            <RegistrarMovimiento visita={v} tipo={operacion} puntos={opciones?.puntosAcceso ?? []} onCancelar={() => setOperacion(null)}
              onHecho={(r) => aplicado(r, operacion === 'entrada' ? 'Entrada registrada.' : 'Salida registrada. La permanencia quedó cerrada.')}
              onDesactualizado={() => void queryClient.invalidateQueries({ queryKey: [CLAVE, 'detalle', id] })} />
          ) : operacion === 'cancelar' ? (
            <CancelarVisita id={id} onCancelar={() => setOperacion(null)} onHecho={(r) => aplicado(r, 'Visita cancelada.')} />
          ) : operacion === 'autorizacion' ? (
            <VincularAutorizacion visita={v} onCancelar={() => setOperacion(null)} onHecho={(r) => aplicado(r, 'Autorización vinculada.')} />
          ) : null}

          {d.permanencia && (
            <section className="dt-bloque" aria-label="Permanencia">
              <h3>Permanencia</h3>
              <p className="vi-linea">
                Entró {enSede(d.permanencia.abiertaEn, v.zonaHoraria)}
                {d.permanencia.cerradaEn ? ` · salió ${enSede(d.permanencia.cerradaEn, v.zonaHoraria)} · duración ${duracion(d.permanencia.duracionSegundos ?? 0)}` : ' · sigue dentro'}
              </p>
            </section>
          )}
          <section className="dt-bloque" aria-label="Movimientos e historial">
            <h3>Movimientos e historial</h3>
            {(() => {
              // Eventos de la visita (auditoría) + movimientos físicos (solo con ControlAccesos.Consultar), en orden cronológico.
              const filas = [
                ...d.historial.map((e) => ({
                  clave: `e${e.idEvento}`, en: e.ocurridoEn, titulo: TEXTO_EVENTO[e.accion] ?? e.accion, actor: e.actor, nota: e.motivo,
                  tono: e.accion === 'Cancelar' ? 'rechazo' : 'violeta',
                })),
                ...(control.consultar ? d.movimientos : []).map((m) => ({
                  clave: `m${m.idMovimiento}`, en: m.ocurridoEn, titulo: `${m.tipoMovimiento} · ${m.puntoAcceso}${m.zona ? ` (${m.zona})` : ''}`,
                  actor: m.operador, nota: m.observacion, tono: undefined,
                })),
              ].sort((x, y) => x.en.localeCompare(y.en))
              return filas.length === 0 ? <p className="ad-tenue vi-linea">Sin movimientos registrados.</p> : (
                <ol className="dt-linea">
                  {filas.map((f) => (
                    <li key={f.clave} data-tono={f.tono}>
                      <strong>{f.titulo}</strong>
                      <small>{f.actor ?? 'Usuario desconocido'} · {enSede(f.en, v.zonaHoraria)}</small>
                      {f.nota && <p className="dt-nota" data-tono={f.tono === 'rechazo' ? 'rechazo' : undefined}>{f.nota}</p>}
                    </li>
                  ))}
                </ol>
              )
            })()}
          </section>
        </>
      )}
    </Dialogo>
  )
}

function RegistrarMovimiento({ visita, tipo, puntos, onCancelar, onHecho, onDesactualizado }: {
  visita: VisitaLista
  tipo: 'entrada' | 'salida'
  puntos: OpcionesVisita['puntosAcceso']
  onCancelar: () => void
  onHecho: (r: VisitaDetalle) => void
  onDesactualizado: () => void
}) {
  const disponibles = puntos.filter((p) => p.idSede === visita.idSede && (p.modoAcceso === 'Ambos' || p.modoAcceso === (tipo === 'entrada' ? 'Entrada' : 'Salida')))
  const [idPunto, setIdPunto] = useState(disponibles.length === 1 ? String(disponibles[0]!.id) : '')
  const [observacion, setObservacion] = useState('')
  const [errorPunto, setErrorPunto] = useState<string | null>(null)
  // Una clave por intento: los reintentos del mismo intento (doble clic, red caída) no duplican el movimiento.
  const clave = useRef(nuevaClave())
  const enviar = useMutation({
    mutationFn: () => registrarMovimiento(visita.idVisita, tipo, Number(idPunto), observacion.trim() || null, clave.current),
    onSuccess: onHecho,
    onError: (error) => { if (error instanceof ApiError && error.status === 409) onDesactualizado() },
  })

  function confirmar() {
    if (!idPunto) { setErrorPunto('Selecciona el punto de acceso.'); return }
    setErrorPunto(null)
    if (!enviar.isPending) enviar.mutate()
  }

  return (
    <div className="pe-subformulario" role="group" aria-label={tipo === 'entrada' ? 'Registrar entrada' : 'Registrar salida'}>
      <strong>{tipo === 'entrada' ? 'Registrar entrada' : 'Registrar salida'}</strong>
      <Campo etiqueta="Punto de acceso" error={errorPunto ?? undefined}>
        <Selector className="sel-campo" etiqueta="Punto de acceso" valor={idPunto} invalido={Boolean(errorPunto)} onChange={(v) => { setIdPunto(v); clave.current = nuevaClave() }}
          opciones={[{ valor: '', texto: disponibles.length ? 'Selecciona…' : 'La sede no tiene puntos de acceso disponibles para ti', deshabilitada: true }, ...disponibles.map((p) => ({ valor: String(p.id), texto: `${p.nombre} · ${p.zona}` }))]} />
      </Campo>
      <Campo etiqueta="Observación (opcional)">
        <textarea value={observacion} maxLength={500} onChange={(e) => { setObservacion(e.target.value); clave.current = nuevaClave() }} />
      </Campo>
      {enviar.error && (
        <div className="vi-alerta" role="alert">
          <strong>{tipo === 'entrada' ? 'Ingreso denegado' : 'Salida no registrada'}</strong>
          <ul>{motivosError(enviar.error).map((m) => <li key={m}>{m}</li>)}</ul>
        </div>
      )}
      <div className="pe-subacciones">
        <button type="button" className="ad-boton-sec" onClick={onCancelar}>Volver</button>
        <button type="button" className="ad-boton" onClick={confirmar} disabled={enviar.isPending}>
          {enviar.isPending ? 'Registrando…' : tipo === 'entrada' ? 'Confirmar entrada' : 'Confirmar salida'}
        </button>
      </div>
    </div>
  )
}

function CancelarVisita({ id, onCancelar, onHecho }: { id: number; onCancelar: () => void; onHecho: (r: VisitaDetalle) => void }) {
  const [motivo, setMotivo] = useState('')
  const [error, setError] = useState<string | null>(null)
  const cancelar = useMutation({ mutationFn: () => cancelarVisita(id, motivo.trim()), onSuccess: onHecho })
  return (
    <div className="pe-subformulario" role="group" aria-label="Cancelar visita">
      <strong>Cancelar visita</strong>
      <Campo etiqueta="Motivo" error={error ?? undefined}>
        <textarea value={motivo} maxLength={500} autoFocus aria-invalid={Boolean(error)} onChange={(e) => setMotivo(e.target.value)} />
      </Campo>
      {cancelar.error && <p className="ad-mensaje" role="alert">{mensajeError(cancelar.error)}</p>}
      <div className="pe-subacciones">
        <button type="button" className="ad-boton-sec" onClick={onCancelar}>Volver</button>
        <button type="button" className="ad-boton" disabled={cancelar.isPending}
          onClick={() => { if (!motivo.trim()) { setError('Indica el motivo.'); return } setError(null); cancelar.mutate() }}>
          {cancelar.isPending ? 'Guardando…' : 'Confirmar cancelación'}
        </button>
      </div>
    </div>
  )
}

function VincularAutorizacion({ visita, onCancelar, onHecho }: { visita: VisitaLista; onCancelar: () => void; onHecho: (r: VisitaDetalle) => void }) {
  const disponibles = useQuery({ queryKey: [CLAVE, 'autorizaciones', visita.idPersona], queryFn: () => consultarAutorizacionesDisponibles(visita.idPersona) })
  const aplicables = (disponibles.data ?? []).filter((a) => a.idSede === visita.idSede && a.idArea === visita.idArea)
  const [idAutorizacion, setIdAutorizacion] = useState('')
  const vincular = useMutation({ mutationFn: () => vincularAutorizacion(visita.idVisita, Number(idAutorizacion)), onSuccess: onHecho })
  return (
    <div className="pe-subformulario" role="group" aria-label="Vincular autorización">
      <strong>Vincular autorización</strong>
      <Campo etiqueta="Autorización de la persona para este destino">
        <Selector className="sel-campo" etiqueta="Autorización" valor={idAutorizacion} onChange={setIdAutorizacion}
          opciones={[{ valor: '', texto: disponibles.isLoading ? 'Cargando…' : aplicables.length ? 'Selecciona…' : 'No hay autorizaciones para esta sede y destino', deshabilitada: true },
            ...aplicables.map((a) => ({ valor: String(a.idAutorizacion), texto: `#${a.idAutorizacion} · ${a.modalidad} · ${a.estado} · desde ${fecha(a.fechaInicio)}` }))]} />
      </Campo>
      {vincular.error && <p className="ad-mensaje" role="alert">{mensajeError(vincular.error)}</p>}
      <div className="pe-subacciones">
        <button type="button" className="ad-boton-sec" onClick={onCancelar}>Volver</button>
        <button type="button" className="ad-boton" disabled={!idAutorizacion || vincular.isPending} onClick={() => vincular.mutate()}>{vincular.isPending ? 'Guardando…' : 'Vincular'}</button>
      </div>
    </div>
  )
}

// ---------- Nueva visita (por pasos) ----------

type Paso = 'persona' | 'destino' | 'autorizacion'
type ErroresVisita = Partial<Record<'sede' | 'area' | 'responsable' | 'motivo' | 'fecha' | 'horas', string>>

function validarVisita(d: Omit<VisitaEntrada, 'idPersona'>): ErroresVisita {
  const errores: ErroresVisita = {}
  if (!d.idSede) errores.sede = 'Selecciona la sede.'
  if (!d.idArea) errores.area = 'Selecciona el destino.'
  if (!d.idMembresiaResponsable) errores.responsable = 'Selecciona quién recibe la visita.'
  if (!d.motivo.trim()) errores.motivo = 'Indica el motivo de la visita.'
  if (d.fechaEsperada && d.fechaEsperada < hoyIso()) errores.fecha = 'La fecha no puede ser anterior a hoy.'
  if (Boolean(d.horaEsperadaInicio) !== Boolean(d.horaEsperadaFin)) errores.horas = 'Indica ambas horas o ninguna.'
  else if (d.horaEsperadaInicio && d.horaEsperadaFin && d.horaEsperadaFin <= d.horaEsperadaInicio) errores.horas = 'La hora de fin debe ser posterior a la de inicio.'
  return errores
}

function NuevaVisita({ opciones, onClose, onCreada }: { opciones: OpcionesVisita; onClose: () => void; onCreada: (v: VisitaDetalle) => void }) {
  const personasAcciones = useAcciones('personas')
  const [paso, setPaso] = useState<Paso>('persona')
  const [persona, setPersona] = useState<PersonaLista | null>(null)
  const [datos, setDatos] = useState<Omit<VisitaEntrada, 'idPersona'>>({
    idSede: opciones.sedes.length === 1 ? opciones.sedes[0]!.id : 0, idArea: 0, idMembresiaResponsable: 0, motivo: '',
    fechaEsperada: null, horaEsperadaInicio: null, horaEsperadaFin: null, idAutorizacion: null, observacion: null,
  })
  const [errores, setErrores] = useState<ErroresVisita>({})
  const queryClient = useQueryClient()
  const crear = useMutation({
    mutationFn: () => registrarVisita({
      ...datos, idPersona: persona!.idPersona, motivo: datos.motivo.trim(), observacion: datos.observacion?.trim() || null,
    }),
    onSuccess: (creada) => {
      void queryClient.invalidateQueries({ queryKey: [CLAVE], predicate: (q) => q.queryKey[1] !== 'detalle' && q.queryKey[1] !== 'opciones' })
      onCreada(creada)
    },
  })
  const cambiar = (parcial: Partial<typeof datos>) => setDatos((d) => ({ ...d, ...parcial }))
  const areas = opciones.areas.filter((a) => a.idSede === datos.idSede)
  // Si el destino tiene responsables designados, solo ellos; si no, cualquier usuario activo (D-04 pendiente).
  const designados = opciones.responsables.filter((r) => r.areas.includes(datos.idArea))
  const responsables = designados.length ? designados : opciones.responsables
  // Accesos recurrentes vigentes de la persona en la sede elegida (D-46). Con ellos la visita no es extemporánea:
  // solo puede registrarse como episodio de esa autorización (CA-11). El servidor lo vuelve a validar (409).
  const recurrentesSede = (persona?.accesosRecurrentes ?? []).filter((a) => a.idSede === datos.idSede) ?? []
  const bloqueoRecurrente = recurrentesSede.length > 0 && !recurrentesSede.some((a) => a.idAutorizacion === datos.idAutorizacion)

  function siguiente(event: FormEvent) {
    event.preventDefault()
    if (paso === 'persona') {
      if (persona) setPaso('destino')
      return
    }
    if (paso === 'destino') {
      const nuevos = validarVisita(datos)
      setErrores(nuevos)
      if (Object.keys(nuevos).length === 0) {
        if (recurrentesSede.length === 1 && !datos.idAutorizacion) cambiar({ idAutorizacion: recurrentesSede[0]!.idAutorizacion })
        setPaso('autorizacion')
      }
      return
    }
    if (!crear.isPending && !bloqueoRecurrente) crear.mutate()
  }

  const pasos: { id: Paso; texto: string }[] = [{ id: 'persona', texto: 'Visitante' }, { id: 'destino', texto: 'Destino' }, { id: 'autorizacion', texto: 'Autorización' }]

  return (
    <Dialogo titulo="Nueva visita" eyebrow="Ingreso extemporáneo" icono="visitas" ancho="amplio" onClose={onClose} onSubmit={siguiente}
      pie={(
        <>
          <button type="button" className="ad-boton-sec" onClick={paso === 'persona' ? onClose : () => setPaso(paso === 'autorizacion' ? 'destino' : 'persona')}>
            {paso === 'persona' ? 'Cancelar' : 'Atrás'}
          </button>
          <button type="submit" className="ad-boton" disabled={(paso === 'persona' && !persona) || (paso === 'autorizacion' && bloqueoRecurrente) || crear.isPending}>
            {paso === 'autorizacion' ? (crear.isPending ? 'Registrando…' : 'Registrar visita') : 'Continuar'}
          </button>
        </>
      )}>
      <ol className="vi-pasos" aria-label="Pasos">
        {pasos.map((p, i) => <li key={p.id} aria-current={paso === p.id ? 'step' : undefined}><span>{i + 1}</span> {p.texto}</li>)}
      </ol>
      {paso === 'persona' && (
        <>
          <p className="vi-alerta" data-tono="info">
            <strong>Ingreso extemporáneo.</strong> Las visitas son para personas que ingresan de forma ocasional. Quien tiene acceso recurrente
            vigente en la sede ingresa con su autorización.
          </p>
          <PasoPersona persona={persona} puedeRegistrar={personasAcciones.anadir} onPersona={setPersona} />
        </>
      )}
      {paso === 'destino' && (
        <div className="ad-rejilla">
          <Campo etiqueta="Sede" error={errores.sede}>
            <Selector className="sel-campo" etiqueta="Sede" valor={String(datos.idSede || '')} invalido={Boolean(errores.sede)}
              onChange={(v) => cambiar({ idSede: Number(v), idArea: 0, idMembresiaResponsable: 0, idAutorizacion: null })}
              opciones={[{ valor: '', texto: opciones.sedes.length ? 'Selecciona…' : 'No tienes sedes en tu alcance', deshabilitada: true }, ...opciones.sedes.map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
          </Campo>
          <Campo etiqueta="Área o destino" error={errores.area}>
            <Selector className="sel-campo" etiqueta="Área o destino" valor={String(datos.idArea || '')} invalido={Boolean(errores.area)}
              onChange={(v) => cambiar({ idArea: Number(v), idMembresiaResponsable: 0, idAutorizacion: null })}
              opciones={[{ valor: '', texto: !datos.idSede ? 'Primero elige la sede' : areas.length ? 'Selecciona…' : 'La sede no tiene destinos activos', deshabilitada: true }, ...areas.map((a) => ({ valor: String(a.id), texto: a.nombre }))]} />
          </Campo>
          <Campo etiqueta="Responsable que recibe" error={errores.responsable}>
            <Selector className="sel-campo" etiqueta="Responsable que recibe" valor={String(datos.idMembresiaResponsable || '')} invalido={Boolean(errores.responsable)}
              onChange={(v) => cambiar({ idMembresiaResponsable: Number(v) })}
              opciones={[{ valor: '', texto: !datos.idArea ? 'Primero elige el destino' : 'Selecciona…', deshabilitada: true }, ...responsables.map((r) => ({ valor: String(r.idMembresia), texto: r.nombre }))]} />
          </Campo>
          <Campo etiqueta="Fecha esperada (opcional)" error={errores.fecha}>
            <input type="date" value={datos.fechaEsperada ?? ''} min={hoyIso()} aria-invalid={Boolean(errores.fecha)} onChange={(e) => cambiar({ fechaEsperada: e.target.value || null })} />
          </Campo>
          <Campo etiqueta="Desde (opcional)" error={errores.horas}>
            <input type="time" value={datos.horaEsperadaInicio ?? ''} aria-invalid={Boolean(errores.horas)} onChange={(e) => cambiar({ horaEsperadaInicio: e.target.value || null })} />
          </Campo>
          <Campo etiqueta="Hasta (opcional)">
            <input type="time" value={datos.horaEsperadaFin ?? ''} onChange={(e) => cambiar({ horaEsperadaFin: e.target.value || null })} />
          </Campo>
          <Campo etiqueta="Motivo de la visita" error={errores.motivo} ancho="completo">
            <textarea value={datos.motivo} maxLength={500} aria-invalid={Boolean(errores.motivo)} onChange={(e) => cambiar({ motivo: e.target.value })} />
          </Campo>
        </div>
      )}
      {paso !== 'persona' && recurrentesSede.length > 0 && (
        <div className="vi-alerta" data-tono={bloqueoRecurrente ? 'error' : 'info'} role={bloqueoRecurrente ? 'alert' : 'status'}>
          <strong>{persona!.nombres} tiene acceso recurrente vigente en esta sede.</strong>
          <span>
            {bloqueoRecurrente
              ? `No puede registrarse como visita extemporánea. Vincula su autorización ${recurrentesSede.map((a) => `#${a.idAutorizacion}`).join(' o ')} para registrar este ingreso como un episodio de su acceso recurrente.`
              : `La visita se registrará como episodio de su autorización #${datos.idAutorizacion}.`}
          </span>
        </div>
      )}
      {paso === 'autorizacion' && persona && (
        <PasoAutorizacion idPersona={persona.idPersona} idSede={datos.idSede} idArea={datos.idArea} valor={datos.idAutorizacion}
          permiteSinAutorizacion={opciones.permiteSinAutorizacion} onChange={(idAutorizacion) => cambiar({ idAutorizacion })} />
      )}
      {crear.error && <p className="ad-mensaje" role="alert">{mensajeError(crear.error, 'No se pudo registrar la visita.')}</p>}
    </Dialogo>
  )
}

function PasoPersona({ persona, onPersona, puedeRegistrar }: { persona: PersonaLista | null; onPersona: (p: PersonaLista | null) => void; puedeRegistrar: boolean }) {
  const catalogos = useQuery({ queryKey: ['catalogos-persona', leerOrganizacionActiva()], queryFn: consultarCatalogosPersona, staleTime: 60_000 })
  const [idTipo, setIdTipo] = useState('')
  const [numero, setNumero] = useState('')
  const [nombre, setNombre] = useState('')
  const [registrar, setRegistrar] = useState(false)
  const texto = useDiferido(nombre)
  const tipos = catalogos.data?.tiposDocumento ?? []
  const tipo = idTipo || (tipos[0] ? String(tipos[0].id) : '')
  // Buscar por documento recupera la persona existente de la organización (evita duplicados).
  const porDocumento = useMutation({ mutationFn: () => buscarPorDocumento(Number(tipo), numero), onSuccess: onPersona })
  const porNombre = useQuery({
    queryKey: ['personas', 'visita', texto],
    queryFn: () => consultarPersonas({ buscar: texto, estado: 'Activo' }),
    enabled: texto.trim().length >= 2,
  })
  const noEncontrada = porDocumento.error instanceof ApiError && porDocumento.error.status === 404

  if (persona) {
    return (
      <div className="pe-existente">
        <span className="vi-seleccion"><strong>{persona.nombres} {persona.apellidos}</strong></span>
        <span className="ad-tenue">{persona.tipoDocumento} {persona.numeroDocumento}{persona.empresaProcedencia ? ` · ${persona.empresaProcedencia}` : ''}</span>
        <span className="vi-permiso"><span className="ad-tenue">Tipo de permiso:</span> <PermisoAcceso persona={persona} /></span>
        {persona.tipoPermiso === 'Recurrente' && (
          <span className="ad-tenue">En las sedes indicadas solo podrá registrarse vinculando su autorización recurrente; en las demás, como visita extemporánea.</span>
        )}
        {!persona.activo && <span className="ad-mensaje">Persona inactiva: el servidor no permitirá registrar la visita.</span>}
        <button type="button" className="ad-enlace" onClick={() => { onPersona(null); porDocumento.reset() }}>Cambiar</button>
      </div>
    )
  }

  return (
    <div className="vi-persona">
      <div className="vi-documento">
        <Campo etiqueta="Tipo de documento">
          <Selector className="sel-campo" etiqueta="Tipo de documento" valor={tipo} onChange={setIdTipo}
            opciones={tipos.map((t) => ({ valor: String(t.id), texto: `${t.codigo} · ${t.nombre}` }))} />
        </Campo>
        <Campo etiqueta="Número de documento">
          <input value={numero} inputMode="text" autoFocus onChange={(e) => { setNumero(e.target.value); porDocumento.reset() }}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); if (numero.trim() && tipo) porDocumento.mutate() } }} />
        </Campo>
        <button type="button" className="ad-boton" disabled={!numero.trim() || !tipo || porDocumento.isPending} onClick={() => porDocumento.mutate()}>
          <Icono nombre="buscar" /> {porDocumento.isPending ? 'Buscando…' : 'Buscar'}
        </button>
      </div>
      {noEncontrada && (
        <div className="vi-alerta" data-tono="info" role="status">
          <strong>No hay una persona con ese documento en tu organización.</strong>
          {puedeRegistrar
            ? <button type="button" className="ad-boton-sec" onClick={() => setRegistrar(true)}>Registrar persona</button>
            : <span>Tu perfil no puede registrar personas; pide a un administrador que la registre.</span>}
        </div>
      )}
      {porDocumento.error && !noEncontrada && <p className="ad-mensaje" role="alert">{mensajeError(porDocumento.error)}</p>}
      <div className="au-buscador-persona">
        <label className="ad-tenue" htmlFor="vi-buscar-nombre">O busca por nombre</label>
        <input id="vi-buscar-nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Nombre o documento (mínimo 2 caracteres)" />
        {porNombre.data && (
          <ul role="listbox" aria-label="Personas encontradas">
            {porNombre.data.length === 0 && <li className="ad-tenue">Sin coincidencias entre personas activas.</li>}
            {porNombre.data.slice(0, 8).map((p) => (
              <li key={p.idPersona}>
                <button type="button" role="option" aria-selected={false} onClick={() => onPersona(p)}>
                  {p.nombres} {p.apellidos} <small>{p.tipoDocumento} {p.numeroDocumento} · {p.tipoPermiso === 'Recurrente' ? 'Recurrente' : 'Extemporáneo'}</small>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
      {registrar && (
        <PersonaFormulario persona={null} catalogos={catalogos.data} onClose={() => setRegistrar(false)}
          onVerExistente={() => { setRegistrar(false); porDocumento.mutate() }}
          onGuardado={() => { setRegistrar(false); porDocumento.mutate() }} />
      )}
    </div>
  )
}

function PasoAutorizacion({ idPersona, idSede, idArea, valor, permiteSinAutorizacion, onChange }: {
  idPersona: number; idSede: number; idArea: number; valor: number | null; permiteSinAutorizacion: boolean; onChange: (id: number | null) => void
}) {
  const disponibles = useQuery({ queryKey: [CLAVE, 'autorizaciones', idPersona], queryFn: () => consultarAutorizacionesDisponibles(idPersona) })
  const aplicables = useMemo(() => (disponibles.data ?? []).filter((a) => a.idSede === idSede && a.idArea === idArea), [disponibles.data, idSede, idArea])
  return (
    <div className="vi-autorizacion">
      <Campo etiqueta="Autorización de ingreso" ancho="completo">
        <Selector className="sel-campo" etiqueta="Autorización de ingreso" valor={String(valor ?? '')} onChange={(v) => onChange(v ? Number(v) : null)}
          opciones={[{ valor: '', texto: 'Sin autorización vinculada' },
            ...aplicables.map((a) => ({ valor: String(a.idAutorizacion), texto: `#${a.idAutorizacion} · ${a.modalidad} · ${a.estado} · ${fecha(a.fechaInicio)}${a.fechaFin ? ` – ${fecha(a.fechaFin)}` : ''}` }))]} />
      </Campo>
      {disponibles.isLoading && <p className="ad-tenue">Buscando autorizaciones de la persona…</p>}
      <p className="ad-tenue pe-nota">
        {valor
          ? 'Si la autorización está aprobada la visita quedará Autorizada; si está pendiente, esperará la decisión del responsable.'
          : permiteSinAutorizacion
            ? 'Tu organización permite visitas sin autorización previa: quedará registrada la política aplicada, sin atribuir la aprobación a nadie.'
            : 'Sin autorización la visita quedará Pendiente y no podrá ingresar hasta que se vincule una autorización aprobada.'}
      </p>
    </div>
  )
}

// ---------- Presencia ----------

function useAhora(ms = 30_000) {
  const [ahora, setAhora] = useState(() => Date.now())
  useEffect(() => {
    const id = window.setInterval(() => setAhora(Date.now()), ms)
    return () => window.clearInterval(id)
  }, [ms])
  return ahora
}

function PresenciaSeccion({ pestanas }: { pestanas: ReactNode }) {
  const visitas = useAcciones('visitas')
  const opciones = useOpciones(visitas.consultar)
  const [idSede, setIdSede] = useState('')
  const [detalle, setDetalle] = useState<number | null>(null)
  const { avisar, aviso } = useAviso()
  const ahora = useAhora()
  // La API es la fuente persistida: se vuelve a consultar periódicamente, al enfocar la ventana y al reconectar.
  const presencia = useQuery({
    queryKey: [CLAVE, 'presencia', leerOrganizacionActiva(), idSede],
    queryFn: () => consultarPresencia(idSede),
    refetchInterval: 30_000,
    refetchOnWindowFocus: true,
    refetchOnReconnect: true,
  })
  return (
    <Marco titulo="Personas dentro" migas="Operación" pestanas={pestanas}
      descripcion="Visitantes con una permanencia abierta en cada sede. Se actualiza cada 30 segundos y al volver a la pantalla.">
      <div className="ad-herramientas au-filtros">
        <Filtro etiqueta="Filtrar por sede" valor={idSede} onChange={setIdSede}
          opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(opciones.data?.sedes ?? []).map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
        <span className="vi-contador" role="status">{presencia.data ? `${presencia.data.length} persona${presencia.data.length === 1 ? '' : 's'} dentro` : ''}</span>
      </div>
      <EstadoConsulta loading={presencia.isLoading} error={presencia.error} empty={!presencia.data?.length} vacio="No hay visitantes dentro en este momento." />
      {presencia.data && presencia.data.length > 0 && (
        <ul className="vi-presencia">
          {presencia.data.map((p) => (
            <li key={p.idPermanencia}>
              <button type="button" onClick={() => visitas.consultar && setDetalle(p.idVisita)} disabled={!visitas.consultar} aria-label={`Ver visita de ${p.persona}`}>
                <strong>{p.persona}</strong>
                <small>{p.documentoPersona}</small>
                <span>{p.sede} · {p.area}{p.responsable ? ` · recibe ${p.responsable}` : ''}</span>
                <span className="vi-tiempo">Entró {enSede(p.entradaEn, p.zonaHoraria, { timeStyle: 'short' })} por {p.puntoEntrada} · {duracion((ahora - new Date(p.entradaEn).getTime()) / 1000)}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
      {detalle !== null && <DetalleVisita id={detalle} opciones={opciones.data} onClose={() => setDetalle(null)} onAviso={avisar} />}
      {aviso}
    </Marco>
  )
}
