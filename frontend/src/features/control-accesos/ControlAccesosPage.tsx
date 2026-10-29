import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { Selector } from '../../shared/ui/Selector'
import { useAcciones } from '../modulos/apiModulos'
import { consultarPersona, consultarPersonas } from '../personas/apiPersonas'
import { leerOrganizacionActiva } from '../sesion/sesion'
import { BotonIcono, BotonNuevo, BotonesFormulario, Buscador, Campo, Dialogo, Estado, EstadoConsulta, Filtro, Marco } from '../administracion/componentes'
import { mensajeError, useAviso, useDiferido } from '../administracion/utilidades'
import '../administracion/administracion.css'
import '../personas/personas.css'
import '../autorizaciones/autorizaciones.css'
import './controlAccesos.css'
import {
  DIAS, MODALIDADES, consultarAutorizacionAcceso, consultarAutorizacionesAcceso, consultarOpcionesAcceso, decidirAutorizacionAcceso,
  solicitarAutorizacionAcceso, textoModalidad,
  type AutorizacionAcceso, type AutorizacionAccesoEntrada, type EstadoAutorizacionAcceso, type Modalidad, type OpcionesAutorizacionAcceso, type VigenciaAcceso,
} from './apiControlAccesos'

/*
 * Control de accesos: solicitudes de autorización de ingreso de personas ya registradas. Se llega desde «Gestionar
 * acceso» en Personas (?persona=ID). Toda solicitud nace pendiente; aprobar o rechazar exige Autorizaciones · Editar.
 * Estados, vigencia, cupo y permisos los decide el servidor.
 */

const CLAVE = 'autorizaciones-acceso'
const ESTADOS: { valor: string; texto: string }[] = [
  { valor: '', texto: 'Todos los estados' },
  { valor: 'Pendiente', texto: 'Pendientes de aprobación' },
  { valor: 'Aprobada', texto: 'Aprobadas' },
  { valor: 'Rechazada', texto: 'Rechazadas' },
]
const TEXTO_ESTADO: Record<EstadoAutorizacionAcceso, string> = {
  Pendiente: 'Pendiente de aprobación', Aprobada: 'Aprobada', Rechazada: 'Rechazada', Cancelada: 'Cancelada', Revocada: 'Revocada', Vencida: 'Vencida', Agotada: 'Agotada',
}
const TEXTO_VIGENCIA: Record<VigenciaAcceso, string> = { Vigente: 'Vigente', Programada: 'Programada', Vencida: 'Vencida', Consumida: 'Consumida', NoAplica: '' }

const fecha = (iso: string) => new Date(`${iso}T00:00:00`).toLocaleDateString('es-CO', { day: '2-digit', month: 'short', year: 'numeric' })
const instante = (iso: string) => new Date(iso).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' })
const hora = (valor: string | null) => valor?.slice(0, 5) ?? ''
const hoyIso = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Resumen legible de las condiciones según la modalidad. */
export function condiciones(a: Pick<AutorizacionAcceso, 'modalidad' | 'fechaInicio' | 'fechaFin' | 'horaInicio' | 'horaFin' | 'horarios'>) {
  const franja = a.horaInicio ? ` · ${hora(a.horaInicio)} a ${hora(a.horaFin)}` : ''
  switch (a.modalidad) {
    case 'Unica':
    case 'Diaria':
      return `${fecha(a.fechaInicio)}${franja || ' · todo el día'}`
    case 'Temporal':
      return `${fecha(a.fechaInicio)} ${hora(a.horaInicio)} → ${a.fechaFin ? fecha(a.fechaFin) : ''} ${hora(a.horaFin)}`
    case 'Recurrente': {
      const dias = a.horarios.map((h) => DIAS.find((d) => d.valor === h.diaSemana)?.corto ?? '').join(' ')
      const primero = a.horarios[0]
      return `${dias}${primero ? ` · ${hora(primero.horaInicio)} a ${hora(primero.horaFin)}` : ''} · desde ${fecha(a.fechaInicio)}${a.fechaFin ? ` hasta ${fecha(a.fechaFin)}` : ''}`
    }
    default:
      return `Desde ${fecha(a.fechaInicio)}, sin fecha final`
  }
}

