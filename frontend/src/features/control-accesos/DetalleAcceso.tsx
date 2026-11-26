import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Selector } from '../../shared/ui/Selector'
import { Campo, Dialogo, EstadoConsulta } from '../administracion/componentes'
import { mensajeError } from '../administracion/utilidades'
import { useAcciones } from '../modulos/apiModulos'
import {
  consultarAcceso, consultarBitacora, decidirAcceso, decidirObjeto, nuevaClave, registrarEntrada, registrarMovimientoObjeto, textoModalidad,
  type AccionObjeto, type AccionRegistro, type EstadoAcceso, type ObjetoAcceso, type OpcionesAcceso, type RegistroAcceso, type VigenciaAcceso,
} from './apiControlAccesos'
import { condiciones, enSede, instante, motivosError, TEXTO_ESTADO, TEXTO_VIGENCIA } from './formato'

export function EstadoAccesoEtiqueta({ estado, vigencia }: { estado: EstadoAcceso; vigencia?: VigenciaAcceso }) {
  const extra = vigencia ? TEXTO_VIGENCIA[vigencia] : ''
  return (
    <span className="au-estado-grupo">
      <span className="ad-estatus au-estado" data-estado={estado}>{TEXTO_ESTADO[estado] ?? estado}</span>
      {extra && <span className="au-vigencia" data-vigencia={vigencia}>{extra}</span>}
    </span>
  )
}

const ACCIONES: Record<AccionRegistro, { titulo: string; boton: string; exitoso: string; motivo: string | null; peligro: boolean }> = {
  aprobar: { titulo: 'Aprobar acceso', boton: 'Aprobar', exitoso: 'Acceso aprobado. Rige según sus fechas y horario.', motivo: null, peligro: false },
  rechazar: { titulo: 'Rechazar acceso', boton: 'Rechazar', exitoso: 'Acceso rechazado. Se conserva en el historial.', motivo: 'Motivo del rechazo', peligro: true },
  revocar: { titulo: 'Revocar acceso', boton: 'Revocar', exitoso: 'Acceso revocado: no habilita nuevos ingresos.', motivo: 'Motivo de la revocación', peligro: true },
  cancelar: { titulo: 'Cancelar acceso', boton: 'Cancelar registro', exitoso: 'Registro cancelado.', motivo: 'Motivo de la cancelación', peligro: true },
}
const TEXTO_OBJETO: Record<string, string> = { Pendiente: 'Pendiente', Aprobada: 'Aprobado', Rechazada: 'Rechazado', Revocada: 'Revocado' }
const TEXTO_EVENTO: Record<string, string> = {
  Crear: 'Registro creado', AprobarPorPolitica: 'Aprobado por política de la categoría', Aprobar: 'Aprobado', Rechazar: 'Rechazado', Revocar: 'Revocado',
  Cancelar: 'Cancelado', CambiarHorario: 'Horario modificado', Solicitar: 'Objeto solicitado', RegistrarEntrada: 'Entrada', RegistrarSalida: 'Salida',
  IngresoFueraDeHorario: 'Ingreso fuera de horario (alerta)', Migrar: 'Migrado desde Visitas', Registrar: 'Visita registrada (módulo anterior)',
  RegistrarEntradaObjeto: 'Entrada del objeto', RegistrarSalidaObjeto: 'Salida del objeto',
}

/**
 * Detalle del registro de acceso: condiciones, motivos actuales de ingreso, decisiones (persona y objetos por separado),
 * entrada por un punto de acceso, historial de estados, movimientos y bitácora inmutable.
 */
