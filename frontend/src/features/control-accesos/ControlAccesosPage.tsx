import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { apiBlob } from '../../shared/api/http'
import { Icono } from '../../shared/ui/Icono'
import { Selector } from '../../shared/ui/Selector'
import { BotonIcono, BotonNuevo, Buscador, Campo, Estado, EstadoConsulta, Filtro, Marco } from '../administracion/componentes'
import { mensajeError, useAviso, useDiferido } from '../administracion/utilidades'
import { useAcciones } from '../modulos/apiModulos'
import { leerOrganizacionActiva } from '../sesion/sesion'
import '../administracion/administracion.css'
import '../personas/personas.css'
import './autorizaciones.css'
import './visitas.css'
import './controlAccesos.css'
import {
  MODALIDADES, consultarAccesos, consultarHistorial, consultarObjetosDentro, consultarOpcionesAcceso, consultarPresencia, nuevaClave, registrarMovimientoObjeto,
  registrarSalida, rutaExportarHistorial, textoModalidad,
  type FiltrosHistorial, type ObjetoDentro, type OpcionesAcceso, type Presencia,
} from './apiControlAccesos'
import { DetalleAcceso, EstadoAccesoEtiqueta } from './DetalleAcceso'
import { NuevoAcceso } from './NuevoAcceso'
import { condiciones, duracion, enSede, motivosError } from './formato'

/*
 * Control de accesos unificado (ADR-029): un solo módulo y un solo punto de creación del registro de acceso. Las
 * «visitas» son una categoría configurable; el episodio de cada ingreso lo crea el servidor al registrar la entrada.
 * Pestañas: registros (listado, aprobación y detalle), personas dentro (salidas) e historial (filtros y CSV).
 */

type Pestana = 'registros' | 'presencia' | 'historial'
const ESTADOS = [
  { valor: '', texto: 'Todos los estados' }, { valor: 'Pendiente', texto: 'Pendientes de aprobación' }, { valor: 'Aprobada', texto: 'Aprobados' },
  { valor: 'Rechazada', texto: 'Rechazados' }, { valor: 'Revocada', texto: 'Revocados' }, { valor: 'Cancelada', texto: 'Cancelados' },
]
const TAMANO = 20

function useOpciones(habilitado: boolean) {
  return useQuery({ queryKey: ['accesos', 'opciones', leerOrganizacionActiva()], queryFn: consultarOpcionesAcceso, enabled: habilitado, staleTime: 60_000 })
}

export function ControlAccesosPage() {
  const acciones = useAcciones('control_accesos')
  const [pestana, setPestana] = useState<Pestana>('registros')
  const opciones = useOpciones(acciones.consultar)
  const { avisar, aviso } = useAviso()
  const pestanas = (
    <div className="ad-tabs" role="tablist" aria-label="Secciones de control de accesos">
      <button type="button" role="tab" aria-selected={pestana === 'registros'} onClick={() => setPestana('registros')}><Icono nombre="accesos" /> Registros</button>
      <button type="button" role="tab" aria-selected={pestana === 'presencia'} onClick={() => setPestana('presencia')}><Icono nombre="personas" /> Personas dentro</button>
      <button type="button" role="tab" aria-selected={pestana === 'historial'} onClick={() => setPestana('historial')}><Icono nombre="reportes" /> Historial</button>
    </div>
  )
  if (!acciones.consultar)
    return <Marco titulo="Control de accesos" migas="Operación" pestanas={null} descripcion=""><Estado tipo="permiso" texto="Tu perfil no tiene permiso para consultar el control de accesos." /></Marco>
  return (
    <>
      {pestana === 'registros' && <Registros pestanas={pestanas} opciones={opciones.data} avisar={avisar} />}
      {pestana === 'presencia' && <PersonasDentro pestanas={pestanas} opciones={opciones.data} avisar={avisar} />}
      {pestana === 'historial' && <Historial pestanas={pestanas} opciones={opciones.data} />}
      {aviso}
    </>
  )
}