export function EstadoAccesoEtiqueta({ estado, vigencia }: { estado: EstadoAutorizacionAcceso; vigencia?: VigenciaAcceso }) {
  const extra = vigencia ? TEXTO_VIGENCIA[vigencia] : ''
  return (
    <span className="au-estado-grupo">
      <span className="ad-estatus au-estado" data-estado={estado}>{TEXTO_ESTADO[estado] ?? estado}</span>
      {extra && <span className="au-vigencia" data-vigencia={vigencia}>{extra}</span>}
    </span>
  )
}

export function ControlAccesosPage() {
  const acciones = useAcciones('control_accesos')
  const decisiones = useAcciones('autorizaciones')
  const [parametros, setParametros] = useSearchParams()
  const [buscar, setBuscar] = useState('')
  const [estado, setEstado] = useState('')
  const [modalidad, setModalidad] = useState('')
  const [nueva, setNueva] = useState<{ idPersona: number | null } | null>(null)
  const [detalle, setDetalle] = useState<number | null>(null)
  const { avisar, aviso } = useAviso()
  const texto = useDiferido(buscar)
  const opciones = useQuery({ queryKey: [CLAVE, 'opciones', leerOrganizacionActiva()], queryFn: consultarOpcionesAcceso, enabled: acciones.consultar, staleTime: 60_000 })
  const lista = useQuery({
    queryKey: [CLAVE, leerOrganizacionActiva(), texto, estado, modalidad],
    queryFn: () => consultarAutorizacionesAcceso({ buscar: texto, estado, modalidad }),
    enabled: acciones.consultar,
    placeholderData: keepPreviousData,
  })

  // «Gestionar acceso» desde Personas (?persona=ID): la solicitud se abre con la persona identificada mientras el
  // parámetro esté en la URL; al cerrarla o enviarla se quita, así recargar no vuelve a abrirla.
  const personaSolicitada = Number(parametros.get('persona')) || null
  const solicitud = nueva ?? (personaSolicitada && acciones.anadir ? { idPersona: personaSolicitada } : null)
  function cerrarSolicitud() {
    setNueva(null)
    if (personaSolicitada) setParametros((p) => { p.delete('persona'); return p }, { replace: true })
  }

  const filtrando = Boolean(buscar || estado || modalidad)

  return (
    <Marco titulo="Control de accesos" migas="Operación" pestanas={null}
      descripcion="Solicitudes de autorización de ingreso de personas registradas. Toda solicitud queda pendiente hasta que un administrador la apruebe; una pendiente o rechazada no habilita el ingreso."
      accion={acciones.anadir && <BotonNuevo texto="Solicitar acceso" onClick={() => setNueva({ idPersona: null })} />}>
      <div className="ad-herramientas au-filtros">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar solicitudes" placeholder="Persona o documento" />
        <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS} />
        <Filtro etiqueta="Filtrar por tipo de autorización" valor={modalidad} onChange={setModalidad}
          opciones={[{ valor: '', texto: 'Todos los tipos' }, ...MODALIDADES.map((m) => ({ valor: m.valor, texto: m.texto }))]} />
      </div>
      {!acciones.consultar ? (
        <Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar el control de accesos." />
      ) : (
        <EstadoConsulta loading={lista.isLoading} error={lista.error} empty={!lista.data?.length}
          vacio={filtrando ? 'Ninguna solicitud coincide con los filtros.' : 'Aún no hay solicitudes de acceso. Usa «Gestionar acceso» desde Personas o «Solicitar acceso».'} />
      )}
      {lista.data && lista.data.length > 0 && (
        <div className="ad-tabla-wrap">
          <table className="ad-tabla">
            <thead>
              <tr>
                <th scope="col">Persona</th>
                <th scope="col">Tipo</th>
                <th scope="col">Condiciones</th>
                <th scope="col">Sede / destino</th>
                <th scope="col">Estado</th>
                <th scope="col"><span className="sr-only">Acciones</span></th>
              </tr>
            </thead>
            <tbody>
              {lista.data.map((a) => (
                <tr key={a.idAutorizacion}>
                  <td><strong>{a.persona}</strong><small className="au-sub">{a.documentoPersona}{a.personaActiva ? '' : ' · inactiva'}</small></td>
                  <td>{textoModalidad(a.modalidad)}</td>
                  <td>{condiciones(a)}</td>
                  <td>{a.sede}<small className="au-sub">{a.area}</small></td>
                  <td><EstadoAccesoEtiqueta estado={a.estado} vigencia={a.vigencia} /></td>
                  <td className="ad-acciones">
                    <BotonIcono etiqueta={`Ver solicitud de ${a.persona}`} icono="flecha" onClick={() => setDetalle(a.idAutorizacion)} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {solicitud && opciones.data && (
        <SolicitudAcceso key={solicitud.idPersona ?? 'nueva'} opciones={opciones.data} idPersonaInicial={solicitud.idPersona}
          onClose={cerrarSolicitud}
          onCreada={(a) => { cerrarSolicitud(); avisar('Solicitud registrada. Queda pendiente de aprobación.'); setDetalle(a.idAutorizacion) }} />
      )}
      {detalle !== null && <DetalleSolicitud id={detalle} puedeDecidir={decisiones.editar} onClose={() => setDetalle(null)} onDecidida={avisar} />}
      {aviso}
    </Marco>
  )
}

// ---------- Detalle y decisión ----------

type Accion = 'aprobar' | 'rechazar'
const ACCIONES: Record<Accion, { titulo: string; boton: string; exitoso: string; requiereMotivo: boolean }> = {
  aprobar: { titulo: 'Aprobar solicitud', boton: 'Aprobar', exitoso: 'Solicitud aprobada. La autorización rige según sus fechas y horarios.', requiereMotivo: false },
  rechazar: { titulo: 'Rechazar solicitud', boton: 'Rechazar', exitoso: 'Solicitud rechazada. Se conserva en el historial.', requiereMotivo: true },
}

function DetalleSolicitud({ id, puedeDecidir, onClose, onDecidida }: { id: number; puedeDecidir: boolean; onClose: () => void; onDecidida: (mensaje: string) => void }) {
  const queryClient = useQueryClient()
  const detalle = useQuery({ queryKey: [CLAVE, 'detalle', id], queryFn: () => consultarAutorizacionAcceso(id) })
  const [accion, setAccion] = useState<Accion | null>(null)
  const [motivo, setMotivo] = useState('')
  const [errorMotivo, setErrorMotivo] = useState<string | null>(null)
  const decidir = useMutation({
    mutationFn: (a: Accion) => decidirAutorizacionAcceso(id, a, motivo.trim() || null),
    onSuccess: (resultado, a) => {
      queryClient.setQueryData([CLAVE, 'detalle', id], resultado)
      void queryClient.invalidateQueries({ queryKey: [CLAVE], predicate: (q) => q.queryKey[1] !== 'detalle' })
      setAccion(null)
      setMotivo('')
      onDecidida(ACCIONES[a].exitoso)
    },
    // Un 409 (otro usuario decidió antes) deja el detalle desactualizado: se recarga.
    onError: () => void queryClient.invalidateQueries({ queryKey: [CLAVE, 'detalle', id] }),
  })
  const a = detalle.data

  function confirmar() {
    if (!accion) return
    if (ACCIONES[accion].requiereMotivo && !motivo.trim()) { setErrorMotivo('Indica el motivo del rechazo.'); return }
    setErrorMotivo(null)
    if (!decidir.isPending) decidir.mutate(accion)
  }

  const disponibles: Accion[] = a && puedeDecidir && a.estado === 'Pendiente' ? ['aprobar', 'rechazar'] : []

  return (
    <Dialogo titulo={a ? a.persona : 'Solicitud de acceso'} eyebrow={a ? `Autorización ${textoModalidad(a.modalidad).toLowerCase()} #${a.idAutorizacion}` : 'Solicitud de acceso'}
      icono="accesos" ancho="matriz" onClose={onClose} onSubmit={(e) => e.preventDefault()}
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
              <div><dt>Estado</dt><dd><EstadoAccesoEtiqueta estado={a.estado} vigencia={a.vigencia} /></dd></div>
              <div><dt>Persona</dt><dd>{a.persona} · {a.documentoPersona}</dd></div>
              <div><dt>Tipo de autorización</dt><dd>{textoModalidad(a.modalidad)}{a.cupoIngresos ? ` · ${a.ingresosConsumidos}/${a.cupoIngresos} ingreso${a.cupoIngresos === 1 ? '' : 's'}` : ''}</dd></div>
              <div><dt>Condiciones</dt><dd>{condiciones(a)}</dd></div>
              <div><dt>Sede y destino</dt><dd>{a.sede} · {a.area}</dd></div>
              <div><dt>Responsable</dt><dd>{a.responsable ?? '—'}</dd></div>
              <div><dt>Zonas</dt><dd>{a.zonas.length ? a.zonas.map((z) => z.nombre).join(', ') : 'Sin restricción por zona'}</dd></div>
              <div><dt>Solicitada por</dt><dd>{a.solicitadoPor ?? '—'} · {instante(a.solicitadoEn)}</dd></div>
              {a.decididoPor && <div><dt>{a.estado === 'Rechazada' ? 'Rechazada por' : 'Decidida por'}</dt><dd>{a.decididoPor} · {a.decididoEn && instante(a.decididoEn)}</dd></div>}
              {a.motivo && <div data-ancho="completo"><dt>Motivo de la solicitud</dt><dd>{a.motivo}</dd></div>}
              {a.observacion && <div data-ancho="completo"><dt>Observación</dt><dd>{a.observacion}</dd></div>}
              {a.motivoDecision && <div data-ancho="completo"><dt>{a.estado === 'Rechazada' ? 'Motivo del rechazo' : 'Observación de la decisión'}</dt><dd>{a.motivoDecision}</dd></div>}
            </dl>
          </div>
          {a.estado === 'Pendiente' && (
            <p className="ad-tenue pe-nota">
              Pendiente de aprobación: no habilita el ingreso.{!puedeDecidir && ' La decide un administrador de la organización.'}
            </p>
          )}
          {accion && (
            <div className="pe-subformulario" role="group" aria-label={ACCIONES[accion].titulo}>
              <strong>{ACCIONES[accion].titulo}</strong>
              <Campo etiqueta={ACCIONES[accion].requiereMotivo ? 'Motivo del rechazo' : 'Observación (opcional)'} error={errorMotivo ?? undefined}>
                <textarea value={motivo} maxLength={500} autoFocus aria-invalid={Boolean(errorMotivo)} onChange={(e) => setMotivo(e.target.value)} />
              </Campo>
              {decidir.error && <p className="ad-mensaje" role="alert">{mensajeError(decidir.error)}</p>}
              <div className="pe-subacciones">
                <button type="button" className="ad-boton-sec" onClick={() => { setAccion(null); setErrorMotivo(null) }}>Cancelar</button>
                <button type="button" className="ad-boton" onClick={confirmar} disabled={decidir.isPending}>{decidir.isPending ? 'Guardando…' : `Confirmar: ${ACCIONES[accion].boton.toLowerCase()}`}</button>
              </div>
            </div>
          )}
          <section className="pe-seccion" aria-label="Historial de la solicitud">
            <header><h3>Historial</h3></header>
            <ol className="au-historial">
              {a.historial.map((t) => (
                <li key={t.idTransicion}>
                  <strong>{t.estadoAnterior ? `${TEXTO_ESTADO[t.estadoAnterior as EstadoAutorizacionAcceso] ?? t.estadoAnterior} → ${TEXTO_ESTADO[t.estadoNuevo as EstadoAutorizacionAcceso] ?? t.estadoNuevo}` : 'Solicitud creada (pendiente de aprobación)'}</strong>
                  <small>{t.actor ?? 'Usuario desconocido'} · {instante(t.ocurridoEn)}</small>
                  {t.motivo && <small>{t.estadoAnterior ? 'Motivo' : 'Motivo de la solicitud'}: {t.motivo}</small>}
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

type Datos = Omit<AutorizacionAccesoEntrada, 'idPersona'>
type ErroresAcceso = Partial<Record<'persona' | 'sede' | 'area' | 'responsable' | 'modalidad' | 'fechaInicio' | 'fechaFin' | 'horas' | 'dias' | 'motivo', string>>

/** Validación de cortesía por modalidad (el servidor vuelve a validar todo). Devuelve solo los campos que aplican. */
export function validarSolicitudAcceso(idPersona: number, d: Datos, permitePermanente: boolean, hoy = hoyIso()): ErroresAcceso {
  const e: ErroresAcceso = {}
  if (!idPersona) e.persona = 'Selecciona la persona.'
  if (!d.idSede) e.sede = 'Selecciona la sede.'
  if (!d.idArea) e.area = 'Selecciona el área o destino.'
  if (!d.idMembresiaResponsable) e.responsable = 'Selecciona el responsable.'
  if (!d.motivo.trim()) e.motivo = 'Indica el motivo de la solicitud.'
  if (d.modalidad === 'Permanente' && !permitePermanente) e.modalidad = 'La política de la organización no permite autorizaciones permanentes.'
  if (!d.fechaInicio) e.fechaInicio = d.modalidad === 'Unica' || d.modalidad === 'Diaria' ? 'Indica el día.' : 'Indica la fecha de inicio.'
  switch (d.modalidad) {
    case 'Unica':
    case 'Diaria':
      if (d.fechaInicio && d.fechaInicio < hoy) e.fechaInicio = 'El día no puede ser anterior a hoy.'
      if (Boolean(d.horaInicio) !== Boolean(d.horaFin)) e.horas = 'Indica ambas horas de la franja o ninguna.'
      else if (d.horaInicio && d.horaFin && d.horaFin <= d.horaInicio) e.horas = 'La hora de fin debe ser posterior a la de inicio.'
      break
    case 'Temporal':
      if (!d.horaInicio || !d.horaFin) e.horas = 'Indica la hora de inicio y la de finalización.'
      if (!d.fechaFin) e.fechaFin = 'Indica la fecha de finalización.'
      else if (d.fechaInicio && d.horaInicio && d.horaFin && `${d.fechaFin}T${d.horaFin}` <= `${d.fechaInicio}T${d.horaInicio}`)
        e.fechaFin = 'La finalización debe ser posterior al inicio.'
      break
    case 'Recurrente':
      if (d.diasSemana.length === 0) e.dias = 'Selecciona al menos un día.'
      if (!d.horaInicio || !d.horaFin) e.horas = 'Indica el horario.'
      else if (d.horaFin <= d.horaInicio) e.horas = 'La hora de fin debe ser posterior a la de inicio.'
      if (d.fechaFin && d.fechaInicio && d.fechaFin < d.fechaInicio) e.fechaFin = 'El fin del periodo no puede ser anterior al inicio.'
      else if (d.fechaFin && d.fechaFin < hoy) e.fechaFin = 'El periodo ya terminó.'
      break
  }
  return e
}

/** Solo los campos de la modalidad elegida viajan al servidor. */
function cuerpo(idPersona: number, d: Datos): AutorizacionAccesoEntrada {
  const base = { ...d, idPersona, motivo: d.motivo.trim(), observacion: d.observacion?.trim() || null }
  switch (d.modalidad) {
    case 'Unica':
    case 'Diaria': return { ...base, fechaFin: null, diasSemana: [] }
    case 'Temporal': return { ...base, diasSemana: [] }
    case 'Recurrente': return base
    default: return { ...base, fechaFin: null, horaInicio: null, horaFin: null, diasSemana: [] }
  }
}

function SolicitudAcceso({ opciones, idPersonaInicial, onClose, onCreada }: {
  opciones: OpcionesAutorizacionAcceso
  idPersonaInicial: number | null
  onClose: () => void
  onCreada: (a: AutorizacionAcceso) => void
}) {
  const [idPersona, setIdPersona] = useState(idPersonaInicial ?? 0)
  const [datos, setDatos] = useState<Datos>({
    idSede: opciones.sedes.length === 1 ? opciones.sedes[0]!.id : 0, idArea: 0, idMembresiaResponsable: 0,
    modalidad: 'Unica', fechaInicio: hoyIso(), fechaFin: null, horaInicio: null, horaFin: null, diasSemana: [], zonas: [], motivo: '', observacion: null,
  })
  const [buscar, setBuscar] = useState('')
  const [errores, setErrores] = useState<ErroresAcceso>({})
  const texto = useDiferido(buscar)
  const personas = useQuery({
    queryKey: ['personas', 'selector', texto],
    queryFn: () => consultarPersonas({ buscar: texto, estado: 'Activo' }),
    enabled: texto.trim().length >= 2 && !idPersona,
  })
  const persona = useQuery({ queryKey: ['persona', idPersona], queryFn: () => consultarPersona(idPersona), enabled: idPersona > 0 })
  const queryClient = useQueryClient()
  const crear = useMutation({
    mutationFn: () => solicitarAutorizacionAcceso(cuerpo(idPersona, datos)),
    onSuccess: (creada) => {
      void queryClient.invalidateQueries({ queryKey: [CLAVE], predicate: (q) => q.queryKey[1] !== 'detalle' && q.queryKey[1] !== 'opciones' })
      onCreada(creada)
    },
  })
  const cambiar = (parcial: Partial<Datos>) => setDatos((d) => ({ ...d, ...parcial }))
  const areas = opciones.areas.filter((x) => x.idSede === datos.idSede)
  const zonas = opciones.zonas.filter((x) => x.idSede === datos.idSede)
  // Si el destino tiene responsables designados, solo ellos; si no, cualquier usuario activo. Por defecto, quien solicita.
  const designados = opciones.responsables.filter((r) => r.areas.includes(datos.idArea))
  const responsables = designados.length ? designados : opciones.responsables
  useEffect(() => {
    if (datos.idMembresiaResponsable || !opciones.idMembresiaPropia) return
    if (responsables.some((r) => r.idMembresia === opciones.idMembresiaPropia)) cambiar({ idMembresiaResponsable: opciones.idMembresiaPropia })
  }, [datos.idMembresiaResponsable, opciones.idMembresiaPropia, responsables])

  function elegirModalidad(m: Modalidad) {
    setErrores({})
    // Al cambiar de tipo se limpian los campos que no le aplican para no enviar condiciones ocultas.
    cambiar({ modalidad: m, fechaFin: null, horaInicio: null, horaFin: null, diasSemana: m === 'Recurrente' ? [1, 2, 3, 4, 5] : [] })
  }

  function enviar(event: FormEvent) {
    event.preventDefault()
    const nuevos = validarSolicitudAcceso(idPersona, datos, opciones.permitePermanente)
    setErrores(nuevos)
    if (Object.keys(nuevos).length === 0 && !crear.isPending) crear.mutate()
  }

  const p = persona.data
  const unDia = datos.modalidad === 'Unica' || datos.modalidad === 'Diaria'

  return (
    <Dialogo titulo="Solicitar acceso" eyebrow="Control de accesos" icono="accesos" ancho="amplio" onClose={onClose} onSubmit={enviar}
      pie={<BotonesFormulario onCancelar={onClose} pendiente={crear.isPending} texto="Enviar solicitud" />}>
      <Campo etiqueta="Persona" error={errores.persona} ancho="completo">
        {idPersona && p ? (
          <div className="pe-existente">
            <strong>{p.nombres} {p.apellidos}</strong>
            <span className="ad-tenue">{p.tipoDocumento} {p.numeroDocumento}{p.tipoPersona ? ` · ${p.tipoPersona}` : ''}</span>
            {!p.activo && <span className="ad-mensaje" role="alert">Persona inactiva: no se puede solicitar su acceso.</span>}
            <button type="button" className="ad-enlace" onClick={() => { setIdPersona(0); setBuscar('') }}>Cambiar</button>
          </div>
        ) : idPersona && persona.isLoading ? (
          <span className="ad-tenue">Cargando persona…</span>
        ) : (
          <div className="au-buscador-persona">
            {idPersona > 0 && persona.error && <p className="ad-mensaje" role="alert">{mensajeError(persona.error, 'No se encontró la persona.')}</p>}
            <input value={buscar} onChange={(e) => { setBuscar(e.target.value); if (idPersona) setIdPersona(0) }} placeholder="Nombre o documento (mínimo 2 caracteres)" aria-label="Buscar persona" aria-invalid={Boolean(errores.persona)} />
            {personas.data && (
              <ul role="listbox" aria-label="Personas encontradas">
                {personas.data.length === 0 && <li className="ad-tenue">Sin coincidencias entre personas activas. Regístrala primero en Personas.</li>}
                {personas.data.slice(0, 8).map((x) => (
                  <li key={x.idPersona}>
                    <button type="button" role="option" aria-selected={false} onClick={() => setIdPersona(x.idPersona)}>
                      {x.nombres} {x.apellidos} <small>{x.tipoDocumento} {x.numeroDocumento}</small>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </Campo>

      <fieldset className="ca-modalidades">
        <legend>Tipo de autorización</legend>
        <div className="ca-modalidades-opciones">
          {MODALIDADES.map((m) => {
            const bloqueada = m.valor === 'Permanente' && !opciones.permitePermanente
            return (
              <label key={m.valor} className="ca-modalidad" data-bloqueada={bloqueada || undefined}>
                <input type="radio" name="modalidad" value={m.valor} checked={datos.modalidad === m.valor} disabled={bloqueada} onChange={() => elegirModalidad(m.valor)} />
                <strong>{m.texto}</strong>
                <small>{bloqueada ? 'No disponible: la política de la organización no la permite.' : m.descripcion}</small>
              </label>
            )
          })}
        </div>
        {errores.modalidad && <p className="ad-mensaje" role="alert">{errores.modalidad}</p>}
      </fieldset>

      <div className="ad-rejilla">
        <Campo etiqueta="Sede" error={errores.sede}>
          <Selector className="sel-campo" etiqueta="Sede" valor={String(datos.idSede || '')} invalido={Boolean(errores.sede)}
            onChange={(v) => cambiar({ idSede: Number(v), idArea: 0, zonas: [] })}
            opciones={[{ valor: '', texto: opciones.sedes.length ? 'Selecciona…' : 'No hay sedes en tu alcance', deshabilitada: true }, ...opciones.sedes.map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
        </Campo>
        <Campo etiqueta="Área o destino" error={errores.area}>
          <Selector className="sel-campo" etiqueta="Área o destino" valor={String(datos.idArea || '')} invalido={Boolean(errores.area)}
            onChange={(v) => cambiar({ idArea: Number(v) })}
            opciones={[{ valor: '', texto: !datos.idSede ? 'Primero elige la sede' : areas.length ? 'Selecciona…' : 'La sede no tiene áreas activas', deshabilitada: true }, ...areas.map((x) => ({ valor: String(x.id), texto: x.nombre }))]} />
        </Campo>
        <Campo etiqueta="Responsable" error={errores.responsable} ancho="completo">
          <Selector className="sel-campo" etiqueta="Responsable" valor={String(datos.idMembresiaResponsable || '')} invalido={Boolean(errores.responsable)} buscable
            onChange={(v) => cambiar({ idMembresiaResponsable: Number(v) })}
            opciones={[{ valor: '', texto: 'Selecciona…', deshabilitada: true }, ...responsables.map((r) => ({ valor: String(r.idMembresia), texto: r.nombre }))]} />
        </Campo>

        {unDia && (
          <>
            <Campo etiqueta="Día" error={errores.fechaInicio} ancho="completo">
              <input type="date" value={datos.fechaInicio} min={hoyIso()} aria-invalid={Boolean(errores.fechaInicio)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
            </Campo>
            <Campo etiqueta="Desde la hora (opcional)" error={errores.horas}>
              <input type="time" value={datos.horaInicio ?? ''} aria-invalid={Boolean(errores.horas)} onChange={(e) => cambiar({ horaInicio: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Hasta la hora (opcional)">
              <input type="time" value={datos.horaFin ?? ''} onChange={(e) => cambiar({ horaFin: e.target.value || null })} />
            </Campo>
          </>
        )}
        {datos.modalidad === 'Temporal' && (
          <>
            <Campo etiqueta="Fecha de inicio" error={errores.fechaInicio}>
              <input type="date" value={datos.fechaInicio} min={hoyIso()} aria-invalid={Boolean(errores.fechaInicio)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
            </Campo>
            <Campo etiqueta="Hora de inicio" error={errores.horas}>
              <input type="time" value={datos.horaInicio ?? ''} aria-invalid={Boolean(errores.horas)} onChange={(e) => cambiar({ horaInicio: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Fecha de finalización" error={errores.fechaFin}>
              <input type="date" value={datos.fechaFin ?? ''} min={datos.fechaInicio || hoyIso()} aria-invalid={Boolean(errores.fechaFin)} onChange={(e) => cambiar({ fechaFin: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Hora de finalización">
              <input type="time" value={datos.horaFin ?? ''} onChange={(e) => cambiar({ horaFin: e.target.value || null })} />
            </Campo>
          </>
        )}
        {datos.modalidad === 'Recurrente' && (
          <>
            <fieldset className="ca-dias" data-ancho="completo" aria-invalid={Boolean(errores.dias)}>
              <legend>Días de la semana</legend>
              <div className="ca-dias-opciones">
                {DIAS.map((d) => (
                  <label key={d.valor} title={d.texto}>
                    <input type="checkbox" checked={datos.diasSemana.includes(d.valor)} aria-label={d.texto}
                      onChange={(e) => cambiar({ diasSemana: e.target.checked ? [...datos.diasSemana, d.valor].sort() : datos.diasSemana.filter((x) => x !== d.valor) })} />
                    <span aria-hidden="true">{d.corto}</span>
                  </label>
                ))}
              </div>
              {errores.dias && <small className="ad-campo-error" role="alert">{errores.dias}</small>}
            </fieldset>
            <Campo etiqueta="Hora de inicio" error={errores.horas}>
              <input type="time" value={datos.horaInicio ?? ''} aria-invalid={Boolean(errores.horas)} onChange={(e) => cambiar({ horaInicio: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Hora de fin">
              <input type="time" value={datos.horaFin ?? ''} onChange={(e) => cambiar({ horaFin: e.target.value || null })} />
            </Campo>
            <Campo etiqueta="Se repite desde" error={errores.fechaInicio}>
              <input type="date" value={datos.fechaInicio} aria-invalid={Boolean(errores.fechaInicio)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
            </Campo>
            <Campo etiqueta="Hasta (opcional)" error={errores.fechaFin}>
              <input type="date" value={datos.fechaFin ?? ''} min={datos.fechaInicio || hoyIso()} aria-invalid={Boolean(errores.fechaFin)} onChange={(e) => cambiar({ fechaFin: e.target.value || null })} />
            </Campo>
          </>
        )}
        {datos.modalidad === 'Permanente' && (
          <Campo etiqueta="Vigente desde" error={errores.fechaInicio} ancho="completo">
            <input type="date" value={datos.fechaInicio} aria-invalid={Boolean(errores.fechaInicio)} onChange={(e) => cambiar({ fechaInicio: e.target.value })} />
          </Campo>
        )}

        {zonas.length > 0 && (
          <fieldset className="ca-zonas" data-ancho="completo">
            <legend>Zonas permitidas (opcional; sin selección, cualquier zona de la sede)</legend>
            <div className="ca-zonas-opciones">
              {zonas.map((z) => (
                <label key={z.id}>
                  <input type="checkbox" checked={datos.zonas.includes(z.id)}
                    onChange={(e) => cambiar({ zonas: e.target.checked ? [...datos.zonas, z.id] : datos.zonas.filter((x) => x !== z.id) })} />
                  {z.nombre}
                </label>
              ))}
            </div>
          </fieldset>
        )}
        <Campo etiqueta="Motivo" error={errores.motivo} ancho="completo">
          <textarea value={datos.motivo} maxLength={500} aria-invalid={Boolean(errores.motivo)} onChange={(e) => cambiar({ motivo: e.target.value })} />
        </Campo>
        <Campo etiqueta="Observación (opcional)" ancho="completo">
          <textarea value={datos.observacion ?? ''} maxLength={1000} onChange={(e) => cambiar({ observacion: e.target.value || null })} />
        </Campo>
      </div>
      <p className="ad-tenue pe-nota">La solicitud queda <strong>pendiente de aprobación</strong> y no habilita el ingreso hasta que un administrador de la organización la apruebe.</p>
      {crear.error && <p className="ad-mensaje" role="alert">{mensajeError(crear.error, 'No se pudo registrar la solicitud.')}</p>}
    </Dialogo>
  )
}