export function DetalleAcceso({ id, opciones, onClose, onAviso }: { id: number; opciones: OpcionesAcceso | undefined; onClose: () => void; onAviso: (m: string) => void }) {
  const control = useAcciones('control_accesos')
  const autorizaciones = useAcciones('autorizaciones')
  const queryClient = useQueryClient()
  const detalle = useQuery({ queryKey: ['accesos', 'detalle', id], queryFn: () => consultarAcceso(id) })
  const [accion, setAccion] = useState<AccionRegistro | null>(null)
  const [motivo, setMotivo] = useState('')
  const [errorMotivo, setErrorMotivo] = useState<string | null>(null)
  const [verBitacora, setVerBitacora] = useState(false)
  const bitacora = useQuery({ queryKey: ['accesos', 'bitacora', id], queryFn: () => consultarBitacora(id), enabled: verBitacora })
  const refrescar = (resultado?: RegistroAcceso) => {
    if (resultado) queryClient.setQueryData(['accesos', 'detalle', id], resultado)
    void queryClient.invalidateQueries({ queryKey: ['accesos'], predicate: (q) => !(q.queryKey[1] === 'detalle' && q.queryKey[2] === id && resultado) })
  }
  const decidir = useMutation({
    mutationFn: (a: AccionRegistro) => decidirAcceso(id, a, motivo.trim() || null),
    onSuccess: (resultado, a) => { refrescar(resultado); setAccion(null); setMotivo(''); onAviso(ACCIONES[a].exitoso) },
    // Un 409 (otro usuario cambió el registro) deja el detalle desactualizado: se recarga.
    onError: () => refrescar(),
  })
  const a = detalle.data

  function confirmar() {
    if (!accion) return
    if (ACCIONES[accion].motivo && !motivo.trim()) { setErrorMotivo(`Indica el ${ACCIONES[accion].motivo!.toLowerCase()}.`); return }
    setErrorMotivo(null)
    if (!decidir.isPending) decidir.mutate(accion)
  }

  // Coherencia visual: el servidor decide (el anfitrión puede decidir en flujo Anfitrion aunque no tenga Autorizaciones·Editar).
  const puedeDecidir = autorizaciones.editar || a?.flujoAprobacion === 'Anfitrion'
  const disponibles: AccionRegistro[] = !a ? [] : [
    ...(a.estado === 'Pendiente' && puedeDecidir ? ['aprobar', 'rechazar'] as const : []),
    ...(a.estado === 'Aprobada' && autorizaciones.editar ? ['revocar'] as const : []),
    ...((a.estado === 'Pendiente' || a.estado === 'Aprobada') && control.editar ? ['cancelar'] as const : []),
  ]

  return (
    <Dialogo titulo={a ? a.persona : 'Registro de acceso'} eyebrow={a ? `${a.categoria ?? 'Acceso'} · ${textoModalidad(a.modalidad).toLowerCase()} #${a.idAutorizacion}` : 'Registro de acceso'}
      icono="accesos" ancho="matriz" onClose={onClose} onSubmit={(e) => e.preventDefault()}
      pie={(
        <>
          <button type="button" className="ad-boton-sec" onClick={onClose}>Cerrar</button>
          {accion === null && disponibles.map((x) => (
            <button key={x} type="button" className={ACCIONES[x].peligro ? 'ad-boton-sec au-peligro' : 'ad-boton'} onClick={() => { decidir.reset(); setAccion(x) }}>{ACCIONES[x].boton}</button>
          ))}
        </>
      )}>
      <EstadoConsulta loading={detalle.isLoading} error={detalle.error} empty={false} vacio="" />
      {a && (
        <>
          <dl className="dt-franja">
            <div><dt>Estado</dt><dd><EstadoAccesoEtiqueta estado={a.estado} vigencia={a.vigencia} />{a.dentro && <span className="au-vigencia" data-vigencia="Vigente">Dentro</span>}</dd></div>
            <div><dt>Persona</dt><dd>{a.persona} <small>· {a.documentoPersona}</small></dd></div>
            <div><dt>Permanencia</dt><dd>{textoModalidad(a.modalidad)}{a.cupoIngresos ? <small> · {a.ingresosConsumidos}/{a.cupoIngresos} ingreso{a.cupoIngresos === 1 ? '' : 's'}</small> : null}</dd></div>
          </dl>
          <div className="dt-bloques">
            <section className="dt-bloque" aria-label="Acceso autorizado">
              <h3>Acceso autorizado</h3>
              <dl>
                <div><dt>Sede y destino</dt><dd>{a.sede}<small>{a.area}</small></dd></div>
                <div><dt>Condiciones</dt><dd>{condiciones(a)}</dd></div>
              </dl>
            </section>
            <section className="dt-bloque" aria-label="Trazabilidad">
              <h3>Trazabilidad</h3>
              <dl>
                <div><dt>Anfitrión</dt><dd>{a.anfitrion ?? '—'}</dd></div>
                <div><dt>Registrado por</dt><dd>{a.creadoPor ?? '—'}<small>{instante(a.creadoEn)}{a.origen === 'Migracion' ? ' · migrado' : ''}</small></dd></div>
                {(a.decididoPor || a.origen === 'Politica') && (
                  <div><dt>Decidido por</dt><dd>{a.decididoPor ?? 'Política de la categoría'}<small>{a.decididoEn && instante(a.decididoEn)}</small></dd></div>
                )}
              </dl>
            </section>
          </div>
          {(a.motivo || a.observacion || a.motivoDecision) && (
            <section className="dt-bloque" aria-label="Motivos y observaciones">
              <h3>Motivos y observaciones</h3>
              <dl>
                {a.motivo && <div><dt>Motivo del ingreso</dt><dd><p className="dt-nota">{a.motivo}</p></dd></div>}
                {a.observacion && <div><dt>Observación</dt><dd><p className="dt-nota">{a.observacion}</p></dd></div>}
                {a.motivoDecision && <div><dt>Motivo de la decisión</dt><dd><p className="dt-nota" data-tono={a.estado === 'Aprobada' ? undefined : 'rechazo'}>{a.motivoDecision}</p></dd></div>}
              </dl>
            </section>
          )}
          {accion && (
            <div className="pe-subformulario" role="group" aria-label={ACCIONES[accion].titulo}>
              <strong>{ACCIONES[accion].titulo}</strong>
              <Campo etiqueta={ACCIONES[accion].motivo ?? 'Observación (opcional)'} error={errorMotivo ?? undefined}>
                <textarea value={motivo} maxLength={500} autoFocus aria-invalid={Boolean(errorMotivo)} onChange={(e) => setMotivo(e.target.value)} />
              </Campo>
              {decidir.error && <p className="ad-mensaje" role="alert">{mensajeError(decidir.error)}</p>}
              <div className="pe-subacciones">
                <button type="button" className="ad-boton-sec" onClick={() => { setAccion(null); setErrorMotivo(null) }}>Volver</button>
                <button type="button" className="ad-boton" onClick={confirmar} disabled={decidir.isPending}>{decidir.isPending ? 'Guardando…' : `Confirmar: ${ACCIONES[accion].boton.toLowerCase()}`}</button>
              </div>
            </div>
          )}
          {a.estado === 'Aprobada' && !a.dentro && control.anadir && <EntradaAcceso acceso={a} opciones={opciones} onRegistrada={(m) => { refrescar(); onAviso(m) }} />}
          <ObjetosAcceso acceso={a} opciones={opciones} puedeDecidir={autorizaciones.editar} onCambio={(r, m) => { refrescar(r); onAviso(m) }} />
          <section className="dt-bloque" aria-label="Historial de estados">
            <h3>Historial de estados</h3>
            <ol className="dt-linea">
              {(a.historial ?? []).map((t, i) => (
                <li key={i} data-tono={t.estadoNuevo === 'Rechazada' || t.estadoNuevo === 'Revocada' || t.estadoNuevo === 'Cancelada' ? 'rechazo' : undefined}>
                  <strong>{t.estadoAnterior ? `${TEXTO_ESTADO[t.estadoAnterior as EstadoAcceso] ?? t.estadoAnterior} → ${TEXTO_ESTADO[t.estadoNuevo as EstadoAcceso] ?? t.estadoNuevo}` : `Registrado (${TEXTO_ESTADO[t.estadoNuevo as EstadoAcceso] ?? t.estadoNuevo})`}</strong>
                  <small>{t.actor ?? 'Sistema'} · {instante(t.ocurridoEn)}</small>
                  {t.observacion && <p className="dt-nota">{t.observacion}</p>}
                </li>
              ))}
            </ol>
          </section>
          {(a.movimientos?.length ?? 0) > 0 && (
            <section className="dt-bloque" aria-label="Movimientos">
              <h3>Entradas, salidas e intentos</h3>
              <ol className="dt-linea">
                {a.movimientos!.map((m) => (
                  <li key={m.idMovimiento} data-tono={m.resultado === 'Denegado' ? 'rechazo' : undefined}>
                    <strong>{m.tipoMovimiento}{m.resultado === 'Denegado' ? ' denegada' : ''} · {m.puntoAcceso}</strong>
                    <small>{m.operador ?? '—'} · {enSede(m.ocurridoEn, a.zonaHoraria)}</small>
                    {m.motivo && <p className="dt-nota">{m.motivo}</p>}
                  </li>
                ))}
              </ol>
            </section>
          )}
          <section className="dt-bloque" aria-label="Bitácora">
            <h3>Bitácora</h3>
            <button type="button" className="ad-enlace" aria-expanded={verBitacora} onClick={() => setVerBitacora((v) => !v)}>{verBitacora ? 'Ocultar bitácora' : 'Ver bitácora completa (inmutable)'}</button>
            {verBitacora && <EstadoConsulta loading={bitacora.isLoading} error={bitacora.error} empty={!bitacora.data?.length} vacio="Sin eventos." />}
            {verBitacora && bitacora.data && bitacora.data.length > 0 && (
              <ol className="dt-linea" aria-label="Eventos de la bitácora">
                {bitacora.data.map((e) => (
                  <li key={e.idAuditoria} data-tono={e.resultado === 'Denegado' || e.resultado === 'Rechazado' ? 'rechazo' : e.resultado === 'Alerta' ? 'violeta' : undefined}>
                    <strong>{TEXTO_EVENTO[e.accion] ?? e.accion}{e.entidad !== 'Autorizacion' ? ` · ${e.entidad === 'AutorizacionObjeto' ? 'objeto' : e.entidad.toLowerCase()} #${e.idRegistro}` : ''}{e.resultado !== 'Exitoso' ? ` (${e.resultado.toLowerCase()})` : ''}</strong>
                    <small>{e.usuario ?? 'Sistema'} · {instante(e.ocurridoEn)}{e.direccionIp ? ` · IP ${e.direccionIp}` : ''}</small>
                  </li>
                ))}
              </ol>
            )}
          </section>
        </>
      )}
    </Dialogo>
  )
}