// ---------- Registros ----------

function useFiltro(setPagina: (p: number) => void) {
  const [valor, asignar] = useState('')
  return [valor, (v: string) => { asignar(v); setPagina(1) }] as const
}

function Registros({ pestanas, opciones, avisar }: { pestanas: ReactNode; opciones: OpcionesAcceso | undefined; avisar: (m: string) => void }) {
  const acciones = useAcciones('control_accesos')
  const [parametros, setParametros] = useSearchParams()
  const [pagina, setPagina] = useState(1)
  // Cambiar un filtro vuelve a la primera página.
  const [buscar, setBuscar] = useFiltro(setPagina)
  const [estado, setEstado] = useFiltro(setPagina)
  const [idCategoria, setIdCategoria] = useFiltro(setPagina)
  const [modalidad, setModalidad] = useFiltro(setPagina)
  const [nueva, setNueva] = useState(false)
  const [detalle, setDetalle] = useState<number | null>(null)
  const texto = useDiferido(buscar)
  const lista = useQuery({
    queryKey: ['accesos', 'lista', leerOrganizacionActiva(), texto, estado, idCategoria, modalidad, pagina],
    queryFn: () => consultarAccesos({ buscar: texto, estado, idCategoria, modalidad, pagina, tamano: TAMANO }),
    placeholderData: keepPreviousData,
  })

  // «Gestionar acceso» desde Personas (?persona=ID): el formulario se abre con la persona identificada; al cerrarlo se
  // quita el parámetro para que recargar no lo vuelva a abrir.
  const personaSolicitada = Number(parametros.get('persona')) || null
  const formulario = nueva || (personaSolicitada !== null && acciones.anadir)
  function cerrar() {
    setNueva(false)
    if (personaSolicitada) setParametros((p) => { p.delete('persona'); return p }, { replace: true })
  }
  const datos = lista.data
  const paginas = datos ? Math.max(1, Math.ceil(datos.total / TAMANO)) : 1
  const filtrando = Boolean(buscar || estado || idCategoria || modalidad)

  return (
    <Marco titulo="Control de accesos" migas="Operación" pestanas={pestanas}
      descripcion="Registro único de accesos de personas, con su categoría, tipo de permanencia, horario, vehículos y dispositivos. Aprobado no es vigente: cada entrada valida fecha, horario y cupo."
      accion={acciones.anadir && <BotonNuevo texto="Nuevo acceso" onClick={() => setNueva(true)} disabled={!opciones} />}>
      <div className="ad-herramientas au-filtros">
        <Buscador valor={buscar} onChange={setBuscar} etiqueta="Buscar registros" placeholder="Persona o documento" />
        <Filtro etiqueta="Filtrar por estado" valor={estado} onChange={setEstado} opciones={ESTADOS} />
        <Filtro etiqueta="Filtrar por categoría" valor={idCategoria} onChange={setIdCategoria}
          opciones={[{ valor: '', texto: 'Todas las categorías' }, ...(opciones?.categorias ?? []).map((c) => ({ valor: String(c.idCategoriaAcceso), texto: c.nombre }))]} />
        <Filtro etiqueta="Filtrar por tipo de permanencia" valor={modalidad} onChange={setModalidad}
          opciones={[{ valor: '', texto: 'Todos los tipos' }, ...MODALIDADES.map((m) => ({ valor: m.valor, texto: m.texto }))]} />
      </div>
      <EstadoConsulta loading={lista.isLoading} error={lista.error} empty={!datos?.registros.length}
        vacio={filtrando ? 'Ningún registro coincide con los filtros.' : 'Aún no hay registros de acceso. Usa «Nuevo acceso».'} />
      {datos && datos.registros.length > 0 && (
        <>
          <div className="ad-tabla-wrap">
            <table className="ad-tabla">
              <thead>
                <tr>
                  <th scope="col">Persona</th>
                  <th scope="col">Categoría</th>
                  <th scope="col">Condiciones</th>
                  <th scope="col">Sede / destino</th>
                  <th scope="col">Estado</th>
                  <th scope="col"><span className="sr-only">Acciones</span></th>
                </tr>
              </thead>
              <tbody>
                {datos.registros.map((a) => (
                  <tr key={a.idAutorizacion}>
                    <td><strong>{a.persona}</strong><small className="au-sub">{a.documentoPersona}{a.personaActiva ? '' : ' · inactiva'}</small></td>
                    <td>{a.categoria ? <span className="ca-categoria" data-flujo={a.flujoAprobacion ?? ''}>{a.categoria}</span> : '—'}<small className="au-sub">{textoModalidad(a.modalidad)}</small></td>
                    <td>{condiciones(a)}{a.objetos.length > 0 && <small className="au-sub">{a.objetos.length} vehículo(s)/dispositivo(s)</small>}</td>
                    <td>{a.sede}<small className="au-sub">{a.area}{a.anfitrion ? ` · ${a.anfitrion}` : ''}</small></td>
                    <td><EstadoAccesoEtiqueta estado={a.estado} vigencia={a.vigencia} />{a.dentro && <small className="au-sub">Dentro</small>}</td>
                    <td className="ad-acciones"><BotonIcono etiqueta={`Ver acceso de ${a.persona}`} icono="flecha" onClick={() => setDetalle(a.idAutorizacion)} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <nav className="vi-paginacion" aria-label="Paginación de registros">
            <span>{datos.total} registro{datos.total === 1 ? '' : 's'} · página {datos.pagina} de {paginas}</span>
            <button type="button" className="ad-boton-sec" disabled={pagina <= 1} onClick={() => setPagina((p) => p - 1)}>Anterior</button>
            <button type="button" className="ad-boton-sec" disabled={pagina >= paginas} onClick={() => setPagina((p) => p + 1)}>Siguiente</button>
          </nav>
        </>
      )}
      {formulario && opciones && (
        <NuevoAcceso key={personaSolicitada ?? 'nuevo'} opciones={opciones} idPersonaInicial={personaSolicitada} onClose={cerrar}
          onCreado={(a) => { cerrar(); avisar(a.estado === 'Aprobada' ? 'Acceso registrado y aprobado por la política de la categoría.' : 'Acceso registrado. Queda pendiente de aprobación.'); setDetalle(a.idAutorizacion) }} />
      )}
      {detalle !== null && <DetalleAcceso id={detalle} opciones={opciones} onClose={() => setDetalle(null)} onAviso={avisar} />}
    </Marco>
  )
}

// ---------- Personas dentro ----------

function useAhora(ms = 30_000) {
  const [ahora, setAhora] = useState(() => Date.now())
  useEffect(() => {
    const id = window.setInterval(() => setAhora(Date.now()), ms)
    return () => window.clearInterval(id)
  }, [ms])
  return ahora
}

function PersonasDentro({ pestanas, opciones, avisar }: { pestanas: ReactNode; opciones: OpcionesAcceso | undefined; avisar: (m: string) => void }) {
  const acciones = useAcciones('control_accesos')
  const vehiculos = useAcciones('vehiculos')
  const dispositivos = useAcciones('dispositivos')
  const veObjetos = [vehiculos, dispositivos].some((m) => m.consultar || m.anadir || m.editar)
  const [idSede, setIdSede] = useState('')
  const [detalle, setDetalle] = useState<number | null>(null)
  const [saliendo, setSaliendo] = useState<Presencia | null>(null)
  const ahora = useAhora()
  // Sin SignalR: la API es la fuente persistida; se vuelve a consultar cada 30 s, al enfocar y al reconectar.
  const presencia = useQuery({
    queryKey: ['accesos', 'presencia', leerOrganizacionActiva(), idSede],
    queryFn: () => consultarPresencia(idSede),
    refetchInterval: 30_000, refetchOnWindowFocus: true, refetchOnReconnect: true,
  })
  const objetos = useQuery({
    queryKey: ['accesos', 'objetos-dentro', leerOrganizacionActiva(), idSede],
    queryFn: () => consultarObjetosDentro(idSede),
    enabled: veObjetos, refetchInterval: 30_000, refetchOnWindowFocus: true, refetchOnReconnect: true,
  })
  return (
    <Marco titulo="Personas dentro" migas="Operación" pestanas={pestanas}
      descripcion="Personas con una permanencia abierta en cada sede. Se actualiza cada 30 segundos y al volver a la pantalla.">
      <div className="ad-herramientas au-filtros">
        <Filtro etiqueta="Filtrar por sede" valor={idSede} onChange={setIdSede}
          opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(opciones?.sedes ?? []).map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
        <span className="vi-contador" role="status">{presencia.data ? `${presencia.data.length} persona${presencia.data.length === 1 ? '' : 's'} dentro` : ''}</span>
      </div>
      <EstadoConsulta loading={presencia.isLoading} error={presencia.error} empty={!presencia.data?.length} vacio="No hay personas dentro en este momento." />
      {presencia.data && presencia.data.length > 0 && (
        <ul className="vi-presencia">
          {presencia.data.map((p) => (
            <li key={p.idPermanencia}>
              <button type="button" onClick={() => setDetalle(p.idAutorizacion)} aria-label={`Ver acceso de ${p.persona}`}>
                <strong>{p.persona}</strong>
                <small>{p.documentoPersona}{p.categoria ? ` · ${p.categoria}` : ''}</small>
                <span>{p.sede} · {p.area}{p.anfitrion ? ` · recibe ${p.anfitrion}` : ''}</span>
                <span className="vi-tiempo">Entró {enSede(p.entradaEn, p.zonaHoraria, { timeStyle: 'short' })} por {p.puntoEntrada} · {duracion((ahora - new Date(p.entradaEn).getTime()) / 1000)}</span>
                {p.objetosDentro.length > 0 && <span className="ca-objetos-presentes">Con {p.objetosDentro.join(' · ')}</span>}
              </button>
              {acciones.editar && <button type="button" className="ad-boton-sec" onClick={() => setSaliendo(p)}>Registrar salida</button>}
            </li>
          ))}
        </ul>
      )}
      {saliendo && (
        <Salida presencia={saliendo} opciones={opciones} onClose={() => setSaliendo(null)}
          onRegistrada={(pendientes) => { setSaliendo(null); avisar(pendientes.length ? `Salida registrada. Siguen dentro: ${pendientes.join(' · ')}.` : 'Salida registrada.') }} />
      )}
      {veObjetos && (
        <section className="ca-objetos-dentro" aria-label="Vehículos y dispositivos dentro">
          <h2>Vehículos y dispositivos dentro</h2>
          <EstadoConsulta loading={objetos.isLoading} error={objetos.error} empty={!objetos.data?.length} vacio="No hay vehículos ni dispositivos dentro." />
          {objetos.data && objetos.data.length > 0 && (
            <ul className="vi-presencia">
              {objetos.data.map((o) => <ObjetoDentroItem key={o.idMovimientoEntrada} objeto={o} ahora={ahora} opciones={opciones} puedeSalir={acciones.editar}
                onVer={() => setDetalle(o.idAutorizacion)} onRegistrada={avisar} />)}
            </ul>
          )}
        </section>
      )}
      {detalle !== null && <DetalleAcceso id={detalle} opciones={opciones} onClose={() => setDetalle(null)} onAviso={avisar} />}
    </Marco>
  )
}

/** Vehículo o dispositivo con entrada abierta; su salida se registra aparte de la persona (D-10). */
function ObjetoDentroItem({ objeto, ahora, opciones, puedeSalir, onVer, onRegistrada }: {
  objeto: ObjetoDentro; ahora: number; opciones: OpcionesAcceso | undefined; puedeSalir: boolean; onVer: () => void; onRegistrada: (m: string) => void
}) {
  const queryClient = useQueryClient()
  const puntos = (opciones?.puntosAcceso ?? []).filter((p) => p.idSede === objeto.idSede && p.modoAcceso !== 'Entrada')
  const [abierto, setAbierto] = useState(false)
  const [idPunto, setIdPunto] = useState(puntos.length === 1 ? puntos[0]!.id : 0)
  const [clave, setClave] = useState(nuevaClave)
  const salida = useMutation({
    mutationFn: () => registrarMovimientoObjeto(objeto.idAutorizacion, objeto.idAutorizacionObjeto, 'salida', idPunto, null, clave),
    onSuccess: () => { setAbierto(false); void queryClient.invalidateQueries({ queryKey: ['accesos'] }); onRegistrada(`Salida de ${objeto.objeto} registrada.`) },
    onError: () => { setClave(nuevaClave()); void queryClient.invalidateQueries({ queryKey: ['accesos', 'objetos-dentro'] }) },
  })
  return (
    <li data-alerta={!objeto.personaDentro || undefined}>
      <button type="button" onClick={onVer} aria-label={`Ver acceso de ${objeto.persona}`}>
        <strong>{objeto.objeto}</strong>
        <small>{objeto.persona} · {objeto.documentoPersona}</small>
        <span>{objeto.sede}{objeto.personaDentro ? '' : ' · el responsable ya salió'}</span>
        <span className="vi-tiempo">Entró {enSede(objeto.entradaEn, objeto.zonaHoraria, { timeStyle: 'short' })} por {objeto.puntoEntrada} · {duracion((ahora - new Date(objeto.entradaEn).getTime()) / 1000)}</span>
      </button>
      {puedeSalir && !abierto && <button type="button" className="ad-boton-sec" onClick={() => { salida.reset(); setAbierto(true) }}>Registrar salida</button>}
      {abierto && (
        <div className="pe-subformulario" role="group" aria-label={`Registrar salida de ${objeto.objeto}`}>
          <Campo etiqueta="Punto de salida">
            <Selector className="sel-campo" etiqueta="Punto de salida" valor={String(idPunto || '')} onChange={(v) => setIdPunto(Number(v))}
              opciones={[{ valor: '', texto: puntos.length ? 'Selecciona…' : 'La sede no tiene puntos de salida', deshabilitada: true }, ...puntos.map((p) => ({ valor: String(p.id), texto: p.nombre }))]} />
          </Campo>
          {salida.error && <ul className="ad-mensaje" role="alert">{motivosError(salida.error).map((m) => <li key={m}>{m}</li>)}</ul>}
          <div className="pe-subacciones">
            <button type="button" className="ad-boton-sec" onClick={() => setAbierto(false)}>Volver</button>
            <button type="button" className="ad-boton" disabled={!idPunto || salida.isPending} onClick={() => salida.mutate()}>{salida.isPending ? 'Registrando…' : 'Confirmar salida'}</button>
          </div>
        </div>
      )}
    </li>
  )
}

function Salida({ presencia, opciones, onClose, onRegistrada }: { presencia: Presencia; opciones: OpcionesAcceso | undefined; onClose: () => void; onRegistrada: (objetosDentro: string[]) => void }) {
  const queryClient = useQueryClient()
  const puntos = (opciones?.puntosAcceso ?? []).filter((p) => p.idSede === presencia.idSede && p.modoAcceso !== 'Entrada')
  const [idPunto, setIdPunto] = useState(puntos.length === 1 ? puntos[0]!.id : 0)
  const [clave, setClave] = useState(nuevaClave)
  const salida = useMutation({
    mutationFn: () => registrarSalida(presencia.idPermanencia, idPunto, null, clave),
    onSuccess: (r) => { void queryClient.invalidateQueries({ queryKey: ['accesos'] }); onRegistrada(r.objetosDentro) },
    onError: () => { setClave(nuevaClave()); void queryClient.invalidateQueries({ queryKey: ['accesos', 'presencia'] }) },
  })
  return (
    <div className="pe-subformulario" role="group" aria-label={`Registrar salida de ${presencia.persona}`}>
      <strong>Salida de {presencia.persona}</strong>
      {presencia.objetosDentro.length > 0 && (
        <p className="vi-alerta" role="note">Siguen dentro: {presencia.objetosDentro.join(' · ')}. La salida de la persona no registra la de sus vehículos o dispositivos: regístrala aparte.</p>
      )}
      <Campo etiqueta="Punto de acceso">
        <Selector className="sel-campo" etiqueta="Punto de salida" valor={String(idPunto || '')} onChange={(v) => setIdPunto(Number(v))}
          opciones={[{ valor: '', texto: puntos.length ? 'Selecciona…' : 'La sede no tiene puntos de salida', deshabilitada: true }, ...puntos.map((p) => ({ valor: String(p.id), texto: p.nombre }))]} />
      </Campo>
      {salida.error && <ul className="ad-mensaje" role="alert">{motivosError(salida.error).map((m) => <li key={m}>{m}</li>)}</ul>}
      <div className="pe-subacciones">
        <button type="button" className="ad-boton-sec" onClick={onClose}>Volver</button>
        <button type="button" className="ad-boton" disabled={!idPunto || salida.isPending} onClick={() => salida.mutate()}>{salida.isPending ? 'Registrando…' : 'Confirmar salida'}</button>
      </div>
    </div>
  )
}

// ---------- Historial ----------

function Historial({ pestanas, opciones }: { pestanas: ReactNode; opciones: OpcionesAcceso | undefined }) {
  const [filtros, setFiltros] = useState<FiltrosHistorial>({})
  const [pagina, setPagina] = useState(1)
  const buscar = useDiferido(filtros.buscar ?? '')
  const placa = useDiferido(filtros.placa ?? '')
  const efectivos = { ...filtros, buscar, placa }
  const historial = useQuery({
    queryKey: ['accesos', 'historial', leerOrganizacionActiva(), efectivos, pagina],
    queryFn: () => consultarHistorial({ ...efectivos, pagina }),
    placeholderData: keepPreviousData,
  })
  const exportar = useMutation({
    mutationFn: () => apiBlob(rutaExportarHistorial(efectivos)),
    onSuccess: (blob) => {
      const url = URL.createObjectURL(blob)
      const enlace = document.createElement('a')
      enlace.href = url
      enlace.download = `historial-accesos-${new Date().toISOString().slice(0, 10)}.csv`
      enlace.click()
      URL.revokeObjectURL(url)
    },
  })
  const cambiar = (parcial: Partial<FiltrosHistorial>) => { setFiltros((f) => ({ ...f, ...parcial })); setPagina(1) }
  const datos = historial.data
  const paginas = datos ? Math.max(1, Math.ceil(datos.total / datos.tamano)) : 1

  return (
    <Marco titulo="Historial de accesos" migas="Operación" pestanas={pestanas}
      descripcion="Entradas, salidas e intentos denegados. Los movimientos no se modifican ni se borran; la exportación queda auditada."
      accion={<button type="button" className="ad-boton-sec" disabled={exportar.isPending} onClick={() => exportar.mutate()}>{exportar.isPending ? 'Exportando…' : 'Exportar CSV'}</button>}>
      <div className="ad-herramientas au-filtros">
        <Buscador valor={filtros.buscar ?? ''} onChange={(v) => cambiar({ buscar: v })} etiqueta="Buscar persona" placeholder="Persona o documento" />
        <Filtro etiqueta="Filtrar por sede" valor={filtros.idSede ?? ''} onChange={(v) => cambiar({ idSede: v })}
          opciones={[{ valor: '', texto: 'Todas las sedes' }, ...(opciones?.sedes ?? []).map((s) => ({ valor: String(s.id), texto: s.nombre }))]} />
        <Filtro etiqueta="Filtrar por anfitrión" valor={filtros.idAnfitrion ?? ''} onChange={(v) => cambiar({ idAnfitrion: v })}
          opciones={[{ valor: '', texto: 'Todos los anfitriones' }, ...(opciones?.anfitriones ?? []).map((a) => ({ valor: String(a.id), texto: a.nombre }))]} />
        <Filtro etiqueta="Filtrar por resultado" valor={filtros.resultado ?? ''} onChange={(v) => cambiar({ resultado: v })}
          opciones={[{ valor: '', texto: 'Todos los resultados' }, { valor: 'Confirmado', texto: 'Confirmados' }, { valor: 'Denegado', texto: 'Intentos denegados' }]} />
        <input className="ad-campo-corto" aria-label="Placa del vehículo" placeholder="Placa" value={filtros.placa ?? ''} onChange={(e) => cambiar({ placa: e.target.value })} />
        <input type="date" aria-label="Desde" value={filtros.desde ?? ''} onChange={(e) => cambiar({ desde: e.target.value })} />
        <input type="date" aria-label="Hasta" value={filtros.hasta ?? ''} onChange={(e) => cambiar({ hasta: e.target.value })} />
      </div>
      {exportar.error && <p className="ad-mensaje" role="alert">{mensajeError(exportar.error, 'No se pudo exportar.')}</p>}
      <EstadoConsulta loading={historial.isLoading} error={historial.error} empty={!datos?.registros.length} vacio="No hay movimientos con estos filtros." />
      {datos && datos.registros.length > 0 && (
        <>
          <div className="ad-tabla-wrap">
            <table className="ad-tabla">
              <thead>
                <tr>
                  <th scope="col">Fecha</th>
                  <th scope="col">Movimiento</th>
                  <th scope="col">Persona</th>
                  <th scope="col">Sede / destino</th>
                  <th scope="col">Punto · operador</th>
                </tr>
              </thead>
              <tbody>
                {datos.registros.map((m) => (
                  <tr key={m.idMovimiento} data-tono={m.resultado === 'Denegado' ? 'rechazo' : undefined}>
                    <td>{new Date(m.ocurridoEn).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' })}</td>
                    <td>
                      <span className="ad-estatus au-estado" data-estado={m.resultado === 'Denegado' ? 'Rechazada' : 'Aprobada'}>{m.tipoMovimiento}{m.resultado === 'Denegado' ? ' denegada' : ''}</span>
                      {m.motivo && <small className="au-sub">{m.motivo}</small>}
                    </td>
                    <td><strong>{m.persona}</strong><small className="au-sub">{m.documentoPersona}</small></td>
                    <td>{m.sede}<small className="au-sub">{m.area ?? ''}{m.anfitrion ? ` · ${m.anfitrion}` : ''}</small></td>
                    <td>{m.puntoAcceso}<small className="au-sub">{m.operador ?? '—'}</small></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <nav className="vi-paginacion" aria-label="Paginación del historial">
            <span>{datos.total} movimiento{datos.total === 1 ? '' : 's'} · página {datos.pagina} de {paginas}</span>
            <button type="button" className="ad-boton-sec" disabled={pagina <= 1} onClick={() => setPagina((p) => p - 1)}>Anterior</button>
            <button type="button" className="ad-boton-sec" disabled={pagina >= paginas} onClick={() => setPagina((p) => p + 1)}>Siguiente</button>
          </nav>
        </>
      )}
    </Marco>
  )
}
