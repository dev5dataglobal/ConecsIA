import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { consultarDispositivos, consultarPersona, consultarPersonas } from '../personas/apiPersonas'
import { leerOrganizacionActiva } from '../sesion/sesion'
import { BotonIcono, BotonNuevo, BotonesFormulario, Buscador, Campo, Dialogo, Estado, EstadoConsulta, Filtro, Marco } from '../administracion/componentes'
import { mensajeError, useAviso, useDiferido } from '../administracion/utilidades'
import '../administracion/administracion.css'
import '../personas/personas.css'
import './autorizaciones.css'
import {
  consultarAutorizacionObjeto, consultarAutorizacionesObjeto, consultarOpcionesAutorizacion, decidirAutorizacionObjeto, solicitarAutorizacionObjeto,
  type AutorizacionObjeto, type AutorizacionObjetoEntrada, type EstadoAutorizacion, type TipoObjeto,
} from './apiAutorizacionesObjeto'

/*
 * Autorizaciones de ingreso de vehículos y dispositivos. El frontend solo presenta: estados, vigencia, permisos y
 * validaciones los decide el servidor. Aprobar un objeto no autoriza el ingreso de la persona.
 */

const CLAVE = 'autorizaciones-objeto'
const ESTADOS_AUTORIZACION: { valor: string; texto: string }[] = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'Pendiente', texto: 'Pendientes' },
  { valor: 'Aprobada', texto: 'Aprobadas' },
  { valor: 'Rechazada', texto: 'Rechazadas' },
  { valor: 'Revocada', texto: 'Revocadas' },
]
const TIPOS: { valor: string; texto: string }[] = [
  { valor: '', texto: 'Vehículos y dispositivos' },
  { valor: 'Vehiculo', texto: 'Vehículos' },
  { valor: 'Dispositivo', texto: 'Dispositivos' },
]
const VIGENCIA_TEXTO = { Vigente: 'Vigente', Programada: 'Programada', Vencida: 'Vencida', NoAplica: '' } as const

const fecha = (iso: string) => new Date(`${iso}T00:00:00`).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' })
const instante = (iso: string) => new Date(iso).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' })
const hora = (valor: string | null) => valor?.slice(0, 5) ?? ''

export function EstadoAutorizacionEtiqueta({ estado, vigencia }: { estado: EstadoAutorizacion; vigencia?: AutorizacionObjeto['vigencia'] }) {
  const extra = vigencia ? VIGENCIA_TEXTO[vigencia] : ''
  return (
    <span className="au-estado-grupo">
      <span className="ad-estatus au-estado" data-estado={estado}>{estado}</span>
      {extra && <span className="au-vigencia" data-vigencia={vigencia}>{extra}</span>}
    </span>
  )
}