/** Entrada por un punto de acceso de la sede. Una clave de idempotencia por intento; se reutiliza si el envío falla. */
function EntradaAcceso({ acceso, opciones, onRegistrada }: { acceso: RegistroAcceso; opciones: OpcionesAcceso | undefined; onRegistrada: (m: string) => void }) {
  const puntos = (opciones?.puntosAcceso ?? []).filter((p) => p.idSede === acceso.idSede && p.modoAcceso !== 'Salida')
  const [idPunto, setIdPunto] = useState(puntos.length === 1 ? puntos[0]!.id : 0)
  const [observacion, setObservacion] = useState('')
  const [clave, setClave] = useState(nuevaClave)
  const entrada = useMutation({
    mutationFn: () => registrarEntrada(acceso.idAutorizacion, idPunto, observacion.trim() || null, clave),
    onSuccess: (r) => { setClave(nuevaClave()); onRegistrada(r.alertas.length ? `Entrada registrada con alerta: ${r.alertas.join(' ')}` : 'Entrada registrada.') },
    // Un rechazo del servidor es definitivo para ese intento: el siguiente usa otra clave.
    onError: () => setClave(nuevaClave()),
  })
  return (
    <section className="dt-bloque" aria-label="Registrar entrada">
      <h3>Registrar entrada</h3>
      {(acceso.motivosIngreso?.length ?? 0) > 0 && (
        <ul className="vi-alerta" aria-label="Motivos por los que hoy no puede ingresar">{acceso.motivosIngreso!.map((m) => <li key={m}>{m}</li>)}</ul>
      )}
      <div className="ad-rejilla">
        <Campo etiqueta="Punto de acceso">
          <Selector className="sel-campo" etiqueta="Punto de acceso" valor={String(idPunto || '')} onChange={(v) => setIdPunto(Number(v))}
            opciones={[{ valor: '', texto: puntos.length ? 'Selecciona…' : 'La sede no tiene puntos de entrada', deshabilitada: true }, ...puntos.map((p) => ({ valor: String(p.id), texto: p.nombre }))]} />
        </Campo>
        <Campo etiqueta="Observación (opcional)">
          <input value={observacion} maxLength={500} onChange={(e) => setObservacion(e.target.value)} />
        </Campo>
      </div>
      {entrada.error && <ul className="ad-mensaje" role="alert">{motivosError(entrada.error).map((m) => <li key={m}>{m}</li>)}</ul>}
      <button type="button" className="ad-boton" disabled={!idPunto || entrada.isPending} onClick={() => entrada.mutate()}>{entrada.isPending ? 'Registrando…' : 'Registrar entrada'}</button>
    </section>
  )
}

/**
 * Cada vehículo o dispositivo tiene su propia decisión: aprobar a la persona no los aprueba. En portería se registra su
 * entrada (con la persona ya dentro y el objeto aprobado) y su salida, que no depende de la persona (ADR-030, D-10).
 * Los botones siguen los permisos visibles; el servidor valida todo.
 */
function ObjetosAcceso({ acceso, opciones, puedeDecidir, onCambio }: { acceso: RegistroAcceso; opciones: OpcionesAcceso | undefined; puedeDecidir: boolean; onCambio: (r: RegistroAcceso | undefined, m: string) => void }) {
  const control = useAcciones('control_accesos')
  const vehiculos = useAcciones('vehiculos')
  const dispositivos = useAcciones('dispositivos')
  const [movimiento, setMovimiento] = useState<{ id: number; tipo: 'entrada' | 'salida' } | null>(null)
  const [decision, setDecision] = useState<{ id: number; accion: AccionObjeto } | null>(null)
  const [motivo, setMotivo] = useState('')
  const [errorMotivo, setErrorMotivo] = useState<string | null>(null)
  const decidir = useMutation({
    mutationFn: (d: { id: number; accion: AccionObjeto }) => decidirObjeto(acceso.idAutorizacion, d.id, d.accion, motivo.trim() || null),
    onSuccess: (r, d) => { setDecision(null); setMotivo(''); onCambio(r, d.accion === 'aprobar' ? 'Elemento aprobado.' : d.accion === 'rechazar' ? 'Elemento rechazado.' : 'Elemento revocado.') },
  })
  if (acceso.objetos.length === 0) return null
  return (
    <section className="dt-bloque" aria-label="Vehículos y dispositivos">
      <h3>Vehículos y dispositivos</h3>
      <ul className="dt-objetos">
        {acceso.objetos.map((o) => {
          const activa = decision?.id === o.idAutorizacionObjeto ? decision : null
          const botones: AccionObjeto[] = !puedeDecidir ? [] : o.estado === 'Pendiente' ? ['aprobar', 'rechazar'] : o.estado === 'Aprobada' ? ['revocar'] : []
          const modulo = o.tipoObjeto === 'Vehiculo' ? vehiculos : dispositivos
          const conModulo = modulo.consultar || modulo.anadir || modulo.editar
          const tipoMovimiento = o.dentro
            ? (control.editar && conModulo ? 'salida' as const : null)
            : (o.estado === 'Aprobada' && acceso.estado === 'Aprobada' && acceso.dentro && control.anadir && conModulo ? 'entrada' as const : null)
          const moviendo = movimiento?.id === o.idAutorizacionObjeto ? movimiento : null
          return (
            <li key={o.idAutorizacionObjeto} className="dt-objeto">
              <header>
                <div><strong>{o.objeto ?? `${o.tipoObjeto} #${o.idObjeto}`}</strong><small>{o.tipoObjeto === 'Vehiculo' ? 'Vehículo' : 'Dispositivo'}{o.decididoPor ? ` · ${o.decididoPor}` : ''}</small></div>
                <div className="dt-objeto-acciones">
                  {o.dentro && <span className="au-vigencia" data-vigencia="Vigente">Dentro</span>}
                  <span className="dt-decision" data-estado={o.estado}>{TEXTO_OBJETO[o.estado]}</span>
                  {!activa && !moviendo && tipoMovimiento && (
                    <button type="button" className="ad-boton-sec" onClick={() => { setDecision(null); setMovimiento({ id: o.idAutorizacionObjeto, tipo: tipoMovimiento }) }}>
                      {tipoMovimiento === 'entrada' ? 'Registrar entrada' : 'Registrar salida'}
                    </button>
                  )}
                  {!activa && botones.map((b) => (
                    <button key={b} type="button" className={b === 'aprobar' ? 'ad-boton-sec dt-boton-verde' : 'ad-boton-sec au-peligro'}
                      onClick={() => { decidir.reset(); setErrorMotivo(null); setMovimiento(null); setDecision({ id: o.idAutorizacionObjeto, accion: b }) }}>
                      {b === 'aprobar' ? 'Aprobar' : b === 'rechazar' ? 'Rechazar' : 'Revocar'}
                    </button>
                  ))}
                </div>
              </header>
              {o.motivoDecision && <p className="dt-nota" data-tono={o.estado === 'Aprobada' ? undefined : 'rechazo'}>{o.motivoDecision}</p>}
              {o.dentro && o.entradaEn && <p className="dt-nota">Dentro desde {enSede(o.entradaEn, acceso.zonaHoraria)}{acceso.dentro ? '' : ' · la persona ya salió'}</p>}
              {moviendo && (
                <MovimientoObjeto acceso={acceso} objeto={o} tipo={moviendo.tipo} opciones={opciones} onCancelar={() => setMovimiento(null)}
                  onRegistrado={(m) => { setMovimiento(null); onCambio(undefined, m) }} />
              )}
              {activa && (
                <div className="pe-subformulario" role="group" aria-label={`Decidir ${o.objeto ?? 'elemento'}`}>
                  <Campo etiqueta={activa.accion === 'aprobar' ? 'Observación (opcional)' : 'Motivo'} error={errorMotivo ?? undefined}>
                    <textarea value={motivo} maxLength={500} autoFocus aria-invalid={Boolean(errorMotivo)} onChange={(e) => setMotivo(e.target.value)} />
                  </Campo>
                  {decidir.error && <p className="ad-mensaje" role="alert">{mensajeError(decidir.error)}</p>}
                  <div className="pe-subacciones">
                    <button type="button" className="ad-boton-sec" onClick={() => { setDecision(null); setMotivo(''); setErrorMotivo(null) }}>Volver</button>
                    <button type="button" className="ad-boton" disabled={decidir.isPending} onClick={() => {
                      if (activa.accion !== 'aprobar' && !motivo.trim()) { setErrorMotivo('Indica el motivo.'); return }
                      if (!decidir.isPending) decidir.mutate(activa)
                    }}>{decidir.isPending ? 'Guardando…' : 'Confirmar'}</button>
                  </div>
                </div>
              )}
            </li>
          )
        })}
      </ul>
    </section>
  )
}