export function AutorizacionesPage() {
  const acciones = useAcciones('autorizaciones')
  const vehiculos = useAcciones('vehiculos')
  const dispositivos = useAcciones('dispositivos')
  const [buscar, setBuscar] = useState('')
  const [estado, setEstado] = useState('')
  const [tipoObjeto, setTipoObjeto] = useState('')
  const [idSede, setIdSede] = useState('')
  const [desde, setDesde] = useState('')
  const [hasta, setHasta] = useState('')
  const [nueva, setNueva] = useState(false)
  const [detalle, setDetalle] = useState<number | null>(null)
  const { avisar, aviso } = useAviso()
  const texto = useDiferido(buscar)
  const opciones = useQuery({ queryKey: [CLAVE, 'opciones', leerOrganizacionActiva()], queryFn: consultarOpcionesAutorizacion, enabled: acciones.consultar, staleTime: 60_000 })
  const lista = useQuery({
    queryKey: [CLAVE, leerOrganizacionActiva(), texto, estado, tipoObjeto, idSede, desde, hasta],
    queryFn: () => consultarAutorizacionesObjeto({ buscar: texto, estado, tipoObjeto, idSede, desde, hasta }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })
  const puedeSolicitar = acciones.anadir && (vehiculos.anadir || dispositivos.anadir)
  const filtrando = Boolean(buscar || estado || tipoObjeto || idSede || desde || hasta)

  return (
    <Marco titulo="Autorizaciones de vehículos y dispositivos" migas="Operación" pestanas={null}
      descripcion="Solicitudes y decisiones de ingreso de vehículos y equipos. Autorizar un objeto no autoriza el ingreso de la persona."
      accion={puedeSolicitar && <BotonNuevo texto="Nueva solicitud" onClick={() => setNueva(true)} />}>
      <div className="ad-herramientas au-filtros">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar autorizaciones" placeholder="Persona, documento, placa o identificador" />
        <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS_AUTORIZACION} />
        <Filtro etiqueta="Filtrar por tipo de objeto" valor={tipoObjeto} onChange={setTipoObjeto} opciones={TIPOS} />
        <Filtro etiqueta="Filtrar por sede" valor={idSede} onChange={setIdSede}
          opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(opciones.data?.sedes ?? []).map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
        <label className="au-fecha">Desde<input type="date" value={desde} onChange={(e) => setDesde(e.target.value)} /></label>
        <label className="au-fecha">Hasta<input type="date" value={hasta} onChange={(e) => setHasta(e.target.value)} /></label>
      </div>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar autorizaciones." />
      ) : (
        <EstadoConsulta loading={lista.isLoading} error={lista.error} empty={!lista.data?.length}
          vacio={filtrando ? 'Ninguna autorización coincide con los filtros.' : 'Aún no hay autorizaciones de vehículos o dispositivos.'} />
      )}
      {lista.data && lista.data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead>
              <tr>
                <th scope="col">Objeto</th>
                <th scope="col">Persona</th>
                <th scope="col">Sede / área</th>
                <th scope="col">Periodo</th>
                <th scope="col">Estado</th>
                <th scope="col"><span className="sr-only">Acciones</span></th>
              </tr>
            </thead>
            <tbody>
              {lista.data.map((a) => (
                <tr key={a.idAutorizacionObjeto}>
                  <td>
                    <strong className={a.tipoObjeto === 'Vehiculo' ? 'pe-placa' : undefined}>{a.objeto ?? '—'}</strong>
                    <small className="au-sub">{a.tipoObjeto === 'Vehiculo' ? 'Vehículo' : 'Dispositivo'}{a.objetoActivo ? '' : ' · inactivo'}</small>
                  </td>
                  <td>{a.persona}<small className="au-sub">{a.documentoPersona}</small></td>
                  <td>{a.sede}{a.area && <small className="au-sub">{a.area}</small>}</td>
                  <td>{fecha(a.fechaInicio)} – {fecha(a.fechaFin)}{a.horaInicio && <small className="au-sub">{hora(a.horaInicio)} a {hora(a.horaFin)}</small>}</td>
                  <td><EstadoAutorizacionEtiqueta estado={a.estado} vigencia={a.vigencia} /></td>
                  <td className="ad-acciones">
                    <BotonIcono etiqueta={`Ver autorización de ${a.objeto ?? 'objeto'}`} icono="flecha" onClick={() => setDetalle(a.idAutorizacionObjeto)} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {nueva && (
        <SolicitudFormulario sedes={opciones.data?.sedes ?? []} areas={opciones.data?.areas ?? []}
          permiteVehiculos={vehiculos.anadir} permiteDispositivos={dispositivos.anadir}
          onClose={() => setNueva(false)} onCreada={(a) => { setNueva(false); avisar('Solicitud registrada. Queda pendiente de decisión.'); setDetalle(a.idAutorizacionObjeto) }} />
      )}
      {detalle !== null && <DetalleAutorizacion id={detalle} puedeDecidir={acciones.editar} onClose={() => setDetalle(null)} onDecidida={avisar} />}
      {aviso}
    </Marco>
  )
}

// ---------- Detalle y decisiones ----------

type Accion = 'aprobar' | 'rechazar' | 'revocar'
const ACCIONES: Record<Accion, { titulo: string; boton: string; exitoso: string; requiereMotivo: boolean }> = {
  aprobar: { titulo: 'Aprobar autorización', boton: 'Aprobar', exitoso: 'Autorización aprobada.', requiereMotivo: false },
  rechazar: { titulo: 'Rechazar autorización', boton: 'Rechazar', exitoso: 'Autorización rechazada.', requiereMotivo: true },
  revocar: { titulo: 'Revocar autorización', boton: 'Revocar', exitoso: 'Autorización revocada.', requiereMotivo: true },
}

function DetalleAutorizacion({ id, puedeDecidir, onClose, onDecidida }: { id: number; puedeDecidir: boolean; onClose: () => void; onDecidida: (mensaje: string) => void }) {
  const queryClient = useQueryClient()
  const detalle = useQuery({ queryKey: [CLAVE, 'detalle', id], queryFn: () => consultarAutorizacionObjeto(id) })
  const [accion, setAccion] = useState<Accion | null>(null)
  const [motivo, setMotivo] = useState('')
  const [errorMotivo, setErrorMotivo] = useState<string | null>(null)
  const decidir = useMutation({
    mutationFn: (a: Accion) => decidirAutorizacionObjeto(id, a, motivo.trim() || null),
    onSuccess: (resultado, a) => {
      // La respuesta ya trae el detalle actualizado; solo se refrescan los listados.
      queryClient.setQueryData([CLAVE, 'detalle', id], resultado)
      void queryClient.invalidateQueries({ queryKey: [CLAVE], predicate: (q) => q.queryKey[1] !== 'detalle' })
      setAccion(null)
      setMotivo('')
      onDecidida(ACCIONES[a].exitoso)
    },
    // Un 409 (otro usuario decidió antes o cambió el estado) deja el detalle desactualizado: se recarga.
    onError: () => void queryClient.invalidateQueries({ queryKey: [CLAVE, 'detalle', id] }),
  })
  const a = detalle.data

  function confirmar() {
    if (!accion) return
    if (ACCIONES[accion].requiereMotivo && !motivo.trim()) { setErrorMotivo('Indica el motivo.'); return }
    setErrorMotivo(null)
    if (!decidir.isPending) decidir.mutate(accion)
  }

  const disponibles: Accion[] = !a || !puedeDecidir ? []
    : a.estado === 'Pendiente' && a.vigencia !== 'Vencida' ? ['aprobar', 'rechazar']
    : a.estado === 'Pendiente' ? ['rechazar']
    : a.estado === 'Aprobada' ? ['revocar']
    : []

  return (
    <Dialogo titulo={a ? `${a.tipoObjeto === 'Vehiculo' ? 'Vehículo' : 'Dispositivo'} ${a.objeto ?? ''}` : 'Autorización'} eyebrow="Autorización de ingreso" icono="autorizaciones" ancho="matriz"
      onClose={onClose} onSubmit={(e) => e.preventDefault()}
      pie={(
        <>
          <button type="button" className="ad-boton-sec" onClick={onClose}>Cerrar</button>
          {accion === null && disponibles.map((x) => (
            <button key={x} type="button" className={x === 'aprobar' ? 'ad-boton' : 'ad-boton-sec au-peligro'} onClick={() => { decidir.reset(); setAccion(x) }}>{ACCIONES[x].boton}</button>
          ))}
        </>
      )}>
      <EstadoConsulta loading={detalle.isLoading} error={detalle.error} empty={false} vacio="" />
      {a && (
        <>
          <div className="pe-resumen">
            <dl>
              <div><dt>Estado</dt><dd><EstadoAutorizacionEtiqueta estado={a.estado} vigencia={a.vigencia} /></dd></div>
              <div><dt>Persona</dt><dd>{a.persona} · {a.documentoPersona}</dd></div>
              <div><dt>Sede</dt><dd>{a.sede}{a.area ? ` · ${a.area}` : ''}</dd></div>
              <div><dt>Periodo</dt><dd>{fecha(a.fechaInicio)} – {fecha(a.fechaFin)}</dd></div>
              <div><dt>Horario</dt><dd>{a.horaInicio ? `${hora(a.horaInicio)} a ${hora(a.horaFin)}` : 'Sin restricción horaria'}</dd></div>
              <div><dt>Solicitada por</dt><dd>{a.solicitadoPor ?? '—'} · {instante(a.solicitadoEn)}</dd></div>
              {a.decididoPor && <div><dt>Decidida por</dt><dd>{a.decididoPor} · {a.decididoEn && instante(a.decididoEn)}</dd></div>}
              {a.motivo && <div data-ancho="completo"><dt>Motivo de la solicitud</dt><dd>{a.motivo}</dd></div>}
              {a.motivoDecision && <div data-ancho="completo"><dt>Motivo de la decisión</dt><dd>{a.motivoDecision}</dd></div>}
            </dl>
          </div>
          {!a.objetoActivo && <p className="ad-mensaje" role="status">El {a.tipoObjeto === 'Vehiculo' ? 'vehículo' : 'dispositivo'} está inactivo: la autorización se conserva como historial.</p>}
          <p className="ad-tenue pe-nota">Esta autorización no habilita por sí sola el ingreso de la persona; su ingreso requiere su propia autorización.</p>
          {accion && (
            <div className="pe-subformulario" role="group" aria-label={ACCIONES[accion].titulo}>
              <strong>{ACCIONES[accion].titulo}</strong>
              <Campo etiqueta={ACCIONES[accion].requiereMotivo ? 'Motivo' : 'Observación (opcional)'} error={errorMotivo ?? undefined}>
                <textarea value={motivo} maxLength={500} autoFocus aria-invalid={Boolean(errorMotivo)} onChange={(e) => setMotivo(e.target.value)} />
              </Campo>
              {decidir.error && <p className="ad-mensaje" role="alert">{mensajeError(decidir.error)}</p>}
              <div className="pe-subacciones">
                <button type="button" className="ad-boton-sec" onClick={() => { setAccion(null); setErrorMotivo(null) }}>Cancelar</button>
                <button type="button" className="ad-boton" onClick={confirmar} disabled={decidir.isPending}>{decidir.isPending ? 'Guardando…' : `Confirmar: ${ACCIONES[accion].boton.toLowerCase()}`}</button>
              </div>
            </div>
          )}
          <section className="pe-seccion" aria-label="Historial de la autorización">
            <header><h3>Historial</h3></header>
            <ol className="au-historial">
              {a.historial.map((t) => (
                <li key={t.idTransicion}>
                  <strong>{t.estadoAnterior ? `${t.estadoAnterior} → ${t.estadoNuevo}` : `Solicitud creada (${t.estadoNuevo})`}</strong>
                  <small>{t.actor ?? 'Usuario desconocido'} · {instante(t.ocurridoEn)}</small>
                  {t.motivo && <small>Motivo: {t.motivo}</small>}
                </li>
              ))}
            </ol>
          </section>
        </>
      )}
    </Dialogo>
  )
}

// ---------- Nueva solicitud ----------

const hoyIso = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

type ErroresSolicitud = Partial<Record<'persona' | 'objeto' | 'sede' | 'fechas' | 'horas', string>>

export function validarSolicitud(d: AutorizacionObjetoEntrada): ErroresSolicitud {
  const errores: ErroresSolicitud = {}
  if (!d.idPersona) errores.persona = 'Selecciona la persona.'
  if (!(d.tipoObjeto === 'Vehiculo' ? d.idVehiculo : d.idDispositivo)) errores.objeto = d.tipoObjeto === 'Vehiculo' ? 'Selecciona el vehículo.' : 'Selecciona el dispositivo.'
  if (!d.idSede) errores.sede = 'Selecciona la sede.'
  if (!d.fechaInicio || !d.fechaFin) errores.fechas = 'Indica el periodo.'
  else if (d.fechaFin < d.fechaInicio) errores.fechas = 'La fecha de fin no puede ser anterior a la de inicio.'
  else if (d.fechaFin < hoyIso()) errores.fechas = 'El periodo ya venció.'
  if (Boolean(d.horaInicio) !== Boolean(d.horaFin)) errores.horas = 'Indica ambas horas o ninguna.'
  else if (d.horaInicio && d.horaFin && d.horaFin <= d.horaInicio) errores.horas = 'La hora de fin debe ser posterior a la de inicio.'
  return errores
}

function SolicitudFormulario({ sedes, areas, permiteVehiculos, permiteDispositivos, onClose, onCreada }: {
  sedes: { id: number; nombre: string }[]
  areas: { id: number; idSede: number; nombre: string }[]
  permiteVehiculos: boolean
  permiteDispositivos: boolean
  onClose: () => void
  onCreada: (a: AutorizacionObjeto) => void
}) {
  const [datos, setDatos] = useState<AutorizacionObjetoEntrada>({
    idPersona: 0, tipoObjeto: permiteVehiculos ? 'Vehiculo' : 'Dispositivo', idVehiculo: null, idDispositivo: null,
    idSede: sedes.length === 1 ? sedes[0]!.id : 0, idArea: null, fechaInicio: hoyIso(), fechaFin: hoyIso(), horaInicio: null, horaFin: null, motivo: null,
  })
  const [buscar, setBuscar] = useState('')
  const [errores, setErrores] = useState<ErroresSolicitud>({})
  const texto = useDiferido(buscar)
  const personas = useQuery({
    queryKey: ['personas', 'selector', texto],
    queryFn: () => consultarPersonas({ buscar: texto, estado: 'Activo' }),
    enabled: texto.trim().length >= 2 && !datos.idPersona,
  })
  const persona = useQuery({ queryKey: ['persona', datos.idPersona], queryFn: () => consultarPersona(datos.idPersona), enabled: datos.idPersona > 0 })
  const equipos = useQuery({
    queryKey: ['dispositivos', datos.idPersona],
    queryFn: () => consultarDispositivos(datos.idPersona),
    enabled: datos.idPersona > 0 && datos.tipoObjeto === 'Dispositivo',
  })
  const queryClient = useQueryClient()
  const crear = useMutation({
    mutationFn: () => solicitarAutorizacionObjeto({ ...datos, motivo: datos.motivo?.trim() || null }),
    onSuccess: (creada) => {
      void queryClient.invalidateQueries({ queryKey: [CLAVE], predicate: (q) => q.queryKey[1] !== 'detalle' })
      onCreada(creada)
    },
  })
  const cambiar = (parcial: Partial<AutorizacionObjetoEntrada>) => setDatos((d) => ({ ...d, ...parcial }))

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos = validarSolicitud(datos)
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !crear.isPending) crear.mutate()
  }

  const objetos = datos.tipoObjeto === 'Vehiculo'
    ? (persona.data?.vehiculos ?? []).filter((v) => v.activo).map((v) => ({ valor: String(v.idVehiculo), texto: `${v.placa}${v.tipo ? ` · ${v.tipo}` : ''}` }))
    : (equipos.data ?? []).filter((d) => d.activo).map((d) => ({ valor: String(d.idDispositivo), texto: `${d.tipoDispositivo ?? 'Dispositivo'} · ${d.codigoIdentificador}` }))
  const cargandoObjetos = datos.tipoObjeto === 'Vehiculo' ? persona.isLoading : equipos.isLoading
  const idObjeto = datos.tipoObjeto === 'Vehiculo' ? datos.idVehiculo : datos.idDispositivo
  const tipos: { valor: TipoObjeto; texto: string }[] = [
    ...(permiteVehiculos ? [{ valor: 'Vehiculo' as const, texto: 'Vehículo' }] : []),
    ...(permiteDispositivos ? [{ valor: 'Dispositivo' as const, texto: 'Dispositivo' }] : []),
  ]

  return (
    <Dialogo titulo="Nueva solicitud de autorización" eyebrow="Vehículos y dispositivos" icono="autorizaciones" ancho="amplio" onClose={onClose} onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={crear.isPending} texto="Solicitar" />}>
      <Campo etiqueta="Persona" error={errores.persona} ancho="completo">
        {datos.idPersona && persona.data ? (
          <div className="pe-existente">
            <strong>{persona.data.nombres} {persona.data.apellidos}</strong>
            <span className="ad-tenue">{persona.data.tipoDocumento} {persona.data.numeroDocumento}</span>
            <button type="button" className="ad-enlace" onClick={() => { cambiar({ idPersona: 0, idVehiculo: null, idDispositivo: null }); setBuscar('') }}>Cambiar</button>
          </div>
        ) : (
          <div className="au-buscador-persona">
            <input value={buscar} onChange={(e) => setBuscar(e.target.value)} placeholder="Nombre o documento (mínimo 2 caracteres)" aria-label="Buscar persona" aria-invalid={Boolean(errores.persona)} />
            {personas.data && (
              <ul role="listbox" aria-label="Personas encontradas">
                {personas.data.length === 0 && <li className="ad-tenue">Sin coincidencias entre personas activas.</li>}
                {personas.data.slice(0, 8).map((p) => (
                  <li key={p.idPersona}>
                    <button type="button" role="option" aria-selected={false} onClick={() => cambiar({ idPersona: p.idPersona, idVehiculo: null, idDispositivo: null })}>
                      {p.nombres} {p.apellidos} <small>{p.tipoDocumento} {p.numeroDocumento}</small>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </Campo>
      <div className="ad-rejilla">
        <Campo etiqueta="Tipo de objeto">
          <Selector className="sel-campo" etiqueta="Tipo de objeto" valor={datos.tipoObjeto} opciones={tipos}
            onChange={(v) => cambiar({ tipoObjeto: v as TipoObjeto, idVehiculo: null, idDispositivo: null })} />
        </Campo>
        <Campo etiqueta={datos.tipoObjeto === 'Vehiculo' ? 'Vehículo' : 'Dispositivo'} error={errores.objeto}>
          <Selector className="sel-campo" etiqueta={datos.tipoObjeto === 'Vehiculo' ? 'Vehículo' : 'Dispositivo'} valor={String(idObjeto ?? '')} invalido={Boolean(errores.objeto)}
            onChange={(v) => cambiar(datos.tipoObjeto === 'Vehiculo' ? { idVehiculo: Number(v) } : { idDispositivo: Number(v) })}
            opciones={[{ valor: '', texto: !datos.idPersona ? 'Primero elige la persona' : cargandoObjetos ? 'Cargando…' : objetos.length ? 'Selecciona…' : 'La persona no tiene objetos activos de este tipo', deshabilitada: true }, ...objetos]} />
        </Campo>
        <Campo etiqueta="Sede" error={errores.sede}>
          <Selector className="sel-campo" etiqueta="Sede" valor={String(datos.idSede || '')} invalido={Boolean(errores.sede)}
            onChange={(v) => cambiar({ idSede: Number(v), idArea: null })}
            opciones={[{ valor: '', texto: sedes.length ? 'Selecciona…' : 'No hay sedes activas', deshabilitada: true }, ...sedes.map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
        </Campo>
        <Campo etiqueta="Área o destino (opcional)">
          <Selector className="sel-campo" etiqueta="Área o destino" valor={String(datos.idArea ?? '')} onChange={(v) => cambiar({ idArea: v ? Number(v) : null })}
            opciones={[{ valor: '', texto: 'Sin área específica' }, ...areas.filter((x) => x.idSede === datos.idSede).map((x) => ({ valor: String(x.id), texto: x.nombre }))]} />
        </Campo>
        <Campo etiqueta="Desde" error={errores.fechas}>
          <input type="date" value={datos.fechaInicio} min={hoyIso()} aria-invalid={Boolean(errores.fechas)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
        </Campo>
        <Campo etiqueta="Hasta">
          <input type="date" value={datos.fechaFin} min={datos.fechaInicio || hoyIso()} onChange={(e) => cambiar({ fechaFin: e.target.value })} />
        </Campo>
        <Campo etiqueta="Hora de inicio (opcional)" error={errores.horas}>
          <input type="time" value={datos.horaInicio ?? ''} aria-invalid={Boolean(errores.horas)} onChange={(e) => cambiar({ horaInicio: e.target.value || null })} />
        </Campo>
        <Campo etiqueta="Hora de fin (opcional)">
          <input type="time" value={datos.horaFin ?? ''} onChange={(e) => cambiar({ horaFin: e.target.value || null })} />
        </Campo>
        <Campo etiqueta="Motivo (opcional)" ancho="completo">
          <textarea value={datos.motivo ?? ''} maxLength={500} onChange={(e) => cambiar({ motivo: e.target.value || null })} />
        </Campo>
      </div>
      <p className="ad-tenue pe-nota">La solicitud queda pendiente hasta que un usuario facultado la apruebe o rechace. No autoriza el ingreso de la persona.</p>
      {crear.error && <p className="ad-mensaje" role="alert">{mensajeError(crear.error, 'No se pudo registrar la solicitud.')}</p>}
    </Dialogo>
  )
}