/** Entrada o salida del vehículo o dispositivo por un punto de la sede. Una clave de idempotencia por intento. */
function MovimientoObjeto({ acceso, objeto, tipo, opciones, onCancelar, onRegistrado }: {
  acceso: RegistroAcceso; objeto: ObjetoAcceso; tipo: 'entrada' | 'salida'; opciones: OpcionesAcceso | undefined; onCancelar: () => void; onRegistrado: (m: string) => void
}) {
  const puntos = (opciones?.puntosAcceso ?? []).filter((p) => p.idSede === acceso.idSede && p.modoAcceso !== (tipo === 'entrada' ? 'Salida' : 'Entrada'))
  const [idPunto, setIdPunto] = useState(puntos.length === 1 ? puntos[0]!.id : 0)
  const [observacion, setObservacion] = useState('')
  const [clave, setClave] = useState(nuevaClave)
  const nombre = objeto.tipoObjeto === 'Vehiculo' ? 'vehículo' : 'dispositivo'
  const registrar = useMutation({
    mutationFn: () => registrarMovimientoObjeto(acceso.idAutorizacion, objeto.idAutorizacionObjeto, tipo, idPunto, observacion.trim() || null, clave),
    onSuccess: (r) => {
      setClave(nuevaClave())
      onRegistrado(tipo === 'entrada' ? `Entrada del ${nombre} registrada.` : `Salida del ${nombre} registrada${r.personaFuera ? ' (la persona ya había salido)' : ''}.`)
    },
    // Un rechazo del servidor es definitivo para ese intento: el siguiente usa otra clave.
    onError: () => setClave(nuevaClave()),
  })
  const titulo = tipo === 'entrada' ? 'Punto de entrada' : 'Punto de salida'
  return (
    <div className="pe-subformulario" role="group" aria-label={`${tipo === 'entrada' ? 'Entrada' : 'Salida'} de ${objeto.objeto ?? nombre}`}>
      <div className="ad-rejilla">
        <Campo etiqueta={titulo}>
          <Selector className="sel-campo" etiqueta={titulo} valor={String(idPunto || '')} onChange={(v) => setIdPunto(Number(v))}
            opciones={[{ valor: '', texto: puntos.length ? 'Selecciona…' : `La sede no tiene puntos de ${tipo}`, deshabilitada: true }, ...puntos.map((p) => ({ valor: String(p.id), texto: p.nombre }))]} />
        </Campo>
        <Campo etiqueta="Observación (opcional)">
          <input value={observacion} maxLength={500} onChange={(e) => setObservacion(e.target.value)} />
        </Campo>
      </div>
      {registrar.error && <ul className="ad-mensaje" role="alert">{motivosError(registrar.error).map((m) => <li key={m}>{m}</li>)}</ul>}
      <div className="pe-subacciones">
        <button type="button" className="ad-boton-sec" onClick={onCancelar}>Volver</button>
        <button type="button" className="ad-boton" disabled={!idPunto || registrar.isPending} onClick={() => registrar.mutate()}>
          {registrar.isPending ? 'Registrando…' : tipo === 'entrada' ? 'Confirmar entrada' : 'Confirmar salida'}
        </button>
      </div>
    </div>
  )
}
